namespace Omnium.models
{
    public class OrderDTO
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }

        public int Total { get; set; }

        public List<OrderLine> OrderLines { get; set; }
    }
}

