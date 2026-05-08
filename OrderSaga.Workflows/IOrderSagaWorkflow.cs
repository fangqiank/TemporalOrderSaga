using OrderSaga.Contracts;

namespace OrderSaga.Workflows
{
    public interface IOrderSagaWorkflow
    {
        Task<OrderResult> ExecuteAsync(OrderInput input);
        Task CustomerCancelledAsync(string reason);
        OrderStatus GetCurrentStatus();
    }
}