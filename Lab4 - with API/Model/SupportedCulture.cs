namespace Lab3.Model;

/// Supported cultures for multi-language book support
public enum SupportedCulture
{
    EnglishUS,      // en-US (default)
    Spanish,        // es
    French,         // fr
    German,         // de
    Japanese        // ja
}

/// Extension methods for SupportedCulture
public static class SupportedCultureExtensions
{
    public static string ToCode(this SupportedCulture culture)
    {
        return culture switch
        {
            SupportedCulture.EnglishUS => "en-US",
            SupportedCulture.Spanish => "es",
            SupportedCulture.French => "fr",
            SupportedCulture.German => "de",
            SupportedCulture.Japanese => "ja",
            _ => "en-US"
        };
    }

    public static SupportedCulture FromCode(string code)
    {
        return code?.ToLower() switch
        {
            "es" or "es-es" or "es-mx" => SupportedCulture.Spanish,
            "fr" or "fr-fr" or "fr-ca" => SupportedCulture.French,
            "de" or "de-de" or "de-at" => SupportedCulture.German,
            "ja" or "ja-jp" => SupportedCulture.Japanese,
            _ => SupportedCulture.EnglishUS
        };
    }
}
