using OrderSaga.Contracts;
using Temporalio.Common;
using Temporalio.Workflows;

namespace OrderSaga.Workflows;

[Workflow]
public class OrderSagaWorkflow : IOrderSagaWorkflow
{
    private readonly Stack<Func<Task>> _compensations = new();
    private OrderStatus _currentStatus = OrderStatus.Pending;
    private bool _customerCancelled;
    private string? _cancelReason;

    [WorkflowRun]
    public async Task<OrderResult> ExecuteAsync(OrderInput input)
    {
        Console.WriteLine($"🚀 订单 Saga 启动: OrderId={input.OrderId}, Amount={input.TotalAmount:C}");

        try
        {
            // 步骤 1: 预留库存
            Console.WriteLine($"📦 步骤 1/3: 预留库存");
            await UpdateOrderStatusAsync(input.OrderId, OrderStatus.Pending);

            // ✅ 正确的 Activity 调用方式
            var reserveResult = await Workflow.ExecuteActivityAsync(
                (OrderSagaActivities a) => a.ReserveInventoryAsync(
                    new ReserveInventoryInput(input.OrderId,
                        input.Items.Select(i => new InventoryItem(i.ProductId, i.Quantity)).ToList())),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(10),
                    RetryPolicy = new RetryPolicy
                    {
                        MaximumAttempts = 3,
                        InitialInterval = TimeSpan.FromSeconds(1),
                        BackoffCoefficient = 2,
                        MaximumInterval = TimeSpan.FromSeconds(10)
                    }
                });

            if (!reserveResult.Success)
                throw new InvalidOperationException(reserveResult.Reason ?? "库存预留失败");

            _compensations.Push(() => CompensateInventoryAsync(input.OrderId, reserveResult.ReservationId));
            await UpdateOrderStatusAsync(input.OrderId, OrderStatus.InventoryReserved);

            if (_customerCancelled)
                throw new InvalidOperationException($"客户取消: {_cancelReason}");

            // 步骤 2: 支付授权
            Console.WriteLine($"💳 步骤 2/3: 支付授权");
            var paymentResult = await Workflow.ExecuteActivityAsync(
                (OrderSagaActivities a) => a.AuthorizePaymentAsync(
                    new PaymentInput(input.OrderId, input.CustomerId, input.TotalAmount, input.CustomerBalance)),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(10),
                    RetryPolicy = new RetryPolicy
                    {
                        MaximumAttempts = 3,
                        InitialInterval = TimeSpan.FromSeconds(1),
                        BackoffCoefficient = 2
                    }
                });

            if (!paymentResult.Success)
                throw new InvalidOperationException(paymentResult.Reason ?? "支付失败");

            _compensations.Push(() => VoidPaymentAsync(input.OrderId, paymentResult.PaymentId));
            await UpdateOrderStatusAsync(input.OrderId, OrderStatus.PaymentAuthorized);

            // 步骤 3: 完成订单
            Console.WriteLine($"📧 步骤 3/3: 发送确认邮件");
            await Workflow.ExecuteActivityAsync(
                (OrderSagaActivities a) => a.SendConfirmationEmailAsync(input.OrderId, input.CustomerId),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(30),
                    RetryPolicy = new RetryPolicy { MaximumAttempts = 5 }
                });

            await UpdateOrderStatusAsync(input.OrderId, OrderStatus.Completed);
            Console.WriteLine($"🎉 订单 Saga 完成: OrderId={input.OrderId}");

            return new OrderResult(input.OrderId, "COMPLETED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Saga 失败: {ex.Message}");
            Console.WriteLine($"🔄 开始补偿");

            while (_compensations.Count > 0)
            {
                try
                {
                    await _compensations.Pop()();
                }
                catch (Exception compEx)
                {
                    Console.WriteLine($"⚠️ 补偿失败: {compEx.Message}");
                }
            }

            await UpdateOrderStatusAsync(input.OrderId, OrderStatus.Failed);
            return new OrderResult(input.OrderId, "FAILED", ex.Message);
        }
    }

    public async Task CustomerCancelledAsync(string reason)
    {
        Console.WriteLine($"🛑 客户取消订单: {reason}");
        _customerCancelled = true;
        _cancelReason = reason;
        await Task.CompletedTask;
    }

    public OrderStatus GetCurrentStatus() => _currentStatus;

    private async Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
    {
        _currentStatus = status;
        await Workflow.ExecuteActivityAsync(
            (OrderSagaActivities a) => a.UpdateOrderStatusAsync(orderId, status),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(5) });
    }

    private async Task CompensateInventoryAsync(Guid orderId, Guid reservationId)
    {
        await Workflow.ExecuteActivityAsync(
            (OrderSagaActivities a) => a.ReleaseInventoryAsync(orderId, reservationId),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
    }

    private async Task VoidPaymentAsync(Guid orderId, Guid paymentId)
    {
        await Workflow.ExecuteActivityAsync(
            (OrderSagaActivities a) => a.VoidPaymentAsync(orderId, paymentId),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
    }
}