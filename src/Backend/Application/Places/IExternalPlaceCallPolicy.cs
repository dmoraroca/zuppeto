namespace Zuppeto.Application.Places;

/// <summary>Decideix si el context actual pot executar crides facturables de llocs.</summary>
public interface IExternalPlaceCallPolicy
{
    bool AllowsBillableCalls { get; }
}

internal sealed class DisabledExternalPlaceCallsPolicy : IExternalPlaceCallPolicy
{
    public bool AllowsBillableCalls => false;
}
