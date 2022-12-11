namespace Omnium
{
    public interface IOrdreRepository
    {
        public Task<List<Order>> GetOrders();

        public Task<Order> PostOrder(OrderDTO order);

        public Task PostMockOrders();
    }
}
