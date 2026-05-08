namespace OrderSaga.Contracts
{
    // ============ 工作流输入输出 ============
    public record OrderInput(
        Guid OrderId,
        Guid CustomerId,
        List<OrderItemInput> Items,
        decimal TotalAmount
    );

    public record OrderItemInput(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

    public record OrderResult(Guid OrderId, string Status, string? FailureReason = null);

    // ============ Activity 输入输出 ============
    public record ReserveInventoryInput(Guid OrderId, List<InventoryItem> Items);
    public record InventoryItem(Guid ProductId, int Quantity);
    public record ReserveInventoryResult(Guid ReservationId, bool Success, string? Reason = null);

    public record PaymentInput(Guid OrderId, Guid CustomerId, decimal Amount);
    public record PaymentResult(Guid PaymentId, bool Success, string? Reason = null);

    // ============ 订单状态枚举 ============
    public enum OrderStatus
    {
        Pending,
        InventoryReserved,
        PaymentAuthorized,
        Completed,
        Failed
    }
}
