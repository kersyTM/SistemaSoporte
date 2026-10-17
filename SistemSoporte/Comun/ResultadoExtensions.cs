using Abstracciones.Resultados;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API.Comun;

public static class ResultadoExtensions
{
    public static IResult AResultadoHttp<T>(this Result<T> r)
    {
        if (r.EsExito) return Results.Ok(r.Valor);

        var status = r.TipoError switch
        {
            TipoError.Validacion => StatusCodes.Status400BadRequest,
            TipoError.NoEncontrado => StatusCodes.Status404NotFound,
            TipoError.Conflicto => StatusCodes.Status409Conflict,
            TipoError.NoProcesable => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        if (r.Errores is not null)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>(r.Errores),
                detail: r.Detalle, title: r.Titulo, statusCode: status);

        return Results.Problem(title: r.Titulo, detail: r.Detalle, statusCode: status);
    }
}