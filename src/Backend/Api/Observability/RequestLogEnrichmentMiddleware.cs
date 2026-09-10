using System.Security.Claims;
using Serilog;

namespace Zuppeto.Api.Observability;

/// <summary>Completa el registre HTTP després que l'autenticació hagi resolt la identitat.</summary>
public sealed class RequestLogEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IDiagnosticContext diagnosticContext)
    {
        diagnosticContext.Set("CorrelationId", Item(context, "correlationId") ?? context.TraceIdentifier);
        diagnosticContext.Set("ZupTestCode", Item(context, "testCode") ?? "-");
        diagnosticContext.Set("TestRole", Item(context, "testRole") ?? "-");
        diagnosticContext.Set("TestBrowser", Item(context, "testBrowser") ?? "-");
        diagnosticContext.Set(
            "UserId",
            context.User.FindFirstValue("sub") ?? context.User.Identity?.Name ?? "anonymous");
        diagnosticContext.Set("AuthenticatedRole", context.User.FindFirstValue("role") ?? "anonymous");

        await next(context);
    }

    private static string? Item(HttpContext context, string key) => context.Items[key] as string;
}
