using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Omnium;

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
    app.UseSwaggerUI();
}

app.MapGet("/order", GetOrders);

app.MapGet("/order/{id}", GetOrder);

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




app.MapPost("/order", async (IOrdreRepository repo, OrderDTO orderDto) =>
{
    var order = await repo.PostOrder(orderDto);
    return TypedResults.Created($"/order/{order.OrderId}",order);
});

app.MapPost("/mockOrders", async (IOrdreRepository repo) =>
{
    await repo.PostMockOrders();
    return TypedResults.NoContent();
}); 


app.Run();
