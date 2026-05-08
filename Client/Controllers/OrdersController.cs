using Client.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderSaga.Contracts;
using OrderSaga.Workflows;
using Temporalio.Client;

namespace Client.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(AppDbContext db, TemporalClient temporalClient) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateOrderRequest request)
    {
        if (request.Items.Count == 0)
            return BadRequest(new { error = "购物车不能为空" });

        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var orderItems = new List<OrderItemInput>();
        var totalAmount = 0m;

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                return BadRequest(new { error = $"商品不存在: {item.ProductId}" });

            if (product.Stock < item.Quantity)
                return BadRequest(new { error = $"库存不足: {product.Name}" });

            orderItems.Add(new OrderItemInput(product.Id, product.Name, item.Quantity, product.Price));
            totalAmount += product.Price * item.Quantity;
        }

        var orderId = Guid.NewGuid();
        var customerId = request.CustomerId ?? Guid.NewGuid();
        var input = new OrderInput(orderId, customerId, orderItems, totalAmount);

        var handle = await temporalClient.StartWorkflowAsync<OrderSagaWorkflow, OrderResult>(
            wf => wf.ExecuteAsync(input),
            new WorkflowOptions
            {
                Id = $"order-{orderId}",
                TaskQueue = "order-saga-task-queue"
            });

        return Ok(new { orderId, workflowId = handle.Id });
    }

    [HttpGet("{workflowId}")]
    public async Task<ActionResult> GetStatus(string workflowId)
    {
        var handle = temporalClient.GetWorkflowHandle<IOrderSagaWorkflow>(workflowId);

        try
        {
            var desc = await handle.DescribeAsync();
            var isRunning = desc.Status.ToString().Contains("Running");

            if (isRunning)
            {
                try
                {
                    var queryStatus = await handle.QueryAsync(wf => wf.GetCurrentStatus());
                    return Ok(new { workflowId, status = queryStatus.ToString(), isRunning = true });
                }
                catch
                {
                    return Ok(new { workflowId, status = "Pending", isRunning = true });
                }
            }

            // Workflow finished — get actual result
            try
            {
                var result = await temporalClient.GetWorkflowHandle(workflowId).GetResultAsync<OrderResult>();
                var finalStatus = result.Status == "COMPLETED" ? "Completed" : "Failed";
                return Ok(new
                {
                    workflowId,
                    status = finalStatus,
                    failureReason = result.FailureReason,
                    isRunning = false
                });
            }
            catch
            {
                return Ok(new { workflowId, status = "Failed", isRunning = false });
            }
        }
        catch (Exception ex)
        {
            return Ok(new { workflowId, status = "Unknown", error = ex.Message });
        }
    }
}
