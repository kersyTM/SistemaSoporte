using System.Text.RegularExpressions;

namespace API.Constraints;

public class TicketCodigoRouteConstraint : IRouteConstraint
{
    // ⚠️ VARIANTE V=2: SP-AA-NNNN
    private static readonly Regex Patron = new(@"^SP-\d{2}-\d{3}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
                      RouteValueDictionary values, RouteDirection routeDirection)
        => values.TryGetValue(routeKey, out var v) && v is not null && Patron.IsMatch(v.ToString()!);
}