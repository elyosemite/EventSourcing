using Core;
using Core.Aggregates;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddEventStoreInMemory();
builder.Services.AddEventBusInMemory();

var app = builder.Build();

var bus = app.Services.GetRequiredService<IEventBus>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
