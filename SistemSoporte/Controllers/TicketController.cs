using Abstracciones.Interfaces;
using Abstracciones.Models;
using API.Comun;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
public class TicketsController : ControllerBase
{
    private readonly ITicketFlujo _flujo;
    public TicketsController(ITicketFlujo flujo) => _flujo = flujo;

    [HttpGet]
    [EndpointName("ListarTickets")]
    [EndpointSummary("Lista paginada de tickets con filtros")]
    [ProducesResponseType(typeof(ResultadoPaginado<TicketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Listar([FromQuery] TicketFiltro filtro)
    {
        var r = _flujo.Listar(filtro);
        if (!r.EsExito) return (IActionResult)r.AResultadoHttp();

        Response.Headers["X-Total-Count"] = r.Valor.Total.ToString();
        Response.Headers["X-Page"] = r.Valor.Pagina.ToString();
        Response.Headers["X-Page-Size"] = r.Valor.Tamano.ToString();

        return Ok(r.Valor);
    }

    [HttpGet("{id:int:min(1)}")]
    [EndpointName("ObtenerTicketPorId")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult ObtenerPorId(int id)
    {
        var r = _flujo.Obtener(id);
        if (!r.EsExito) return (IActionResult)r.AResultadoHttp();

        var etag = CalcularETag(r.Valor);
        Response.Headers.ETag = etag;

        if (Request.Headers.IfNoneMatch.Count > 0 &&
            Request.Headers.IfNoneMatch.Any(v => v == etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(r.Valor);
    }

    [HttpGet("codigo/{codigo:ticketCode}")]
    [EndpointName("ObtenerTicketPorCodigo")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult ObtenerPorCodigo(string codigo)
    {
        var r = _flujo.ObtenerPorCodigo(codigo);
        return r.EsExito ? Ok(r.Valor) : (IActionResult)r.AResultadoHttp();
    }

    [HttpPost]
    [EndpointName("CrearTicket")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Crear([FromBody] TicketCrearRequest request)
    {
        var r = _flujo.Crear(request);
        if (!r.EsExito) return (IActionResult)r.AResultadoHttp();

        return CreatedAtAction(nameof(ObtenerPorId),
            new { id = r.Valor.Id }, r.Valor);
    }

    [HttpPatch("{id:int:min(1)}/estado")]
    [EndpointName("CambiarEstadoTicket")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public IActionResult CambiarEstado(int id, [FromBody] TicketEstadoPatchRequest request)
    {
        var r = _flujo.CambiarEstado(id, request);
        return r.EsExito ? Ok(r.Valor) : (IActionResult)r.AResultadoHttp();
    }

    private static string CalcularETag(TicketResponse t)
        => $"W/\"ticket-{t.Id}-{t.FechaActualizacion.Ticks}\"";
}