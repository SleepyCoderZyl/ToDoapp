using System;
using System.Globalization;
using System.Text.RegularExpressions;
using ToDoapp.Models;

namespace ToDoapp.Services;

/// <summary>
/// SmartTodoParser 的提醒偏移提取逻辑。
/// </summary>
public partial class SmartTodoParser
{
    // 提前N分钟/半小时/一刻钟/N小时/N天，支持中文数字与末尾“提醒”
    private static readonly Regex ReminderOffsetRegex = new(
        @"提前\s*(?:(?<minutes>[\d〇零一二两三四五六七八九十]+)\s*分\s*钟?|(?<half>半小?时)|(?<quarter>一刻钟)|(?<hours>[\d〇零一二两三四五六七八九十]+)\s*个?\s*小?时|(?<days>[\d〇零一二两三四五六七八九十]+)\s*天)\s*(?:提醒)?",
        RegexOptions.Compiled);

    private static OffsetExtractionResult ExtractReminderOffset(string input, string matchedDateText, string matchedTimeText)
    {
        var match = ReminderOffsetRegex.Match(input);
        if (!match.Success)
        {
            return default;
        }

        if (!string.IsNullOrWhiteSpace(matchedDateText))
        {
            var alreadyStart = input.IndexOf(matchedDateText, StringComparison.OrdinalIgnoreCase);
            if (alreadyStart >= 0)
            {
                var alreadyEnd = alreadyStart + matchedDateText.Length;
                if (match.Index < alreadyEnd && match.Index + match.Length > alreadyStart)
                {
                    return default;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(matchedTimeText))
        {
            var alreadyStart = input.IndexOf(matchedTimeText, StringComparison.OrdinalIgnoreCase);
            if (alreadyStart >= 0)
            {
                var alreadyEnd = alreadyStart + matchedTimeText.Length;
                if (match.Index < alreadyEnd && match.Index + match.Length > alreadyStart)
                {
                    return default;
                }
            }
        }

        int minutes;
        string hint;
        if (TryParseMatchedInteger(match, "minutes", out var minuteValue))
        {
            minutes = minuteValue;
            hint = $"{minutes} 分钟";
        }
        else if (TryParseMatchedInteger(match, "hours", out var hourValue))
        {
            minutes = hourValue * 60;
            hint = $"{hourValue} 小时";
        }
        else if (TryParseMatchedInteger(match, "days", out var dayValue))
        {
            minutes = dayValue * 24 * 60;
            hint = $"{dayValue} 天";
        }
        else if (match.Groups["half"].Success)
        {
            minutes = 30;
            hint = "半小时";
        }
        else if (match.Groups["quarter"].Success)
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

    private static bool TryParseMatchedInteger(Match match, string groupName, out int value)
    {
        value = 0;
        var text = match.Groups[groupName].Value.Trim();
        return !string.IsNullOrEmpty(text) &&
            (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ||
             TryParseChineseNumber(text, out value));
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
}
