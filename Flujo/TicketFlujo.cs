using Abstracciones.Interfaces;
using Abstracciones.Modelos;
using Abstracciones.Models;
using Abstracciones.Resultados;

namespace Flujo;

public class TicketFlujo : ITicketFlujo
{
    private const int TamanoMaximoPagina = 30;
    private const int TamanoPaginaDefecto = 10;

    private const int MaxTicketsActivosPorCliente = 3;

    private readonly ITicketDA _ticketDA;
    private readonly IClienteDA _clienteDA;

    public TicketFlujo(ITicketDA ticketDA, IClienteDA clienteDA)
    {
        _ticketDA = ticketDA;
        _clienteDA = clienteDA;
    }

    // ---------- GET /api/tickets ----------
    public Result<ResultadoPaginado<TicketResponse>> Listar(TicketFiltro f)
    {
        var errores = new Dictionary<string, string[]>();
        if (f.Pagina < 1) errores["pagina"] = ["Debe ser mayor o igual a 1."];
        if (f.Tamano < 1) errores["tamano"] = ["Debe ser mayor o igual a 1."];
        if (f.Tamano > TamanoMaximoPagina)
            errores["tamano"] = [$"El tamaño máximo permitido es {TamanoMaximoPagina}."];
        if (f.Desde.HasValue && f.Hasta.HasValue && f.Desde > f.Hasta)
            errores["desde"] = ["'desde' no puede ser posterior a 'hasta'."];

        var ordenesValidos = new[] { "fecha", "-fecha", "prioridad", "-prioridad" };
        if (!string.IsNullOrWhiteSpace(f.Orden) && !ordenesValidos.Contains(f.Orden))
            errores["orden"] = [$"Valores permitidos: {string.Join(", ", ordenesValidos)}."];

        if (errores.Count > 0)
            return Result<ResultadoPaginado<TicketResponse>>.Validacion("Uno o más parámetros son inválidos.", errores);

        IEnumerable<Ticket> q = _ticketDA.ObtenerTodos();
        if (f.Estado.HasValue) q = q.Where(t => t.Estado == f.Estado);
        if (f.Prioridad.HasValue) q = q.Where(t => t.Prioridad == f.Prioridad);
        if (f.ClienteId.HasValue) q = q.Where(t => t.ClienteId == f.ClienteId);
        if (f.Desde.HasValue) q = q.Where(t => t.FechaCreacion >= f.Desde.Value);
        if (f.Hasta.HasValue) q = q.Where(t => t.FechaCreacion <= f.Hasta.Value);
        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            var term = f.Q.Trim();
            q = q.Where(t =>
                t.Titulo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.Descripcion.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        q = (f.Orden ?? "-fecha") switch
        {
            "fecha" => q.OrderBy(t => t.FechaCreacion),
            "-fecha" => q.OrderByDescending(t => t.FechaCreacion),
            "prioridad" => q.OrderBy(t => t.Prioridad),
            "-prioridad" => q.OrderByDescending(t => t.Prioridad),
            _ => q.OrderByDescending(t => t.FechaCreacion)
        };

        var total = q.Count();
        var items = q.Skip((f.Pagina - 1) * f.Tamano).Take(f.Tamano).Select(Map).ToList();

        return Result<ResultadoPaginado<TicketResponse>>.Ok(new ResultadoPaginado<TicketResponse>
        {
            Items = items,
            Total = total,
            Pagina = f.Pagina,
            Tamano = f.Tamano
        });
    }

    public Result<TicketResponse> Obtener(int id)
    {
        var t = _ticketDA.ObtenerPorId(id);
        return t is null
            ? Result<TicketResponse>.NoEncontrado($"No existe un ticket con id {id}.")
            : Result<TicketResponse>.Ok(Map(t));
    }

    public Result<TicketResponse> ObtenerPorCodigo(string codigo)
    {
        var t = _ticketDA.ObtenerPorCodigo(codigo);
        return t is null
            ? Result<TicketResponse>.NoEncontrado($"No existe un ticket con código {codigo}.")
            : Result<TicketResponse>.Ok(Map(t));
    }

    // ---------- POST /api/tickets ----------
    public Result<TicketResponse> Crear(TicketCrearRequest req)
    {
        if (!_clienteDA.Existe(req.ClienteId))
            return Result<TicketResponse>.NoProcesable(
                $"El cliente {req.ClienteId} no existe.",
                new Dictionary<string, string[]> { ["clienteId"] = [$"Cliente {req.ClienteId} inexistente."] });

        var conflicto = VerificarLimiteActivos(req.ClienteId, null, EstadoTicket.Abierto);
        if (conflicto is not null) return conflicto;

        var ahora = DateTime.UtcNow;
        var t = new Ticket
        {
            Codigo = _ticketDA.GenerarCodigo(),
            Titulo = req.Titulo.Trim(),
            Descripcion = req.Descripcion.Trim(),
            Prioridad = req.Prioridad!.Value,
            Estado = EstadoTicket.Abierto,
            ClienteId = req.ClienteId,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = ahora,
            FechaActualizacion = ahora
        };

        _ticketDA.Agregar(t);
        return Result<TicketResponse>.Ok(Map(t));
    }

    // ---------- PATCH /api/tickets/{id}/estado ----------
    public Result<TicketResponse> CambiarEstado(int id, TicketEstadoPatchRequest req)
    {
        var t = _ticketDA.ObtenerPorId(id);
        if (t is null) return Result<TicketResponse>.NoEncontrado($"No existe un ticket con id {id}.");

        var nuevoEstado = req.Estado!.Value;

        if (!TransicionValida(t.Estado, nuevoEstado))
            return Result<TicketResponse>.Conflicto(
                $"Transición inválida: {t.Estado} → {nuevoEstado}.");

        var conflicto = VerificarLimiteActivos(t.ClienteId, id, nuevoEstado);
        if (conflicto is not null) return conflicto;

        t.Estado = nuevoEstado;
        t.AgenteId = req.AgenteId ?? t.AgenteId;
        t.Prioridad = req.Prioridad ?? t.Prioridad;
        t.FechaActualizacion = DateTime.UtcNow;
        _ticketDA.Actualizar(t);

        return Result<TicketResponse>.Ok(Map(t));
    }

    public Result<TicketResponse> Reabrir(int id)
    {
        var t = _ticketDA.ObtenerPorId(id);
        if (t is null) return Result<TicketResponse>.NoEncontrado($"No existe un ticket con id {id}.");
        if (t.Estado != EstadoTicket.Resuelto)
            return Result<TicketResponse>.Conflicto("Solo se pueden reabrir tickets en estado Resuelto.");

        var conflicto = VerificarLimiteActivos(t.ClienteId, id, EstadoTicket.EnProceso);
        if (conflicto is not null) return conflicto;

        t.Estado = EstadoTicket.EnProceso;
        t.Reaperturas++;
        t.FechaActualizacion = DateTime.UtcNow;
        _ticketDA.Actualizar(t);
        return Result<TicketResponse>.Ok(Map(t));
    }

    public ResumenResponse Resumen()
    {
        var todos = _ticketDA.ObtenerTodos();
        return new ResumenResponse
        {
            Total = todos.Count,
            PorEstado = todos.GroupBy(t => t.Estado).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            PorPrioridad = todos.GroupBy(t => t.Prioridad).ToDictionary(g => g.Key.ToString(), g => g.Count())
        };
    }

    // ---------- Helpers ----------
    private static bool TransicionValida(EstadoTicket actual, EstadoTicket nuevo) => (actual, nuevo) switch
    {
        (EstadoTicket.Abierto, EstadoTicket.EnProceso) => true,
        (EstadoTicket.EnProceso, EstadoTicket.Resuelto) => true,
        (EstadoTicket.Resuelto, EstadoTicket.Cerrado) => true,
        (EstadoTicket.Resuelto, EstadoTicket.EnProceso) => true,   
        _ => false
    };

   
    private Result<TicketResponse>? VerificarLimiteActivos(int clienteId, int? excluirTicketId, EstadoTicket nuevoEstado)
    {
        if (nuevoEstado != EstadoTicket.Abierto && nuevoEstado != EstadoTicket.EnProceso)
            return null;

        var activos = _ticketDA.ObtenerTodos()
            .Count(t => t.ClienteId == clienteId
                        && t.Id != (excluirTicketId ?? 0)
                        && (t.Estado == EstadoTicket.Abierto || t.Estado == EstadoTicket.EnProceso));

        if (activos >= MaxTicketsActivosPorCliente)
            return Result<TicketResponse>.Conflicto(
                $"El cliente {clienteId} ya tiene {activos} tickets en estado Abierto o EnProceso. " +
                $"El máximo permitido es {MaxTicketsActivosPorCliente}.");

        return null;
    }

    private static TicketResponse Map(Ticket t) => new()
    {
        Id = t.Id,
        Codigo = t.Codigo,
        Titulo = t.Titulo,
        Descripcion = t.Descripcion,
        Prioridad = t.Prioridad.ToString(),
        Estado = t.Estado.ToString(),
        ClienteId = t.ClienteId,
        AgenteId = t.AgenteId,
        Reaperturas = t.Reaperturas,
        FechaCreacion = t.FechaCreacion,
        FechaActualizacion = t.FechaActualizacion
    };
}