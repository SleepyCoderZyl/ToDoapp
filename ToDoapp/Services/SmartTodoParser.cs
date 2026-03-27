using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ToDoapp.Services;

public class SmartTodoParser
{
    public class ParsedTodoResult
    {
        public string Title { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }

    private static readonly Regex DayAfterTomorrowRegex = new Regex(@"(后天|后日)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TomorrowRegex = new Regex(@"(明天|明日)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TodayRegex = new Regex(@"(今天|今日)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DaysLaterRegex = new Regex(@"(\d+)(?:天后|天以后)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NextWeekRegex = new Regex(@"(?:下(?:周|星期)(一|二|三|四|五|六|日|天))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ThisWeekRegex = new Regex(@"(本周)(一|二|三|四|五|六|日|天)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WeekDayRegex = new Regex(@"(?:周|星期)(一|二|三|四|五|六|日|天)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FullDateRegex = new Regex(@"(\d{4})[/-](\d{1,2})[/-](\d{1,2})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ShortDateRegex = new Regex(@"(\d{1,2})[/-](\d{1,2})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MonthDayRegex = new Regex(@"(?:(?:本)?月)?(\d{1,2})(?:日|号)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DayRegex = new Regex(@"(\d{1,2})(?:日|号)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LeadingPunctuationRegex = new Regex(@"^\s*[，,。.、；;：:！!?？\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TrailingPunctuationRegex = new Regex(@"[，,。.、；;：:！!?？\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

    public static ParsedTodoResult Parse(string input)
    {
        var result = new ParsedTodoResult();
        var cleanedInput = input.Trim();

        result.DueDate = ExtractDate(cleanedInput);
        result.Title = ExtractTitle(cleanedInput, result.DueDate);

        return result;
    }

    private static DateTime? ExtractDate(string input)
    {
        var today = DateTime.Today;

        var match = TodayRegex.Match(input);
        if (match.Success)
        {
            return today;
        }

        match = TomorrowRegex.Match(input);
        if (match.Success)
        {
            return today.AddDays(1);
        }

        match = DayAfterTomorrowRegex.Match(input);
        if (match.Success)
        {
            return today.AddDays(2);
        }

        match = DaysLaterRegex.Match(input);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int days))
        {
            return today.AddDays(days);
        }

        match = NextWeekRegex.Match(input);
        if (match.Success)
        {
            var dayName = match.Groups[1].Value;
            var targetDayOfWeek = GetDayOfWeek(dayName);
            var daysUntil = ((int)targetDayOfWeek - (int)today.DayOfWeek + 7) % 7;
            if (daysUntil == 0) daysUntil = 7;
            return today.AddDays(daysUntil);
        }

        match = ThisWeekRegex.Match(input);
        if (match.Success)
        {
            var dayName = match.Groups[1].Value;
            if (string.IsNullOrEmpty(dayName)) return today;
            var targetDayOfWeek = GetDayOfWeek(dayName);
            var daysUntil = ((int)targetDayOfWeek - (int)today.DayOfWeek + 7) % 7;
            return today.AddDays(daysUntil);
        }

        match = WeekDayRegex.Match(input);
        if (match.Success)
        {
            var dayName = match.Groups[1].Value;
            var targetDayOfWeek = GetDayOfWeek(dayName);
            var daysUntil = ((int)targetDayOfWeek - (int)today.DayOfWeek + 7) % 7;
            if (daysUntil == 0) return today;
            return today.AddDays(daysUntil);
        }

        match = FullDateRegex.Match(input);
        if (match.Success && 
            int.TryParse(match.Groups[1].Value, out int year) &&
            int.TryParse(match.Groups[2].Value, out int month) &&
            int.TryParse(match.Groups[3].Value, out int day))
        {
            try { return new DateTime(year, month, day); }
            catch { }
        }

        match = ShortDateRegex.Match(input);
        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out int month2) &&
            int.TryParse(match.Groups[2].Value, out int day2))
        {
            try
            {
                var shortDate = new DateTime(today.Year, month2, day2);
                if (shortDate < today) shortDate = shortDate.AddYears(1);
                return shortDate;
            }
            catch { }
        }

        match = MonthDayRegex.Match(input);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int day3))
        {
            try
            {
                var monthDate = new DateTime(today.Year, today.Month, day3);
                if (monthDate < today) monthDate = monthDate.AddMonths(1);
                return monthDate;
            }
            catch { }
        }

        match = DayRegex.Match(input);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int day4))
        {
            try
            {
                var dayDate = new DateTime(today.Year, today.Month, day4);
                if (dayDate < today) dayDate = dayDate.AddMonths(1);
                return dayDate;
            }
            catch { }
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

    private static string ExtractTitle(string input, DateTime? dueDate)
    {
        var title = input;
        
        title = TodayRegex.Replace(title, "");
        title = TomorrowRegex.Replace(title, "");
        title = DayAfterTomorrowRegex.Replace(title, "");
        title = DaysLaterRegex.Replace(title, "");
        title = NextWeekRegex.Replace(title, "");
        title = ThisWeekRegex.Replace(title, "");
        title = WeekDayRegex.Replace(title, "");
        title = FullDateRegex.Replace(title, "");
        title = ShortDateRegex.Replace(title, "");
        title = MonthDayRegex.Replace(title, "");
        title = DayRegex.Replace(title, "");
        title = LeadingPunctuationRegex.Replace(title, "");
        title = TrailingPunctuationRegex.Replace(title, "");

        title = WhitespaceRegex.Replace(title, " ").Trim();
        title = title.Trim('，', ',', '。', '.', '、', '；', ';', '：', ':', '！', '!', '?', '？');

        return string.IsNullOrWhiteSpace(title) ? input : title;
    }
}
