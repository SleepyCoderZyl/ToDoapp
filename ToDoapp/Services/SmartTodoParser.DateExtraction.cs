using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ToDoapp.Models;

namespace ToDoapp.Services;

/// <summary>
/// SmartTodoParser 的日期提取逻辑。
/// </summary>
public partial class SmartTodoParser
{
    private static readonly Regex HolidayExpressionRegex = new(
        $"({HolidayCatalog.AliasRegexPattern})\\s*(前|后)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GreatDayAfterTomorrowRegex = new(@"(大后天)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DayAfterTomorrowRegex = new(@"(后天|后日)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TomorrowRegex = new(@"(明天|明日)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TodayRegex = new(@"(今天|今日)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DaysLaterRegex = new(@"(?<!\d)(\d+)(?:天后|天以后)(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NextNextWeekendRegex = new(@"(下下周末)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NextWeekendRegex = new(@"(下周末)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ThisWeekendRegex = new(@"((?:本周末|这周末|周末))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NextNextWeekRegex = new(@"(?:下下(?:周|星期))(一|二|三|四|五|六|日|天)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NextWeekRegex = new(@"(?:下(?:周|星期))(一|二|三|四|五|六|日|天)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ThisWeekRegex = new(@"(?:本(?:周|星期))(一|二|三|四|五|六|日|天)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WeekDayRegex = new(@"(?:周|星期)(一|二|三|四|五|六|日|天)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NextYearMonthDayRegex = new(@"(明年)\s*(\d{1,2})月(\d{1,2})(?:日|号)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FullDateRegex = new(@"(?<!\d)(\d{4})[年/-](\d{1,2})[月/-](\d{1,2})(?:日|号)?(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NextMonthDayRegex = new(@"(下个月)\s*(\d{1,2})(?:日|号)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MonthDayTextRegex = new(@"(?<!\d)(\d{1,2})月(\d{1,2})(?:日|号)(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ShortDateRegex = new(@"(?<!\d)(\d{1,2})[/-](\d{1,2})(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DayOfMonthRegex = new(@"(?<![\d月])(\d{1,2})(?:日|号)(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EndOfMonthRegex = new(@"(月底|月末)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StartOfMonthRegex = new(@"(月初)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyList<Regex> CleanupDateRegexes =
    [
        GreatDayAfterTomorrowRegex,
        DayAfterTomorrowRegex,
        TomorrowRegex,
        TodayRegex,
        DaysLaterRegex,
        NextNextWeekendRegex,
        NextWeekendRegex,
        ThisWeekendRegex,
        NextNextWeekRegex,
        NextWeekRegex,
        ThisWeekRegex,
        WeekDayRegex,
        NextYearMonthDayRegex,
        FullDateRegex,
        NextMonthDayRegex,
        MonthDayTextRegex,
        ShortDateRegex,
        EndOfMonthRegex,
        StartOfMonthRegex,
        DayOfMonthRegex,
        HolidayExpressionRegex
    ];

    private static DateExtractionResult ExtractDate(string input, DateTime today, IHolidayDateResolver holidayDateResolver)
    {
        foreach (var extractor in GetDateExtractors(today, holidayDateResolver))
        {
            var result = extractor(input);
            if (result.DueDate.HasValue)
            {
                return result;
            }
        }

        return default;
    }

    private static IEnumerable<Func<string, DateExtractionResult>> GetDateExtractors(DateTime today, IHolidayDateResolver holidayDateResolver)
    {
        yield return input => TryExtractSimpleDate(input, GreatDayAfterTomorrowRegex, today.AddDays(3));
        yield return input => TryExtractSimpleDate(input, DayAfterTomorrowRegex, today.AddDays(2));
        yield return input => TryExtractSimpleDate(input, TomorrowRegex, today.AddDays(1));
        yield return input => TryExtractSimpleDate(input, TodayRegex, today);
        yield return input => TryExtractDaysLater(input, today);
        yield return input => TryExtractHolidayExpression(input, today, holidayDateResolver);

        yield return input => TryExtractWeekday(input, NextNextWeekendRegex, today, offsetWeeks: 2, weekend: true, useCalendarWeekOffset: true);
        yield return input => TryExtractWeekday(input, NextWeekendRegex, today, offsetWeeks: 1, weekend: true, useCalendarWeekOffset: true);
        yield return input => TryExtractWeekday(input, ThisWeekendRegex, today, weekend: true);
        yield return input => TryExtractWeekday(input, NextNextWeekRegex, today, offsetWeeks: 2, useCalendarWeekOffset: true);
        yield return input => TryExtractWeekday(input, NextWeekRegex, today, offsetWeeks: 1, useCalendarWeekOffset: true);
        yield return input => TryExtractWeekday(input, ThisWeekRegex, today, allowToday: true);
        yield return input => TryExtractWeekday(input, WeekDayRegex, today, allowToday: true);

        yield return input => TryExtractNextYearMonthDay(input);
        yield return input => TryExtractFullDate(input);
        yield return input => TryExtractNextMonthDay(input, today);
        yield return input => TryExtractMonthDay(input, MonthDayTextRegex, today, nextMonthWhenPast: false);
        yield return input => TryExtractMonthDay(input, ShortDateRegex, today, nextMonthWhenPast: false);
        yield return input => TryExtractMonthBoundary(input, EndOfMonthRegex, today, endOfMonth: true);
        yield return input => TryExtractMonthBoundary(input, StartOfMonthRegex, today, endOfMonth: false);
        yield return input => TryExtractDayOfMonth(input, today);
    }

    private static DateExtractionResult TryExtractSimpleDate(string input, Regex regex, DateTime date)
    {
        var match = regex.Match(input);
        return match.Success
            ? new DateExtractionResult(date, match.Value, "相对日期")
            : default;
    }

    private static DateExtractionResult TryExtractDaysLater(string input, DateTime today)
    {
        var match = DaysLaterRegex.Match(input);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var days))
        {
            return default;
        }

        return new DateExtractionResult(today.AddDays(days), match.Value, "相对日期");
    }

    private static DateExtractionResult TryExtractHolidayExpression(string input, DateTime today, IHolidayDateResolver holidayDateResolver)
    {
        var match = HolidayExpressionRegex.Match(input);
        if (!match.Success)
        {
            return default;
        }

        var relation = match.Groups[2].Value switch
        {
            "前" => HolidayDateRelation.BeforeHoliday,
            "后" => HolidayDateRelation.AfterHoliday,
            _ => HolidayDateRelation.OnHolidayStart
        };

        var resolution = holidayDateResolver.ResolveHolidayDate(match.Groups[1].Value, relation, today);
        return resolution is not null
            ? new DateExtractionResult(
                resolution.DueDate,
                match.Value,
                resolution.UsesOfficialSchedule
                    ? $"节假日：{resolution.CanonicalName}（放假安排）"
                    : $"节假日：{resolution.CanonicalName}（锚点回退）")
            : default;
    }

    private static DateExtractionResult TryExtractWeekday(
        string input,
        Regex regex,
        DateTime today,
        int offsetWeeks = 0,
        bool allowToday = false,
        bool weekend = false,
        bool useCalendarWeekOffset = false)
    {
        var match = regex.Match(input);
        if (!match.Success)
        {
            return default;
        }

        var dueDate = weekend
            ? GetWeekendDate(today, offsetWeeks, useCalendarWeekOffset)
            : GetWeekdayDate(today, GetDayOfWeek(match.Groups[1].Value), offsetWeeks, allowToday, useCalendarWeekOffset);

        return new DateExtractionResult(dueDate, match.Value, weekend ? "周末规则" : "星期规则");
    }

    private static DateExtractionResult TryExtractNextYearMonthDay(string input)
    {
        var match = NextYearMonthDayRegex.Match(input);
        if (!match.Success)
        {
            return default;
        }

        return TryCreateResult(match, (DateTime.Today.Year + 1).ToString(), match.Groups[2].Value, match.Groups[3].Value, "绝对日期");
    }

    private static DateExtractionResult TryExtractFullDate(string input)
    {
        var match = FullDateRegex.Match(input);
        if (!match.Success)
        {
            return default;
        }

        return TryCreateResult(match, match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, "绝对日期");
    }

    private static DateExtractionResult TryExtractNextMonthDay(string input, DateTime today)
    {
        var match = NextMonthDayRegex.Match(input);
        if (!match.Success)
        {
            return default;
        }

        var firstDayOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
        return TryCreateResult(match, firstDayOfNextMonth.Year, firstDayOfNextMonth.Month, match.Groups[2].Value, "绝对日期");
    }

    private static DateExtractionResult TryExtractMonthDay(string input, Regex regex, DateTime today, bool nextMonthWhenPast)
    {
        var match = regex.Match(input);
        if (!match.Success)
        {
            return default;
        }

        if (!int.TryParse(match.Groups[1].Value, out var month) ||
            !int.TryParse(match.Groups[2].Value, out var day))
        {
            return default;
        }

        var dueDate = TryCreateFutureMonthDay(today, month, day, nextMonthWhenPast ? 1 : 1);
        return dueDate.HasValue
            ? new DateExtractionResult(dueDate.Value, match.Value, "绝对日期")
            : default;
    }

    private static DateExtractionResult TryExtractMonthBoundary(string input, Regex regex, DateTime today, bool endOfMonth)
    {
        var match = regex.Match(input);
        if (!match.Success)
        {
            return default;
        }

        var dueDate = endOfMonth
            ? new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month))
            : new DateTime(today.Year, today.Month, 1);

        if (dueDate < today)
        {
            var nextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            dueDate = endOfMonth
                ? new DateTime(nextMonth.Year, nextMonth.Month, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month))
                : nextMonth;
        }

        return new DateExtractionResult(dueDate, match.Value, endOfMonth ? "月末规则" : "月初规则");
    }

    private static DateExtractionResult TryExtractDayOfMonth(string input, DateTime today)
    {
        var match = DayOfMonthRegex.Match(input);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var day))
        {
            return default;
        }

        var dueDate = TryCreateFutureDayOfMonth(today, day);
        return dueDate.HasValue
            ? new DateExtractionResult(dueDate.Value, match.Value, "绝对日期")
            : default;
    }

    private static DateExtractionResult TryCreateResult(Match match, string yearText, string monthText, string dayText, string sourceHint)
    {
        if (!int.TryParse(yearText, out var year) ||
            !int.TryParse(monthText, out var month) ||
            !int.TryParse(dayText, out var day))
        {
            return default;
        }

        return TryCreateResult(match, year, month, day, sourceHint);
    }

    private static DateExtractionResult TryCreateResult(Match match, int year, int month, string dayText, string sourceHint)
    {
        return int.TryParse(dayText, out var day)
            ? TryCreateResult(match, year, month, day, sourceHint)
            : default;
    }

    private static DateExtractionResult TryCreateResult(Match match, int year, int month, int day, string sourceHint)
    {
        try
        {
            return new DateExtractionResult(new DateTime(year, month, day), match.Value, sourceHint);
        }
        catch
        {
            return default;
        }
    }

    private static DateTime GetWeekdayDate(DateTime today, DayOfWeek targetDay, int offsetWeeks, bool allowToday, bool useCalendarWeekOffset)
    {
        if (useCalendarWeekOffset)
        {
            var currentWeekStart = today.AddDays(-GetMondayBasedDayIndex(today.DayOfWeek));
            return currentWeekStart.AddDays(offsetWeeks * 7 + GetMondayBasedDayIndex(targetDay));
        }

        var daysUntil = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;
        if (daysUntil == 0 && !allowToday)
        {
            daysUntil = 7;
        }

        return today.AddDays(daysUntil + offsetWeeks * 7);
    }

    private static DateTime GetWeekendDate(DateTime today, int offsetWeeks, bool useCalendarWeekOffset)
    {
        var saturday = GetWeekdayDate(today, DayOfWeek.Saturday, offsetWeeks, allowToday: true, useCalendarWeekOffset);
        return saturday;
    }

    private static DateTime? TryCreateFutureMonthDay(DateTime today, int month, int day, int yearSearchWindow)
    {
        for (var offset = 0; offset <= yearSearchWindow; offset++)
        {
            try
            {
                var candidate = new DateTime(today.Year + offset, month, day);
                if (candidate >= today)
                {
                    return candidate;
                }
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static DateTime? TryCreateFutureDayOfMonth(DateTime today, int day)
    {
        var cursor = new DateTime(today.Year, today.Month, 1);

        for (var monthOffset = 0; monthOffset < 24; monthOffset++)
        {
            var candidateMonth = cursor.AddMonths(monthOffset);
            if (day > DateTime.DaysInMonth(candidateMonth.Year, candidateMonth.Month))
            {
                continue;
            }

            var candidate = new DateTime(candidateMonth.Year, candidateMonth.Month, day);
            if (candidate >= today)
            {
                return candidate;
            }
        }

        return null;
    }

    private static DayOfWeek GetDayOfWeek(string dayName)
    {
        return dayName switch
        {
            "一" => DayOfWeek.Monday,
            "二" => DayOfWeek.Tuesday,
            "三" => DayOfWeek.Wednesday,
            "四" => DayOfWeek.Thursday,
            "五" => DayOfWeek.Friday,
            "六" => DayOfWeek.Saturday,
            "日" or "天" => DayOfWeek.Sunday,
            _ => DayOfWeek.Monday
        };
    }

    private static int GetMondayBasedDayIndex(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            _ => 6
        };
    }
}
