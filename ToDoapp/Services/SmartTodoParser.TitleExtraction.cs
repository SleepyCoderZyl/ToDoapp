using System;
using System.Text.RegularExpressions;

namespace ToDoapp.Services;

/// <summary>
/// SmartTodoParser 的标题清理与格式化逻辑。
/// </summary>
public partial class SmartTodoParser
{
    private static readonly Regex LeadingPunctuationRegex = new(@"^\s*[，,。.、；;：:！!?？\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TrailingPunctuationRegex = new(@"[，,。.、；;：:！!?？\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex InnerPunctuationRegex = new(@"^[，,。.、；;：:\-—\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex LeadingWeakPhraseRegex = new(
        @"^(?:(?:提醒我|记得|帮我|帮忙|我要|我得|需要|安排|处理|完成|做|去)(?:一下|一件|一趟|一份|一版)?)+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
