namespace Omnium.models
{
    public class OrderLine
    {
        public int Quantity { get; set; }

        public double Price { get; set; }

        public string ProductName { get; set; }

        public Guid OrderLineId { get; set; }

        public Guid ProductId { get; set; }
    }
}
