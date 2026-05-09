using OrderSaga.Contracts;
using Temporalio.Activities;

namespace OrderSaga.Workflows
{
    public class OrderSagaActivities
    {
        [Activity]
        public async Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
        {
            await Task.Delay(50);
            Console.WriteLine($"  📝 订单状态更新: OrderId={orderId}, Status={status}");
        }

        [Activity]
        public async Task<ReserveInventoryResult> ReserveInventoryAsync(ReserveInventoryInput input)
        {
            await Task.Delay(200);
            var reservationId = Guid.NewGuid();
            Console.WriteLine($"  📦 库存预留: OrderId={input.OrderId}, Items={input.Items.Count}, ResId={reservationId}");

            // 模拟 10% 概率失败
            if (Random.Shared.Next(10) < 1)
            {
                Console.WriteLine($"  ❌ 库存不足: OrderId={input.OrderId}");
                return new ReserveInventoryResult(reservationId, false, "库存不足");
            }

            return new ReserveInventoryResult(reservationId, true);
        }

        [Activity]
        public async Task ReleaseInventoryAsync(Guid orderId, Guid reservationId)
        {
            await Task.Delay(100);
            Console.WriteLine($"  🔙 释放库存: OrderId={orderId}, ReservationId={reservationId}");
        }

        [Activity]
        public async Task<PaymentResult> AuthorizePaymentAsync(PaymentInput input)
        {
            await Task.Delay(300);
            var paymentId = Guid.NewGuid();
            Console.WriteLine($"  💳 支付授权: OrderId={input.OrderId}, Amount={input.Amount:C}, Balance={input.CustomerBalance:C}");

            if (input.Amount > input.CustomerBalance)
            {
                Console.WriteLine($"  ❌ 余额不足: OrderId={input.OrderId}, Need={input.Amount:C}, Have={input.CustomerBalance:C}");
                return new PaymentResult(paymentId, false, $"余额不足 (余额: ¥{input.CustomerBalance:F2}, 需要: ¥{input.Amount:F2})");
            }

            Console.WriteLine($"  ✅ 支付成功: OrderId={input.OrderId}, PaymentId={paymentId}");
            return new PaymentResult(paymentId, true);
        }

        [Activity]
        public async Task VoidPaymentAsync(Guid orderId, Guid paymentId)
        {
            await Task.Delay(150);
            Console.WriteLine($"  🔙 取消支付: OrderId={orderId}, PaymentId={paymentId}");
        }

        [Activity]
        public async Task SendConfirmationEmailAsync(Guid orderId, Guid customerId)
        {
            await Task.Delay(500);
            Console.WriteLine($"  📧 发送确认邮件: OrderId={orderId}, CustomerId={customerId}");
        }
    }
}
