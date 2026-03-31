using System.Globalization;
using System.Text.RegularExpressions;

namespace ToDoapp.Services;

public enum HolidayDateRelation
{
    OnHolidayStart = 0,
    BeforeHoliday = 1,
    AfterHoliday = 2
}

public sealed record HolidayRange(string CanonicalName, DateTime StartDate, DateTime EndDate, IReadOnlyList<string> Aliases);

internal sealed record HolidayDefinition(string CanonicalName, IReadOnlyList<string> Aliases, Func<int, DateTime?> AnchorDateFactory);

public static class HolidayCatalog
{
    private static readonly ChineseLunisolarCalendar LunarCalendar = new();
    private static readonly IReadOnlyList<HolidayDefinition> Definitions =
    [
        new("元旦", ["元旦", "新年"], year => new DateTime(year, 1, 1)),
        new("春节", ["春节", "过年"], year => TryCreateLunarDate(year, 1, 1)),
        new("清明节", ["清明节", "清明"], TryCreateQingmingDate),
        new("劳动节", ["劳动节", "五一节", "五一"], year => new DateTime(year, 5, 1)),
        new("端午节", ["端午节", "端午"], year => TryCreateLunarDate(year, 5, 5)),
        new("中秋节", ["中秋节", "中秋"], year => TryCreateLunarDate(year, 8, 15)),
        new("国庆节", ["国庆节", "国庆"], year => new DateTime(year, 10, 1))
    ];

    private static readonly Dictionary<string, HolidayDefinition> AliasLookup =
        Definitions
            .SelectMany(definition => definition.Aliases.Select(alias => new KeyValuePair<string, HolidayDefinition>(alias, definition)))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<string> OrderedAliases =
        AliasLookup.Keys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(alias => alias.Length)
            .ToArray();

    private static readonly IReadOnlyDictionary<string, int> QingmingOffsetOverrides = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["2008"] = 1,
        ["2016"] = 1,
        ["2021"] = -1
    };

    public static IReadOnlyList<string> AllAliases => OrderedAliases;

    public static string AliasRegexPattern { get; } = string.Join("|", OrderedAliases.Select(Regex.Escape));

    public static bool TryGetCanonicalName(string holidayText, out string canonicalName)
    {
        if (AliasLookup.TryGetValue(holidayText.Trim(), out var definition))
        {
            canonicalName = definition.CanonicalName;
            return true;
        }

        canonicalName = string.Empty;
        return false;
    }

    public static IReadOnlyList<string> GetAliases(string canonicalName)
    {
        var definition = Definitions.FirstOrDefault(item => string.Equals(item.CanonicalName, canonicalName, StringComparison.OrdinalIgnoreCase));
        return definition?.Aliases ?? [];
    }

    public static IEnumerable<string> MatchCanonicalNames(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return [];
        }

        return Definitions
            .Where(definition => definition.Aliases.Any(alias => sourceName.Contains(alias, StringComparison.OrdinalIgnoreCase)))
            .Select(definition => definition.CanonicalName)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static DateTime? TryGetAnchorDate(string holidayNameOrAlias, int year)
    {
        if (!TryResolveDefinition(holidayNameOrAlias, out var definition))
        {
            return null;
        }

        return definition.AnchorDateFactory(year);
    }

    private static bool TryResolveDefinition(string holidayNameOrAlias, out HolidayDefinition definition)
    {
        if (AliasLookup.TryGetValue(holidayNameOrAlias.Trim(), out definition!))
        {
            return true;
        }

        definition = Definitions.FirstOrDefault(item => string.Equals(item.CanonicalName, holidayNameOrAlias.Trim(), StringComparison.OrdinalIgnoreCase))!;
        return definition is not null;
    }

    private static DateTime? TryCreateLunarDate(int year, int lunarMonth, int lunarDay)
    {
        if (year < LunarCalendar.MinSupportedDateTime.Year || year > LunarCalendar.MaxSupportedDateTime.Year)
        {
            return null;
        }

        try
        {
            var month = lunarMonth;
            var leapMonth = LunarCalendar.GetLeapMonth(year);
            if (leapMonth > 0 && month >= leapMonth)
            {
                month++;
            }

            return new DateTime(year, month, lunarDay, LunarCalendar);
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? TryCreateQingmingDate(int year)
    {
        if (year < 1901 || year > 2100)
        {
            return null;
        }

        var yearPart = year % 100;
        var day = (int)(yearPart * 0.2422 + 4.81) - yearPart / 4;

        if (QingmingOffsetOverrides.TryGetValue(year.ToString(CultureInfo.InvariantCulture), out var offset))
        {
            day += offset;
        }

        return new DateTime(year, 4, day);
    }
}
