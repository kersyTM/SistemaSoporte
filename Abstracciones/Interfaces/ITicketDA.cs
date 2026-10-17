using Abstracciones.Modelos;
using Abstracciones.Models;
using Abstracciones.Resultados;

namespace Abstracciones.Interfaces;

public interface ITicketDA
{
    IReadOnlyList<Ticket> ObtenerTodos();
    Ticket? ObtenerPorId(int id);
    Ticket? ObtenerPorCodigo(string codigo);
    Ticket Agregar(Ticket ticket);
    void Actualizar(Ticket ticket);
    string GenerarCodigo();
}

public interface IClienteDA
{
    bool Existe(int id);
}

public interface ITicketFlujo
{
    Result<ResultadoPaginado<TicketResponse>> Listar(TicketFiltro filtro);
    Result<TicketResponse> Obtener(int id);
    Result<TicketResponse> ObtenerPorCodigo(string codigo);
    Result<TicketResponse> Crear(TicketCrearRequest request);
    Result<TicketResponse> CambiarEstado(int id, TicketEstadoPatchRequest request);
    Result<TicketResponse> Reabrir(int id);
    ResumenResponse Resumen();
}