using Newtonsoft.Json;

namespace Omnium.models
{
    public class Order : IEquatable<Order>
    {
        [JsonProperty("id")]
        public Guid OrderId { get; set; }

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; }

        public double Total { get; set; }

        //Også en mulig implementasjon

        //private double _total;
        //public double Total
        //{
        //    get { return CalculateOrderTotal(this); }
        //    set { _total = value; }
        //}


        public List<OrderLine> OrderLines { get; set; }

        public Order(OrderDTO orderDto)
        {
            CustomerId = orderDto.CustomerId;
            CustomerName = orderDto.CustomerName;
            Total = orderDto.Total;
            OrderLines = orderDto.OrderLines;
            OrderId = Guid.NewGuid();
        }

        public Order() { }

        public double CalculateOrderTotal()
        {
            return OrderLines.Select(x => x.Price * x.Quantity).Sum();
        }

        public bool Equals(Order? other)
        {
            if (other == null) return false;
            return OrderId.ToString() == other.OrderId.ToString();
        }

        public override int GetHashCode()
        {
            return OrderId.ToString().GetHashCode();
        }
    }
}
