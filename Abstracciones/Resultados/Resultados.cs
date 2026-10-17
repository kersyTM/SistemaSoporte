namespace Abstracciones.Resultados;

public enum TipoError { Validacion, NoEncontrado, Conflicto, NoProcesable }

public class Result<T>
{
    public bool EsExito { get; init; }
    public T Valor { get; init; } = default!;
    public TipoError? TipoError { get; init; }
    public string? Titulo { get; init; }
    public string? Detalle { get; init; }
    public IDictionary<string, string[]>? Errores { get; init; }

    public static Result<T> Ok(T valor) => new() { EsExito = true, Valor = valor };

    public static Result<T> Validacion(string detalle, IDictionary<string, string[]>? errores = null)
        => new() { TipoError = Resultados.TipoError.Validacion, Titulo = "Solicitud inválida", Detalle = detalle, Errores = errores };

    public static Result<T> NoEncontrado(string detalle)
        => new() { TipoError = Resultados.TipoError.NoEncontrado, Titulo = "Recurso no encontrado", Detalle = detalle };

    public static Result<T> Conflicto(string detalle)
        => new() { TipoError = Resultados.TipoError.Conflicto, Titulo = "Conflicto con el estado actual", Detalle = detalle };

    public static Result<T> NoProcesable(string detalle, IDictionary<string, string[]>? errores = null)
        => new() { TipoError = Resultados.TipoError.NoProcesable, Titulo = "Entidad no procesable", Detalle = detalle, Errores = errores };
}