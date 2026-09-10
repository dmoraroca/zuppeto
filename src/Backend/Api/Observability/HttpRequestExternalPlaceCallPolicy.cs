using Zuppeto.Application.Places;

namespace Zuppeto.Api.Observability;

/// <summary>Impedeix que una execució E2E consumeixi proveïdors externs facturables.</summary>
public sealed class HttpRequestExternalPlaceCallPolicy(IHttpContextAccessor httpContextAccessor)
    : IExternalPlaceCallPolicy
{
    public bool AllowsBillableCalls
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            return context is null
                || string.IsNullOrWhiteSpace(
                    context.Request.Headers[RequestCorrelationMiddleware.TestBrowserHeader].ToString());
        }
    }
}
