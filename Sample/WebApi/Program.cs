using Core;
using Core.Aggregates;
using Core.Aggregates.Order.Events;
using Sample.WebApi.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddEventStoreInMemory();
builder.Services.AddEventBusInMemory();

// Listeners - I have to refactor it
builder.Services.AddSendConfirmationEmailListener();
builder.Services.AddProjectToReadModelListener();
builder.Services.AddSendToKafkaTopicListener();

builder.Services.AddTransient<PlaceOrder>();

var app = builder.Build();

var bus = app.Services.GetRequiredService<IEventBus>();

foreach (var listener in app.Services.GetServices<IEventListener<OrderPlacedEvent>>())
{
    bus.SubscribeAsync(listener);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/orders", async (PlaceOrderRequest request, PlaceOrder handler) =>
{
    var response = await handler.Handle(request);
    return Results.Created($"/orders/{response.OrderId}", response);
})
.WithName("PlaceOrder");

app.UseHttpsRedirection();

app.Run();
