using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using ToDoapp.Models;

namespace ToDoapp.Services;

public class SmartTodoParser
{
    private static readonly Regex HolidayExpressionRegex = new(
        $@"({HolidayCatalog.AliasRegexPattern})\s*(前|后)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public class ParsedTodoResult
    {
        public string Title { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public TimeOnly? DueTime { get; set; }
        public int? ReminderOffsetMinutes { get; set; }
        public string? DateSourceHint { get; set; }
        public string? TimeSourceHint { get; set; }
        public string? OffsetSourceHint { get; set; }
    }

    private readonly record struct DateExtractionResult(DateTime? DueDate, string MatchedText, string? SourceHint);
    private readonly record struct TimeExtractionResult(TimeOnly? DueTime, string MatchedText, string? SourceHint, int? RelativeOffsetMinutes = null);
    private readonly record struct OffsetExtractionResult(int? OffsetMinutes, string MatchedText, string? SourceHint);

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

    private static readonly Regex TimeQualifierRegex = new(@"(今晚|今早|今晨|明早|明晨|下午|上午|晚上|早上|凌晨|中午|傍晚)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 24h: HH:mm 或 HH:mm:ss，冒号支持半角/全角
    private static readonly Regex Time24hRegex = new(@"(?<!\d)(\d{1,2})[:：](\d{1,2})(?:[:：](\d{1,2}))?(?!\d)", RegexOptions.Compiled);
    // H点 / H点半 / H点M分（阿拉伯数字）
    private static readonly Regex TimeChineseRegex = new(@"(?<!\d)(\d{1,2})点(?:(半|(\d{1,2})分))?(?!\d)", RegexOptions.Compiled);
    // 上午/下午/晚上/早上/今早/明早/中午/傍晚/凌晨 H点（阿拉伯数字）
    private static readonly Regex TimePeriodRegex = new(@"(上午|下午|晚上|早上|今早|明早|中午|傍晚|凌晨|今晚)\s*(\d{1,2})点(?:(半|(\d{1,2})分))?", RegexOptions.Compiled);

    // H点 / H点半 / H点M分（汉字数字，如四点半、两点十五分）
    private static readonly Regex ChineseNumeralTimeRegex = new(
        @"(?<![〇零一二两三四五六七八九十百千万\d])([〇零一二两三四五六七八九十百千万]+)点(?:(半)|([〇零一二两三四五六七八九十百千万]+)分)?(?![〇零一二两三四五六七八九十百千万\d])",
        RegexOptions.Compiled);
    // 上午/下午... H点（汉字数字，如下午四点半）
    private static readonly Regex ChineseNumeralTimePeriodRegex = new(
        @"(上午|下午|晚上|早上|今早|明早|中午|傍晚|凌晨|今晚)\s*([〇零一二两三四五六七八九十百千万]+)点(?:(半)|([〇零一二两三四五六七八九十百千万]+)分)?",
        RegexOptions.Compiled);

    // 相对当前时间：半小时后、十分钟后、一小时后、两天后
    private static readonly Regex RelativeTimeRegex = new(
        @"(?<!\d)(半小?时|([\d〇零一二两三四五六七八九十百千万]+)\s*分\s*钟?|([\d〇零一二两三四五六七八九十百千万]+)\s*小?时|([\d〇零一二两三四五六七八九十百千万]+)\s*天)(?:后|以后)(?!\d)",
        RegexOptions.Compiled);

    // 提前N分钟/半小时/一刻钟/N小时/N天
    private static readonly Regex ReminderOffsetRegex = new(
        @"提前\s*(?:(\d+)\s*分钟|半小时|一刻钟|(\d+)\s*小时|(\d+)\s*天)",
        RegexOptions.Compiled);

    private static readonly Regex LeadingPunctuationRegex = new(@"^\s*[，,。.、；;：:！!?？\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TrailingPunctuationRegex = new(@"[，,。.、；;：:！!?？\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex InnerPunctuationRegex = new(@"^[，,。.、；;：:\-—\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex LeadingWeakPhraseRegex = new(
        @"^(?:(?:提醒我|记得|帮我|帮忙|我要|我得|需要|安排|处理|完成|做|去)(?:一下|一件|一趟|一份|一版)?)+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

    public static ParsedTodoResult Parse(string input)
    {
        return Parse(input, DateTime.Now, HolidayCalendarService.Instance);
    }

    public static ParsedTodoResult Parse(string input, DateTime referenceDate, IHolidayDateResolver holidayDateResolver)
    {
        var cleanedInput = input.Trim();
        var dateResult = ExtractDate(cleanedInput, referenceDate.Date, holidayDateResolver);
        var timeResult = ExtractTime(cleanedInput, referenceDate);
        var offsetResult = ExtractReminderOffset(cleanedInput, dateResult.MatchedText, timeResult.MatchedText);

        var dueDate = dateResult.DueDate;
        var dueTime = timeResult.DueTime;
        var dateSourceHint = dateResult.SourceHint;

        // 相对当前时间（如半小时后）需要同时确定日期和时间
        if (timeResult.RelativeOffsetMinutes.HasValue)
        {
            var targetDateTime = referenceDate.AddMinutes(timeResult.RelativeOffsetMinutes.Value);
            dueDate = targetDateTime.Date;
            dueTime = TimeOnly.FromDateTime(targetDateTime);
            dateSourceHint ??= "相对当前";
        }

        return new ParsedTodoResult
        {
            DueDate = dueDate,
            DueTime = dueTime,
            ReminderOffsetMinutes = offsetResult.OffsetMinutes,
            Title = ExtractTitle(cleanedInput, dateResult.MatchedText, timeResult.MatchedText, offsetResult.MatchedText),
            DateSourceHint = dateSourceHint,
            TimeSourceHint = timeResult.SourceHint,
            OffsetSourceHint = offsetResult.SourceHint
        };
    }

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

    private static TimeExtractionResult ExtractTime(string input, DateTime referenceDate)
    {
        var candidates = new List<TimeExtractionResult>();

        var periodMatch = TimePeriodRegex.Match(input);
        if (periodMatch.Success)
        {
            var period = ResolvePeriod(periodMatch.Groups[1].Value);
            var hour = int.Parse(periodMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            var minute = ResolveMinutes(periodMatch.Groups[3].Value, periodMatch.Groups[4].Value);
            if (TryBuildTime(hour, minute, period, out var time))
            {
                candidates.Add(new TimeExtractionResult(time, periodMatch.Value, "时段"));
            }
        }

        if (candidates.Count == 0)
        {
            var chinesePeriodMatch = ChineseNumeralTimePeriodRegex.Match(input);
            if (chinesePeriodMatch.Success)
            {
                var period = ResolvePeriod(chinesePeriodMatch.Groups[1].Value);
                if (TryParseHour(chinesePeriodMatch.Groups[2].Value, out var hour))
                {
                    var minute = ResolveMinutes(chinesePeriodMatch.Groups[3].Value, string.Empty, chinesePeriodMatch.Groups[4].Value);
                    if (TryBuildTime(hour, minute, period, out var time))
                    {
                        candidates.Add(new TimeExtractionResult(time, chinesePeriodMatch.Value, "时段"));
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            var chineseMatch = TimeChineseRegex.Match(input);
            if (chineseMatch.Success)
            {
                var hour = int.Parse(chineseMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                var minute = ResolveMinutes(chineseMatch.Groups[2].Value, chineseMatch.Groups[3].Value);
                if (TryBuildTime(hour, minute, DayPeriod.None, out var time))
                {
                    candidates.Add(new TimeExtractionResult(time, chineseMatch.Value, "中文时间"));
                }
            }
        }

        if (candidates.Count == 0)
        {
            var chineseNumeralMatch = ChineseNumeralTimeRegex.Match(input);
            if (chineseNumeralMatch.Success)
            {
                if (TryParseHour(chineseNumeralMatch.Groups[1].Value, out var hour))
                {
                    var minute = ResolveMinutes(chineseNumeralMatch.Groups[2].Value, string.Empty, chineseNumeralMatch.Groups[3].Value);
                    if (TryBuildTime(hour, minute, DayPeriod.None, out var time))
                    {
                        candidates.Add(new TimeExtractionResult(time, chineseNumeralMatch.Value, "中文时间"));
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            var match24 = Time24hRegex.Match(input);
            if (match24.Success)
            {
                var hour = int.Parse(match24.Groups[1].Value, CultureInfo.InvariantCulture);
                var minute = int.Parse(match24.Groups[2].Value, CultureInfo.InvariantCulture);
                if (TryBuildTime(hour, minute, DayPeriod.None, out var time))
                {
                    candidates.Add(new TimeExtractionResult(time, match24.Value, "24小时"));
                }
            }
        }

        if (candidates.Count == 0)
        {
            var relativeMatch = RelativeTimeRegex.Match(input);
            if (relativeMatch.Success)
            {
                var offsetMinutes = ResolveRelativeOffsetMinutes(relativeMatch);
                if (offsetMinutes.HasValue)
                {
                    var targetDateTime = referenceDate.AddMinutes(offsetMinutes.Value);
                    var time = TimeOnly.FromDateTime(targetDateTime);
                    candidates.Add(new TimeExtractionResult(time, relativeMatch.Value, "相对当前", offsetMinutes.Value));
                }
            }
        }

        return candidates.Count > 0 ? candidates[0] : default;
    }

    private static OffsetExtractionResult ExtractReminderOffset(string input, params string[] alreadyMatchedTexts)
    {
        var match = ReminderOffsetRegex.Match(input);
        if (!match.Success)
        {
            return default;
        }

        if (alreadyMatchedTexts.Length > 0)
        {
            foreach (var already in alreadyMatchedTexts)
            {
                if (string.IsNullOrWhiteSpace(already))
                {
                    continue;
                }

                var alreadyStart = input.IndexOf(already, StringComparison.OrdinalIgnoreCase);
                if (alreadyStart < 0)
                {
                    continue;
                }

                var alreadyEnd = alreadyStart + already.Length;
                if (match.Index < alreadyEnd && match.Index + match.Length > alreadyStart)
                {
                    return default;
                }
            }
        }

        int minutes;
        string hint;
        if (match.Groups[1].Success)
        {
            minutes = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            hint = $"{minutes} 分钟";
        }
        else if (match.Groups[2].Success)
        {
            minutes = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) * 60;
            hint = $"{match.Groups[2].Value} 小时";
        }
        else if (match.Groups[3].Success)
        {
            minutes = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) * 24 * 60;
            hint = $"{match.Groups[3].Value} 天";
        }
        else if (match.Value.Contains("半小时"))
        {
            minutes = 30;
            hint = "半小时";
        }
        else if (match.Value.Contains("一刻钟"))
        {
            minutes = 15;
            hint = "一刻钟";
        }
        else
        {
            return default;
        }

        if (minutes > AppConstants.MaxReminderOffsetMinutes)
        {
            return default;
        }

        return new OffsetExtractionResult(minutes, match.Value, hint);
    }

    private static int ResolveMinutes(string halfText, string digitText, string chineseMinuteText = "")
    {
        if (!string.IsNullOrEmpty(digitText) && int.TryParse(digitText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var digit))
        {
            return digit;
        }

        if (!string.IsNullOrEmpty(chineseMinuteText) && TryParseChineseNumber(chineseMinuteText, out var chineseMinute))
        {
            return chineseMinute;
        }

        return halfText == "半" ? 30 : 0;
    }

    private static bool TryParseHour(string text, out int hour)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out hour))
        {
            return true;
        }

        return TryParseChineseNumber(text, out hour);
    }

    private static bool TryParseChineseNumber(string text, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var digits = new Dictionary<char, int>
        {
            ['〇'] = 0,
            ['零'] = 0,
            ['一'] = 1,
            ['二'] = 2,
            ['两'] = 2,
            ['三'] = 3,
            ['四'] = 4,
            ['五'] = 5,
            ['六'] = 6,
            ['七'] = 7,
            ['八'] = 8,
            ['九'] = 9
        };

        if (text.Length == 1 && digits.TryGetValue(text[0], out value))
        {
            return true;
        }

        int result = 0;
        int current = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '十')
            {
                if (current == 0)
                {
                    result += 10;
                }
                else
                {
                    result += current * 10;
                    current = 0;
                }
            }
            else if (digits.TryGetValue(c, out var d))
            {
                current = d;
                if (i + 1 >= text.Length || text[i + 1] != '十')
                {
                    result += current;
                    current = 0;
                }
            }
            else
            {
                return false;
            }
        }

        value = result;
        return value is >= 0 and <= 99;
    }

    private static int? ResolveRelativeOffsetMinutes(Match match)
    {
        if (match.Value.Contains("半小时"))
        {
            return 30;
        }

        if (match.Groups[2].Success)
        {
            var minuteText = match.Groups[2].Value.Trim();
            if (int.TryParse(minuteText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minute) || TryParseChineseNumber(minuteText, out minute))
            {
                return minute;
            }
        }

        if (match.Groups[3].Success)
        {
            var hourText = match.Groups[3].Value.Trim();
            if (int.TryParse(hourText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hour) || TryParseChineseNumber(hourText, out hour))
            {
                return hour * 60;
            }
        }

        if (match.Groups[4].Success)
        {
            var dayText = match.Groups[4].Value.Trim();
            if (int.TryParse(dayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day) || TryParseChineseNumber(dayText, out day))
            {
                return day * 24 * 60;
            }
        }

        return null;
    }

    private enum DayPeriod
    {
        None,
        Morning,
        Noon,
        Afternoon,
        Evening,
        Night
    }

    private static DayPeriod ResolvePeriod(string text)
    {
        return text switch
        {
            "上午" or "早上" or "今早" or "明早" or "凌晨" or "今晨" or "明晨" => DayPeriod.Morning,
            "中午" => DayPeriod.Noon,
            "下午" => DayPeriod.Afternoon,
            "傍晚" => DayPeriod.Evening,
            "晚上" or "今晚" => DayPeriod.Night,
            _ => DayPeriod.None
        };
    }

    private static bool TryBuildTime(int hour, int minute, DayPeriod period, out TimeOnly time)
    {
        time = default;
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
        {
            return false;
        }

        var adjustedHour = hour;
        switch (period)
        {
            case DayPeriod.Morning:
            case DayPeriod.Noon:
            case DayPeriod.Afternoon:
            case DayPeriod.Evening:
            case DayPeriod.Night:
                if (period == DayPeriod.Afternoon || period == DayPeriod.Evening || period == DayPeriod.Night)
                {
                    if (hour < 12)
                    {
                        adjustedHour = hour + 12;
                    }
                    else if (hour == 12 && period == DayPeriod.Night)
                    {
                        // 晚上12点 -> 00点
                        adjustedHour = 0;
                    }
                }
                else if (period == DayPeriod.Morning)
                {
                    if (hour == 12)
                    {
                        // 上午12点 -> 00点（极少出现）
                        adjustedHour = 0;
                    }
                }
                break;
        }

        if (adjustedHour > 23)
        {
            return false;
        }

        time = new TimeOnly(adjustedHour, minute);
        return true;
    }

    private static string ExtractTitle(string input, string matchedDateText, string matchedTimeText, string matchedOffsetText)
    {
        var conservativeTitle = CleanupTitle(input, matchedDateText, matchedTimeText, matchedOffsetText, removeWeakPhrases: false);
        var refinedTitle = CleanupTitle(input, matchedDateText, matchedTimeText, matchedOffsetText, removeWeakPhrases: true);

        return IsValidCleanTitle(refinedTitle)
            ? refinedTitle
            : conservativeTitle;
    }

    private static string CleanupTitle(string input, string matchedDateText, string matchedTimeText, string matchedOffsetText, bool removeWeakPhrases)
    {
        var title = input;

        if (!string.IsNullOrWhiteSpace(matchedDateText))
        {
            title = title.Replace(matchedDateText, " ", StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(matchedTimeText))
        {
            title = title.Replace(matchedTimeText, " ", StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(matchedOffsetText))
        {
            title = title.Replace(matchedOffsetText, " ", StringComparison.OrdinalIgnoreCase);
        }

        foreach (var regex in CleanupDateRegexes)
        {
            title = regex.Replace(title, " ");
        }

        title = TimeQualifierRegex.Replace(title, " ");
        title = ReminderOffsetRegex.Replace(title, " ");
        title = RelativeTimeRegex.Replace(title, " ");
        title = TimePeriodRegex.Replace(title, " ");
        title = ChineseNumeralTimePeriodRegex.Replace(title, " ");
        title = TimeChineseRegex.Replace(title, " ");
        title = ChineseNumeralTimeRegex.Replace(title, " ");
        title = Time24hRegex.Replace(title, " ");
        title = NormalizeSeparators(title);

        if (removeWeakPhrases)
        {
            var previous = title;
            do
            {
                previous = title;
                title = LeadingWeakPhraseRegex.Replace(title, string.Empty);
                title = NormalizeSeparators(title);
            }
            while (!string.Equals(previous, title, StringComparison.Ordinal));
        }

        title = NormalizeSeparators(title);
        return string.IsNullOrWhiteSpace(title) ? input.Trim() : title;
    }

    private static string NormalizeSeparators(string value)
    {
        var normalized = WhitespaceRegex.Replace(value, " ").Trim();
        normalized = LeadingPunctuationRegex.Replace(normalized, string.Empty);
        normalized = TrailingPunctuationRegex.Replace(normalized, string.Empty);

        while (!string.IsNullOrEmpty(normalized))
        {
            var updated = InnerPunctuationRegex.Replace(normalized, string.Empty);
            if (updated == normalized)
            {
                break;
            }

            normalized = updated;
        }

        return normalized.Trim('，', ',', '。', '.', '、', '；', ';', '：', ':', '！', '!', '?', '？', '-', '—');
    }

    private static bool IsValidCleanTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var compact = WhitespaceRegex.Replace(title, string.Empty);
        return compact.Length >= 2;
    }
}
