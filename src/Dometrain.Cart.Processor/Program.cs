
using Dometrain.Cart.Processor;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<CartProcessorService>();
builder.AddAzureCosmosClient("carts");

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
