using Abstracciones.Interfaces;
using Abstracciones.Modelos;
using Abstracciones.Models;

namespace DA;

public class TicketDA : ITicketDA
{
    private readonly List<Ticket> _tickets = new();
    private readonly object _lock = new();
    private int _ultimoId;

    public TicketDA() => Seed();

    public IReadOnlyList<Ticket> ObtenerTodos() { lock (_lock) return _tickets.ToList(); }
    public Ticket? ObtenerPorId(int id) { lock (_lock) return _tickets.FirstOrDefault(t => t.Id == id); }
    public Ticket? ObtenerPorCodigo(string codigo)
    {
        lock (_lock)
            return _tickets.FirstOrDefault(t => t.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));
    }

    public Ticket Agregar(Ticket ticket)
    {
        lock (_lock) { ticket.Id = ++_ultimoId; _tickets.Add(ticket); return ticket; }
    }

    public void Actualizar(Ticket ticket)
    {
        lock (_lock)
        {
            var idx = _tickets.FindIndex(t => t.Id == ticket.Id);
            if (idx >= 0) _tickets[idx] = ticket;
        }
    }


    public string GenerarCodigo()
    {
        lock (_lock)
        {
            var yy = DateTime.UtcNow.Year % 100;
            var prefijo = $"SP-{yy:D2}-";
            var max = _tickets
                .Where(t => t.Codigo.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)
                            && t.Codigo.Length == prefijo.Length + 3)
                .Select(t => int.TryParse(t.Codigo[^3..], out var n) ? n : 0)
                .DefaultIfEmpty(0).Max();
            return $"{prefijo}{max + 1:D3}";
        }
    }

    private void Seed()
    {
        // 26 títulos distintos
        var titulos = new[]
        {
            "No puedo iniciar sesión", "Error al exportar reporte", "Correo no llega al destinatario",
            "Impresora sin conexión", "Solicitud de nueva licencia", "Lentitud en el sistema",
            "Pantalla en blanco al abrir módulo", "Error 500 al guardar formulario",
            "Recuperación de contraseña", "Alta de usuario nuevo", "Baja de usuario",
            "Cambio de permisos", "VPN no conecta", "Base de datos caída",
            "Error al adjuntar archivos", "Módulo de facturación falla",
            "Notificaciones duplicadas", "Sincronización con ERP", "Respaldo no se ejecuta",
            "Acceso denegado a carpeta compartida", "Correo marcado como spam",
            "Aplicación móvil no actualiza", "Reporte descuadrado", "Timeout en consultas",
            "API externa no responde", "Certificado SSL vencido"
        };


        var estadosPorCliente = new Dictionary<int, EstadoTicket[]>
        {
            [1] = new[] { EstadoTicket.Abierto,   EstadoTicket.EnProceso, EstadoTicket.EnProceso,
                          EstadoTicket.Resuelto,  EstadoTicket.Resuelto,  EstadoTicket.Cerrado },
            [2] = new[] { EstadoTicket.Abierto,   EstadoTicket.EnProceso, EstadoTicket.EnProceso,
                          EstadoTicket.Resuelto,  EstadoTicket.Cerrado },
            [3] = new[] { EstadoTicket.Abierto,   EstadoTicket.EnProceso, EstadoTicket.EnProceso,
                          EstadoTicket.Resuelto,  EstadoTicket.Resuelto },
            [4] = new[] { EstadoTicket.Abierto,   EstadoTicket.EnProceso, EstadoTicket.EnProceso,
                          EstadoTicket.Cerrado,   EstadoTicket.Cerrado },
            [5] = new[] { EstadoTicket.Abierto,   EstadoTicket.EnProceso, EstadoTicket.EnProceso,
                          EstadoTicket.Resuelto,  EstadoTicket.Cerrado }
        };

        var rand = new Random(20260217);   
        var ahora = DateTime.UtcNow;
        var yy = ahora.Year % 100;
        int i = 0;

        foreach (var kvp in estadosPorCliente)
        {
            foreach (var estado in kvp.Value)
            {
                var creado = ahora.AddDays(-rand.Next(0, 60)).AddHours(-rand.Next(0, 24));
                _tickets.Add(new Ticket
                {
                    Id = ++_ultimoId,
                    Codigo = $"SP-{yy:D2}-{_ultimoId:D3}",
                    Titulo = $"{titulos[i % titulos.Length]} #{_ultimoId}",
                    Description = "Ticket generado automáticamente para pruebas de paginación.",
                    Prioridad = (PrioridadTicket)rand.Next(0, 4),
                    Estado = estado,
                    ClienteId = kvp.Key,
                    AgenteId = estado == EstadoTicket.Abierto ? null : rand.Next(1, 4),
                    Reaperturas = 0,
                    FechaCreacion = creado,
                    FechaActualizacion = creado
                });
                i++;
            }
        }
    }
}