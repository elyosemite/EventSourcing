using Core;
using Core.Aggregates;
using Core.Observability.Logging;
using EventSourcing.WebApi.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddObservability();

builder.Services.AddEventStoreInMemory();
builder.Services.AddEventBusInMemory();

builder.Services.AddScoped<InitPayment>();
builder.Services.AddScoped<AuthorizePayment>();
builder.Services.AddScoped<CapturePayment>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/payments", async (Command command, InitPayment handler, CancellationToken ct) =>
{
    var id = await handler.HandleAsync(command, ct);
    return Results.Created($"/payments/{id}", new { id });
});

app.MapPost("/payments/{id}/authorize", async (
    Guid id,
    AuthorizePaymentBody body,
    AuthorizePayment handler,
    CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(new AuthorizePaymentCommand(id, body.CreditCardNumber, body.CreditCardCode), ct);
        return Results.Ok(result);
    }
    catch (KeyNotFoundException e)      { return Results.NotFound(new { e.Message }); }
    catch (InvalidOperationException e) { return Results.Conflict(new { e.Message }); }
});

app.MapPost("/payments/{id}/capture", async (Guid id, CapturePayment handler, CancellationToken ct) =>
{
    await handler.HandleAsync(new CapturePaymentCommand(id), ct);
    return Results.Ok();
});

app.Run();
