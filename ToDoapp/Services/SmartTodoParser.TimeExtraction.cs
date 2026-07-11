using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ToDoapp.Services;

/// <summary>
/// SmartTodoParser 的时间提取逻辑。
/// </summary>
public partial class SmartTodoParser
{
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

    private static TimeExtractionResult ExtractTime(string input, DateTime referenceDate)
    {
        var periodMatch = TimePeriodRegex.Match(input);
        if (periodMatch.Success)
        {
            var period = ResolvePeriod(periodMatch.Groups[1].Value);
            var hour = int.Parse(periodMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            var minute = ResolveMinutes(periodMatch.Groups[3].Value, periodMatch.Groups[4].Value);
            if (TryBuildTime(hour, minute, period, out var time))
            {
                return new TimeExtractionResult(time, periodMatch.Value, "时段");
            }
        }

        var chinesePeriodMatch = ChineseNumeralTimePeriodRegex.Match(input);
        if (chinesePeriodMatch.Success)
        {
            var period = ResolvePeriod(chinesePeriodMatch.Groups[1].Value);
            if (TryParseHour(chinesePeriodMatch.Groups[2].Value, out var hour))
            {
                var minute = ResolveMinutes(chinesePeriodMatch.Groups[3].Value, string.Empty, chinesePeriodMatch.Groups[4].Value);
                if (TryBuildTime(hour, minute, period, out var time))
                {
                    return new TimeExtractionResult(time, chinesePeriodMatch.Value, "时段");
                }
            }
        }

        var chineseMatch = TimeChineseRegex.Match(input);
        if (chineseMatch.Success)
        {
            var hour = int.Parse(chineseMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var minute = ResolveMinutes(chineseMatch.Groups[2].Value, chineseMatch.Groups[3].Value);
            if (TryBuildTime(hour, minute, DayPeriod.None, out var time))
            {
                return new TimeExtractionResult(time, chineseMatch.Value, "中文时间");
            }
        }

        var chineseNumeralMatch = ChineseNumeralTimeRegex.Match(input);
        if (chineseNumeralMatch.Success)
        {
            if (TryParseHour(chineseNumeralMatch.Groups[1].Value, out var hour))
            {
                var minute = ResolveMinutes(chineseNumeralMatch.Groups[2].Value, string.Empty, chineseNumeralMatch.Groups[3].Value);
                if (TryBuildTime(hour, minute, DayPeriod.None, out var time))
                {
                    return new TimeExtractionResult(time, chineseNumeralMatch.Value, "中文时间");
                }
            }
        }

        var match24 = Time24hRegex.Match(input);
        if (match24.Success)
        {
            var hour = int.Parse(match24.Groups[1].Value, CultureInfo.InvariantCulture);
            var minute = int.Parse(match24.Groups[2].Value, CultureInfo.InvariantCulture);
            if (TryBuildTime(hour, minute, DayPeriod.None, out var time))
            {
                return new TimeExtractionResult(time, match24.Value, "24小时");
            }
        }

        var relativeMatch = RelativeTimeRegex.Match(input);
        if (relativeMatch.Success)
        {
            var offsetMinutes = ResolveRelativeOffsetMinutes(relativeMatch);
            if (offsetMinutes.HasValue)
            {
                var targetDateTime = referenceDate.AddMinutes(offsetMinutes.Value);
                var time = TimeOnly.FromDateTime(targetDateTime);
                return new TimeExtractionResult(time, relativeMatch.Value, "相对当前", offsetMinutes.Value);
            }
        }

        return default;
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
}
