namespace Abstracciones.Modelos
{
    public enum PrioridadTicket { Baja, Media, Alta, Critica }
    public enum EstadoTicket { Abierto, EnProceso, Resuelto, Cerrado }
    public class Ticket
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public PrioridadTicket Prioridad { get; set; }
        public EstadoTicket Estado { get; set; }
        public int ClienteId { get; set; }
        public int? AgenteId { get; set; }
        public int Reaperturas { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}