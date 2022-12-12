namespace Omnium
{
    public interface IOrdreRepository
    {
        public Task<List<Order>> GetOrders();
        public Task<List<Order>> GetOrdersByCustomerId(string id);
        public Task<List<Order>> GetOrdersByProductId(string id);

        public Task<Order> GetOrder(string orderId);

        public Task<Order> PostOrder(OrderDTO order);

        public Task PostMockOrders();
    }
}
