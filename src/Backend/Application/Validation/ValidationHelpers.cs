using System.Diagnostics.CodeAnalysis;

namespace YepPet.Application.Validation;

public static class ValidationHelpers
{
    public static bool TryParseEnum<TEnum>(string? value, [NotNullWhen(true)] out TEnum? parsed)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsed = null;
            return false;
        }

        return Enum.TryParse(value, ignoreCase: true, out parsed);
    }
}
