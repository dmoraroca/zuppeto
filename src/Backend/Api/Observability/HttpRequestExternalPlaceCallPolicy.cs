using Zuppeto.Application.Places;

namespace Zuppeto.Api.Observability;

/// <summary>
/// Política HTTP heretada conservada per a l'auditoria de la Fase I.
/// Google Places està desactivat per a qualsevol tipus de petició.
/// </summary>
public sealed class HttpRequestExternalPlaceCallPolicy
    : IExternalPlaceCallPolicy
{
    public bool AllowsBillableCalls => false;
}
