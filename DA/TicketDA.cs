using Abstracciones.Interfaces;
using Abstracciones.Modelos;
using Abstracciones.Models;
using System.Net.Sockets;

namespace DA;

public class TicketDA : ITicketDA
{
    private readonly List<Ticket> _tickets = new();
    private readonly object _lock = new();
    private int _ultimoId;

    public TicketDA() => Seed();

    public IReadOnlyList<Ticket> ObtenerTodos() { lock (_lock) return _tickets.ToList(); }

    public Ticket? ObtenerPorId(int id) { lock (_lock) return _tickets.FirstOrDefault(t => t.Id == id); }

    public Ticket? ObtenerPorCodigo(string codigo) { lock (_lock) return _tickets.FirstOrDefault(t => t.Codigo == codigo); }

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
            var anio = DateTime.UtcNow.Year;
            var prefijo = $"TK-{anio}-";
            var max = _tickets
                .Where(t => t.Codigo.StartsWith(prefijo) && t.Codigo.Length == prefijo.Length + 4)
                .Select(t => int.TryParse(t.Codigo[^4..], out var n) ? n : 0)
                .DefaultIfEmpty(0).Max();
            return $"{prefijo}{max + 1:D4}";
        }
    }

    private void Seed()
    {
        var titulos = new[] {
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

        var rand = new Random(20260101);
        var ahora = DateTime.UtcNow;

        for (int i = 0; i < 26; i++)
        {
            var creado = ahora.AddDays(-rand.Next(0, 60)).AddHours(-rand.Next(0, 24));
            _tickets.Add(new Ticket
            {
                Id = ++_ultimoId,
                Codigo = $"TK-{ahora.Year}-{(i + 1):D4}",
                Titulo = $"{titulos[i % titulos.Length]} #{i + 1}",
                Descripcion = "Ticket generado automáticamente para pruebas de paginación.",
                Prioridad = (PrioridadTicket)rand.Next(0, 4),
                Estado = (EstadoTicket)rand.Next(0, 4),
                ClienteId = rand.Next(1, 6),
                AgenteId = rand.Next(0, 2) == 0 ? null : rand.Next(1, 4),
                Reaperturas = rand.Next(0, 2),
                FechaCreacion = creado,
                FechaActualizacion = creado
            });
        }
    }
}