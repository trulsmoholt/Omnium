using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Omnium;
using Omnium.models;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<CosmosConfiguration>(
    builder.Configuration.GetSection(CosmosConfiguration.MyConfig)
);
builder.Services.AddScoped<IOrdreRepository, OrderRepository>();

var app = builder.Build();

using CosmosClient client = new(
    accountEndpoint: builder.Configuration["cosmos:url"]!,
    authKeyOrResourceToken: builder.Configuration["cosmos:key"]!
);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();//naviger til /swagger for å se dokumentasjon
}

var orderController = app.MapGroup("/order");

orderController.MapGet("/", GetOrders).WithOpenApi();

orderController.MapGet("/{id}", GetOrder);

orderController.MapPost("/", PostOrder);

app.MapGet("/topSellingProducts", GetTopSellingProducts);
static async Task<IResult> GetTopSellingProducts(IOrdreRepository repo)
{
    var topSellingDict = new Dictionary<string, double>();

    var orders = await repo.GetOrders();

    foreach(Order order in orders)
    {
        order.OrderLines.ForEach(purchase =>
        {
            if (topSellingDict.TryGetValue(purchase.ProductName,out double total)) {
                topSellingDict[purchase.ProductName] = total + (purchase.Price*purchase.Quantity);
            }
            else
            {
                topSellingDict.Add(purchase.ProductName, purchase.Quantity * purchase.Price);
            }
        });
    }

    return TypedResults.Ok(topSellingDict.OrderBy(x => -x.Value).Take(5).ToDictionary(x => x.Key, x=> x.Value));
}

static async Task<IResult> GetOrder(string id, IOrdreRepository repo)
{
    var order = await repo.GetOrder(id);
    if(order != null)
    {
        return TypedResults.Ok(order);
    }
    return TypedResults.NotFound();
}

static async Task<IResult> GetOrders(IOrdreRepository repo, string? customerId, string? productId)
{
    if(customerId != null && productId != null)
    {
        var customerFilteredOrders = await repo.GetOrdersByCustomerId(customerId);
        var productFilteredOrders = await repo.GetOrdersByProductId(productId);
        var result = customerFilteredOrders.Intersect(productFilteredOrders).ToList();
        return ReturnList(result);
    }
    else if(customerId != null) 
    {
        var result = await repo.GetOrdersByCustomerId(customerId);
        return ReturnList(result);
    }
    else if(productId != null)
    {
        var result = await repo.GetOrdersByProductId(productId);
        return ReturnList(result);
    }
    var list = await repo.GetOrders();

    AddPosOrder(list);

    return ReturnList(list);
}

static IResult ReturnList(IEnumerable<Order> list)
{
    if (list.Any())
    {
        return TypedResults.Ok(list);
    }
    return TypedResults.NotFound();
}

static async Task<IResult> PostOrder(IOrdreRepository repo, OrderDTO orderDto)
{
    var order = await repo.PostOrder(orderDto);
    return TypedResults.Created($"/order/{order.OrderId}", order);
}

app.MapPost("/mockOrders", async (IOrdreRepository repo) =>
{
    await repo.PostMockOrders();
    return TypedResults.NoContent();
}); 

static void AddPosOrder(List<Order> list)//eksempel på at man kan gjøre Order ting med en PosOrder
{
    var posOrder = new PosOrder()
    {
        OrderId = Guid.NewGuid(),
        PosId = Guid.NewGuid(),
        OrderLines = new OrderLine[]
    {
            new OrderLine() {
                OrderLineId= Guid.NewGuid(),
                ProductId= Guid.NewGuid(),
                Price=12,
                ProductName="strawberry",
                Quantity=1
            }
    }.ToList(),
        CustomerId = Guid.NewGuid(),
        CustomerName = "Truls",
    };

    posOrder.Total = posOrder.CalculateOrderTotal();

    list.Add(posOrder);
}

app.Run();
