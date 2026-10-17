using Abstracciones.Modelos;
using System.ComponentModel.DataAnnotations;

namespace Abstracciones.Models;

public class TicketCrearRequest
{
    [Required, StringLength(120, MinimumLength = 3)]
    public string Titulo { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 5)]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    public PrioridadTicket? Prioridad { get; set; }

    [Range(1, int.MaxValue)]
    public int ClienteId { get; set; }
}

public class TicketEstadoPatchRequest
{
    [Required]
    public EstadoTicket? Estado { get; set; }     // obligatorio
    public int? AgenteId { get; set; }            // opcional
    public PrioridadTicket? Prioridad { get; set; } // opcional
}

public class TicketResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Prioridad { get; set; } = "";
    public string Estado { get; set; } = "";
    public int ClienteId { get; set; }
    public int? AgenteId { get; set; }
    public int Reaperturas { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class TicketFiltro
{
    public EstadoTicket? Estado { get; set; }
    public PrioridadTicket? Prioridad { get; set; }
    public int? ClienteId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Q { get; set; }
    public string? Orden { get; set; } = "-fecha";
    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 10;
}

public class ResultadoPaginado<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamano { get; set; }
}

public class ResumenResponse
{
    public int Total { get; set; }
    public Dictionary<string, int> PorEstado { get; set; } = new();
    public Dictionary<string, int> PorPrioridad { get; set; } = new();
}