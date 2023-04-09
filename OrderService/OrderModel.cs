namespace OrderService
{
    public class OrderModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.ORDER_CREATED;
    }

    public enum OrderStatus
    { 
        ORDER_CREATED = 1,
        ORDER_FAILURE,
        ORDER_SUCCESS,
        PAYMENT_SUCCESS,
        PAYMENT_FAILURE,
        DELIVERY_SUCCESS,
        DELIVERY_FAILURE,
       
    }
}
