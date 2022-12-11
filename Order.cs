using Newtonsoft.Json;

namespace Omnium
{
    public class Order
    {
        [JsonProperty("id")]
        public Guid OrderId { get; set; }

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; }

        public int Total { get; set; }

        public List<OrderLine> OrderLines { get; set; }

        public Order(OrderDTO orderDto) {
            CustomerId= orderDto.CustomerId;
            CustomerName= orderDto.CustomerName;
            Total= orderDto.Total;
            OrderLines = orderDto.OrderLines;
            OrderId = Guid.NewGuid();
        }

        public Order() { }
    }
}
