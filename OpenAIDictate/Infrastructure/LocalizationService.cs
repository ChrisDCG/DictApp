using System;
using System.Globalization;
using System.Linq;

namespace OpenAIDictate.Infrastructure;

/// <summary>
/// Centralizes application-wide localization concerns (UI culture and supported locales).
/// </summary>
public static class LocalizationService
{
    private static readonly CultureInfo[] Supported =
    {
        new("en-US"),
        new("de-DE"),
        new("es-ES"),
        new("fr-FR")
    };

    /// <summary>
    /// Gets the cultures that are fully supported by the UI.
    /// </summary>
    public static IReadOnlyList<CultureInfo> SupportedCultures => Supported;

    /// <summary>
    /// Applies the requested culture (falls back to English if not available).
    /// </summary>
    public static void ApplyCulture(string? cultureName)
    {
        var fallback = new CultureInfo("en-US");
        CultureInfo culture;

        if (string.IsNullOrWhiteSpace(cultureName))
        {
            culture = fallback;
        }
        else
        {
            try
            {
                culture = CultureInfo.GetCultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                culture = fallback;
            }
        }

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    /// <summary>
    /// Finds the best matching supported culture for a persisted configuration value.
    /// </summary>
    public static CultureInfo ResolveCulture(string? storedCulture)
    {
        if (!string.IsNullOrWhiteSpace(storedCulture))
        {
            try
            {
                var requested = CultureInfo.GetCultureInfo(storedCulture);
                var exact = Supported.FirstOrDefault(c => c.Name.Equals(requested.Name, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    return exact;
                }

                var neutral = Supported.FirstOrDefault(c =>
                    c.TwoLetterISOLanguageName.Equals(requested.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
                if (neutral != null)
                {
                    return neutral;
                }
            }
            catch (CultureNotFoundException)
            {
                // Ignore and fall back below
            }
        }

        return Supported[0];
    }

    /// <summary>
    /// Returns a human friendly label for UI drop-downs (native name + culture code).
    /// </summary>
    public static string GetDisplayName(CultureInfo culture)
        => $"{culture.NativeName} ({culture.Name})";
}
