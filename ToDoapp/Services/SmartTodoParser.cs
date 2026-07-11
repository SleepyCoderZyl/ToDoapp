using System;
using ToDoapp.Models;

namespace ToDoapp.Services;

/// <summary>
/// 智能待办解析器 - 从自然语言输入中提取标题、日期、时间和提醒偏移。
/// 实现按职责拆分为多个 partial 文件：
/// - SmartTodoParser.cs                       入口、public API 与主流程
/// - SmartTodoParser.DateExtraction.cs        日期提取（相对日期、节假日、星期、绝对日期等）
/// - SmartTodoParser.TimeExtraction.cs        时间提取（24小时制、中文时间、时段、相对时间）
/// - SmartTodoParser.ReminderOffsetExtraction.cs 提醒偏移提取（提前N分钟/小时/天）
/// - SmartTodoParser.TitleExtraction.cs       标题清理与格式化
/// - SmartTodoParser.ChineseNumerals.cs       中文数字解析
/// </summary>
public partial class SmartTodoParser
{
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
}
