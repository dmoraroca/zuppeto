using Serilog.Context;

namespace Zuppeto.Api.Observability;

/// <summary>
/// Assigna una correlació segura a cada petició i conserva el context funcional
/// enviat per les execucions E2E.
/// </summary>
public sealed class RequestCorrelationMiddleware(RequestDelegate next)
{
    public const string CorrelationHeader = "X-Correlation-ID";
    public const string TestCodeHeader = "X-Zuppeto-Test-Code";
    public const string TestRoleHeader = "X-Zuppeto-Test-Role";
    public const string TestBrowserHeader = "X-Zuppeto-Test-Browser";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ReadSafeHeader(context, CorrelationHeader, 80)
            ?? context.TraceIdentifier;
        var testCode = ReadSafeHeader(context, TestCodeHeader, 32);
        var testRole = ReadSafeHeader(context, TestRoleHeader, 32);
        var testBrowser = ReadSafeHeader(context, TestBrowserHeader, 32);

        context.Items[nameof(correlationId)] = correlationId;
        context.Items[nameof(testCode)] = testCode;
        context.Items[nameof(testRole)] = testRole;
        context.Items[nameof(testBrowser)] = testBrowser;
        context.Response.Headers[CorrelationHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("ZupTestCode", testCode ?? "-"))
        using (LogContext.PushProperty("TestRole", testRole ?? "-"))
        using (LogContext.PushProperty("TestBrowser", testBrowser ?? "-"))
        {
            await next(context);
        }
    }

    private static string? ReadSafeHeader(HttpContext context, string name, int maximumLength)
    {
        var value = context.Request.Headers[name].ToString().Trim();
        if (value.Length == 0 || value.Length > maximumLength)
        {
            return null;
        }

        return value.All(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' or '.' or ':')
            ? value
            : null;
    }
}
