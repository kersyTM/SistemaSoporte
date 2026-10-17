using System.Text.RegularExpressions;

namespace API.Constraints;

public class TicketCodigoRouteConstraint : IRouteConstraint
{
    private static readonly Regex Patron = new(@"^TK-\d{4}-\d{4}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
                      RouteValueDictionary values, RouteDirection routeDirection)
        => values.TryGetValue(routeKey, out var v) && v is not null && Patron.IsMatch(v.ToString()!);
}