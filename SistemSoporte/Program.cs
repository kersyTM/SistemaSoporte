using System.Text.Json.Serialization;
using Abstracciones.Interfaces;
using API.Comun;
using API.Constraints;
using DA;
using Flujo;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Permite enviar/recibir enums como string en JSON
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();

// Restricción de ruta personalizada
builder.Services.Configure<RouteOptions>(o =>
    o.ConstraintMap.Add("ticketCode", typeof(TicketCodigoRouteConstraint)));

// Inyección de dependencias
builder.Services.AddSingleton<ITicketDA, TicketDA>();
builder.Services.AddSingleton<IClienteDA, ClientesDA>();
builder.Services.AddScoped<ITicketFlujo, TicketFlujo>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers();

// ---------- Endpoints Map ----------
app.MapGet("/min/tickets/resumen", (ITicketFlujo flujo) =>
        Results.Ok(flujo.Resumen()))
    .WithName("ResumenTickets")
    .WithSummary("Resumen agregado: total, por estado y por prioridad")
    .Produces<Abstracciones.Models.ResumenResponse>(StatusCodes.Status200OK);

app.MapPost("/min/tickets/{id:int:min(1)}/reabrir", (int id, ITicketFlujo flujo) =>
        flujo.Reabrir(id).AResultadoHttp())
    .WithName("ReabrirTicket")
    .WithSummary("Reabre un ticket en estado Resuelto pasándolo a EnProceso")
    .Produces<Abstracciones.Models.TicketResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

app.Run();