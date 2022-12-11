using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Omnium
{
    public class OrderRepository : IOrdreRepository
    {
        private CosmosClient _client;

        public OrderRepository(IOptions<CosmosConfiguration> cosmosConfig)
        {
            _client = new(
            accountEndpoint: cosmosConfig.Value.Url!,
            authKeyOrResourceToken: cosmosConfig.Value.Key!
            );
        }

        private async Task<Container> GetContainer()
        {
            Database database = await _client.CreateDatabaseIfNotExistsAsync(
                id: "oms_db"
            );

            Container container = await database.CreateContainerIfNotExistsAsync(
                id: "order",
                partitionKeyPath: "/id",
                throughput: 400
            );

            return container;
        }


        public async Task<List<Order>> GetOrders()
        {
            var container = await GetContainer();
            
            using FeedIterator<Order> feed = container.GetItemQueryIterator<Order>();

            var res = new List<Order>();

            while (feed.HasMoreResults)
            {
                FeedResponse<Order> response = await feed.ReadNextAsync();

                foreach(Order order in response)
                {
                    res.Add(order);
                }
            }

            return res.ToList();
        }

        public async Task<Order> PostOrder(OrderDTO orderDto)
        {
            var container = await GetContainer();

            var order = new Order(orderDto);

            var response = await container.CreateItemAsync(order, new PartitionKey(order.OrderId.ToString()));

            return response.Resource;
        }

        public async Task PostMockOrders()
        {
            var mockOrders = MockOrders();

            var container = await GetContainer();

            foreach(var order in mockOrders)
            {
                await container.CreateItemAsync<Order>(order, new PartitionKey(order.OrderId.ToString()));
            }
        }

        public static List<Order> MockOrders()
        {
            var orders = new List<Order>();

            // Create a list of customer IDs
            var customerIds = new List<Tuple<string,string>>
            {
                Tuple.Create("3fa85f64-5717-4562-b3fc-2c963f66afa6","customer1"),
                Tuple.Create("2d2d2d2d-5717-4562-b3fc-2c963f66afa6","customer2"),
                Tuple.Create("1a1a1a1a-5717-4562-b3fc-2c963f66afa6","customer3"),
                Tuple.Create("4b4b4b4b-5717-4562-b3fc-2c963f66afa6","customer4"),
                Tuple.Create("5c5c5c5c-5717-4562-b3fc-2c963f66afa6","customer5")
            };

            // Create a list of product names
            var productNames = new List<Tuple<string,int>>
            {
                Tuple.Create("apple", 5),
                Tuple.Create("pear", 10),
                Tuple.Create("banana", 20),
                Tuple.Create("orange", 2),
                Tuple.Create("mango", 100)
            };

            // Create 10 orders
            for (int i = 0; i < 10; i++)
            {
                // Generate a random customer ID
                var customer = customerIds[new Random().Next(0, customerIds.Count)];

                // Generate a random product name
                var product = productNames[new Random().Next(0, productNames.Count)];

                // Create an order
                var order = new Order
                {
                    OrderId = Guid.NewGuid(),
                    CustomerId = new Guid(customer.Item1),
                    CustomerName = customer.Item2,
                    Total = 0,
                    OrderLines = new List<OrderLine>
                    {
                        new OrderLine
                        {
                            Quantity = new Random().Next(1,5),
                            Price = product.Item2,
                            ProductName = product.Item1,
                            OrderLineId = Guid.NewGuid(),
                            ProductId = Guid.NewGuid()
                        }
                    }
                };

                // Add the order to the list
                orders.Add(order);
            }
            return orders;
        }

    }
}
