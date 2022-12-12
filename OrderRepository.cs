using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using Omnium.models;

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
        public async Task<List<Order>> GetOrdersByCustomerId(string customerId)
        {
            var container = await GetContainer();
            IOrderedQueryable<Order> queryable = container.GetItemLinqQueryable<Order>();

            var matches = queryable.Where(x => x.CustomerId.ToString() == customerId);

            using FeedIterator<Order> linqFeed = matches.ToFeedIterator();

            var results = new List<Order>();

            // Iterate query result pages
            while (linqFeed.HasMoreResults)
            {
                FeedResponse<Order> response = await linqFeed.ReadNextAsync();

                // Iterate query results
                foreach (Order item in response)
                {
                    results.Add(item);
                }
            }
            return results;
        }

        public async Task<List<Order>> GetOrdersByProductId(string id)
        {
            var container = await GetContainer();
            IOrderedQueryable<Order> queryable = container.GetItemLinqQueryable<Order>();

            var matches = queryable.Where(x => x.OrderLines.Where(x=>x.ProductId.ToString()==id).Any());

            using FeedIterator<Order> linqFeed = matches.ToFeedIterator();

            var results = new List<Order>();

            // Iterate query result pages
            while (linqFeed.HasMoreResults)
            {
                FeedResponse<Order> response = await linqFeed.ReadNextAsync();

                // Iterate query results
                foreach (Order item in response)
                {
                    results.Add(item);
                }
            }
            return results; 
        }

        public async Task<Order> GetOrder(string orderId)
        {
            var container = await GetContainer();

            var response = await container.ReadItemAsync<Order>(id: orderId, partitionKey: new PartitionKey(orderId));

            return response.Resource;
        }

        public static List<Order> MockOrders()
        {
            var orders = new List<Order>();

            // Create a list of customers
            var customers = new List<Tuple<Guid, string>>
            {
                Tuple.Create(new Guid("00000000-0000-0000-0000-000000000000"),"customer1"),
                Tuple.Create(new Guid("00000000-0000-0000-0000-000000000001"),"customer2"),
                Tuple.Create(new Guid("00000000-0000-0000-0000-000000000002"),"customer3"),
                Tuple.Create(new Guid("00000000-0000-0000-0000-000000000003"),"customer4"),
                Tuple.Create(new Guid("00000000-0000-0000-0000-000000000004"),"customer5")
            };

            // Create a list of products
            var products = new List<Tuple<string, int, Guid>>
            {
                Tuple.Create("apple", 5, new Guid("00000000-0000-0000-0000-000000000005")),
                Tuple.Create("pear", 10, new Guid("00000000-0000-0000-0000-000000000006")),
                Tuple.Create("banana", 20, new Guid("00000000-0000-0000-0000-000000000007")),
                Tuple.Create("orange", 2, new Guid("00000000-0000-0000-0000-000000000008")),
                Tuple.Create("mango", 100, new Guid("00000000-0000-0000-0000-000000000009"))
            };

            // Create 10 orders
            for (int i = 0; i < 10; i++)
            {
                // Generate a random customer ID
                var customer = customers[new Random().Next(0, customers.Count)];

                // Generate a random product name
                var product = products[new Random().Next(0, products.Count)];

                // Create an order
                var order = new Order
                {
                    OrderId = Guid.NewGuid(),
                    CustomerId = customer.Item1,
                    CustomerName = customer.Item2,
                    Total = 0,
                    OrderLines = Enumerable.Range(0, new Random().Next(0, 5)).Select(x =>
                    {
                        var product = products[new Random().Next(0, products.Count)];
                        return new OrderLine()
                        {
                            OrderLineId = Guid.NewGuid(),
                            Price = product.Item2,
                            ProductName = product.Item1,
                            ProductId = product.Item3,
                            Quantity= new Random().Next(1,5),
                        };
                    }).ToList()
                };

                // Add the order to the list
                orders.Add(order);
            }

            orders.ForEach(order =>
            {
                order.Total = order.CalculateOrderTotal();
            });
            return orders;
        }
    }
}
