using System.Globalization;

namespace Lingo.Model.Formatting;

public enum PluralCategory
{
    Zero,
    One,
    Two,
    Few,
    Many,
    Other
}

public static class PluralRules
{
    public static PluralCategory SelectCardinal(double number, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var lang = culture.TwoLetterISOLanguageName.ToLowerInvariant();

        var abs = Math.Abs(number);
        var isInteger = Math.Abs(abs - Math.Truncate(abs)) < 0.0000000001;
        var n = isInteger ? (long)Math.Truncate(abs) : -1;

        switch (lang)
        {
            // East Asian languages: no plural inflection
            case "zh":
            case "ja":
            case "ko":
            case "vi":
            case "th":
            case "id":
            case "ms":
                return PluralCategory.Other;

            // French / Brazilian Portuguese: 0 and 1 are 'one'
            case "fr":
                return (abs >= 0 && abs < 2) ? PluralCategory.One : PluralCategory.Other;

            // Arabic
            case "ar":
                if (n == 0) return PluralCategory.Zero;
                if (n == 1) return PluralCategory.One;
                if (n == 2) return PluralCategory.Two;
                if (n % 100 >= 3 && n % 100 <= 10) return PluralCategory.Few;
                if (n % 100 >= 11 && n % 100 <= 99) return PluralCategory.Many;
                return PluralCategory.Other;

            // Russian, Ukrainian, Belarusian
            case "ru":
            case "uk":
            case "be":
                if (isInteger)
                {
                    var mod10 = n % 10;
                    var mod100 = n % 100;
                    if (mod10 == 1 && mod100 != 11) return PluralCategory.One;
                    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return PluralCategory.Few;
                    if (mod10 == 0 || (mod10 >= 5 && mod10 <= 9) || (mod100 >= 11 && mod100 <= 19)) return PluralCategory.Many;
                }
                return PluralCategory.Other;

            // Polish
            case "pl":
                if (isInteger)
                {
                    if (n == 1) return PluralCategory.One;
                    var mod10 = n % 10;
                    var mod100 = n % 100;
                    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return PluralCategory.Few;
                    if ((mod10 >= 0 && mod10 <= 1) || (mod10 >= 5 && mod10 <= 9) || (mod100 >= 12 && mod100 <= 14)) return PluralCategory.Many;
                }
                return PluralCategory.Other;

            // Czech, Slovak
            case "cs":
            case "sk":
                if (isInteger)
                {
                    if (n == 1) return PluralCategory.One;
                    if (n >= 2 && n <= 4) return PluralCategory.Few;
                }
                return PluralCategory.Other;

            // Germanic, English, Spanish, Italian, Dutch, Scandinavian, etc.
            // 1 is 'one', everything else is 'other'
            default:
                return (abs == 1.0) ? PluralCategory.One : PluralCategory.Other;
        }
    }

    public static PluralCategory SelectOrdinal(double number, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var lang = culture.TwoLetterISOLanguageName.ToLowerInvariant();

        var abs = Math.Abs(number);
        var isInteger = Math.Abs(abs - Math.Truncate(abs)) < 0.0000000001;
        if (!isInteger) return PluralCategory.Other;

        var n = (long)Math.Truncate(abs);

        switch (lang)
        {
            case "en":
                var mod10 = n % 10;
                var mod100 = n % 100;
                if (mod10 == 1 && mod100 != 11) return PluralCategory.One; // 1st, 21st, 31st...
                if (mod10 == 2 && mod100 != 12) return PluralCategory.Two; // 2nd, 22nd, 32nd...
                if (mod10 == 3 && mod100 != 13) return PluralCategory.Few; // 3rd, 23rd, 33rd...
                return PluralCategory.Other;

            case "fr":
                return (n == 1) ? PluralCategory.One : PluralCategory.Other; // 1er, 2e...

            default:
                var defMod10 = n % 10;
                var defMod100 = n % 100;
                if (defMod10 == 1 && defMod100 != 11) return PluralCategory.One;
                if (defMod10 == 2 && defMod100 != 12) return PluralCategory.Two;
                if (defMod10 == 3 && defMod100 != 13) return PluralCategory.Few;
                return PluralCategory.Other;
        }
    }
}
