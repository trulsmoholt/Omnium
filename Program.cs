using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
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

static async Task<IResult> GetOrders(IOrdreRepository repo)
{
    var list = await repo.GetOrders();
    if (list.Any())
    {
        return TypedResults.Ok(list);
    }
    else
    {
        return TypedResults.NoContent();
    }
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
