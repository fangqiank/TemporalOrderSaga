using OrderSaga.Contracts;
using OrderSaga.Workflows;
using Temporalio.Client;

var client = await TemporalClient.ConnectAsync(new()
{
    TargetHost = "localhost:7233",
    Namespace = "default"
});

var input = new OrderInput(
    OrderId: Guid.NewGuid(),
    CustomerId: Guid.NewGuid(),
    Items: new List<OrderItemInput>
    {
        new(Guid.NewGuid(), "机械键盘", 1, 399),
        new(Guid.NewGuid(), "鼠标垫", 2, 29.9m)
    },
    TotalAmount: 458.8m
);

// 启动工作流
var handle = await client.StartWorkflowAsync<OrderSagaWorkflow, OrderResult>(
    wf => wf.ExecuteAsync(input),
    new WorkflowOptions
    {
        Id = $"order-{input.OrderId}",
        TaskQueue = "order-saga-task-queue"
    });

Console.WriteLine($"✅ 工作流已启动: {handle.Id}");

try
{
    var result = await handle.GetResultAsync();
    Console.WriteLine($"📊 结果: {result.Status}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 工作流失败: {ex.Message}");
}

