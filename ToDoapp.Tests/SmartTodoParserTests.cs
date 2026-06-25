using ToDoapp.Services;
using Xunit;

namespace ToDoapp.Tests;

public class SmartTodoParserTests
{
    [Fact]
    public void Parse_KeepsExistingRelativeDateRules()
    {
        var referenceDate = new DateTime(2026, 4, 1);
        var resolver = new StubHolidayDateResolver();

        Assert.Equal(new DateTime(2026, 4, 2), SmartTodoParser.Parse("明天交材料", referenceDate, resolver).DueDate);
        Assert.Equal(new DateTime(2026, 4, 6), SmartTodoParser.Parse("下周一开会", referenceDate, resolver).DueDate);
        Assert.Equal(new DateTime(2026, 4, 30), SmartTodoParser.Parse("月底付款", referenceDate, resolver).DueDate);
        Assert.Equal("相对日期", SmartTodoParser.Parse("明天交材料", referenceDate, resolver).DateSourceHint);
    }

    [Fact]
    public void Parse_ResolvesHolidayBeforeAndAfterExpressions()
    {
        var referenceDate = new DateTime(2026, 4, 1);
        var resolver = new StubHolidayDateResolver()
            .Add("清明节", HolidayDateRelation.AfterHoliday, new DateTime(2026, 4, 7))
            .Add("五一节", HolidayDateRelation.BeforeHoliday, new DateTime(2026, 4, 30))
            .Add("劳动节", HolidayDateRelation.OnHolidayStart, new DateTime(2026, 5, 1));

        var afterHoliday = SmartTodoParser.Parse("清明节后交材料", referenceDate, resolver);
        var beforeHoliday = SmartTodoParser.Parse("五一节前提交总结", referenceDate, resolver);
        var onHoliday = SmartTodoParser.Parse("劳动节出游", referenceDate, resolver);

        Assert.Equal(new DateTime(2026, 4, 7), afterHoliday.DueDate);
        Assert.Equal("交材料", afterHoliday.Title);
        Assert.Equal("节假日：清明节（放假安排）", afterHoliday.DateSourceHint);

        Assert.Equal(new DateTime(2026, 4, 30), beforeHoliday.DueDate);
        Assert.Equal("提交总结", beforeHoliday.Title);

        Assert.Equal(new DateTime(2026, 5, 1), onHoliday.DueDate);
        Assert.Equal("出游", onHoliday.Title);
    }

    [Fact]
    public void Parse_UsesResolverForNextAvailableHolidayDate()
    {
        var referenceDate = new DateTime(2026, 10, 1);
        var resolver = new StubHolidayDateResolver()
            .Add("劳动节", HolidayDateRelation.OnHolidayStart, new DateTime(2027, 5, 1));

        var result = SmartTodoParser.Parse("劳动节整理活动方案", referenceDate, resolver);

        Assert.Equal(new DateTime(2027, 5, 1), result.DueDate);
        Assert.Equal("整理活动方案", result.Title);
    }

    [Theory]
    [InlineData("10点半 开会", 10, 30, "开会")]
    [InlineData("10点 开会", 10, 0, "开会")]
    [InlineData("10点15分 开会", 10, 15, "开会")]
    [InlineData("9点 买菜", 9, 0, "买菜")]
    public void Parse_ExtractsChineseTimeExpressions(string input, int expectedHour, int expectedMinute, string expectedTitle)
    {
        var result = SmartTodoParser.Parse(input, new DateTime(2026, 4, 1), new StubHolidayDateResolver());

        Assert.Equal(new TimeOnly(expectedHour, expectedMinute), result.DueTime);
        Assert.Equal(expectedTitle, result.Title);
    }

    [Theory]
    [InlineData("下午 3点 提交", 15, 0, "提交")]
    [InlineData("上午 9点 上班", 9, 0, "上班")]
    [InlineData("晚上 8点 看电影", 20, 0, "看电影")]
    [InlineData("中午 12点 吃饭", 12, 0, "吃饭")]
    [InlineData("凌晨 1点 起床", 1, 0, "起床")]
    [InlineData("明早 8点 跑步", 8, 0, "跑步")]
    [InlineData("今早 7点 晨会", 7, 0, "晨会")]
    public void Parse_ResolvesTimePeriodTo24Hour(string input, int expectedHour, int expectedMinute, string expectedTitle)
    {
        var result = SmartTodoParser.Parse(input, new DateTime(2026, 4, 1), new StubHolidayDateResolver());

        Assert.Equal(new TimeOnly(expectedHour, expectedMinute), result.DueTime);
        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal("时段", result.TimeSourceHint);
    }

    [Fact]
    public void Parse_ExtractsTimeFromHHmm_WhenNoDatePresent()
    {
        var result = SmartTodoParser.Parse("提醒我 14:30 提交报告", new DateTime(2026, 4, 1), new StubHolidayDateResolver());

        Assert.Equal(new TimeOnly(14, 30), result.DueTime);
        Assert.Equal("24小时", result.TimeSourceHint);
    }

    [Theory]
    [InlineData("提醒我 14：30 提交报告", 14, 30, "提交报告")]
    [InlineData("12：30 开会", 12, 30, "开会")]
    [InlineData("提醒我 09：05：12 提交", 9, 5, "提交")]
    public void Parse_ExtractsTimeFromFullWidthColonHHmm(string input, int expectedHour, int expectedMinute, string expectedTitle)
    {
        var result = SmartTodoParser.Parse(input, new DateTime(2026, 4, 1), new StubHolidayDateResolver());

        Assert.Equal(new TimeOnly(expectedHour, expectedMinute), result.DueTime);
        Assert.Equal("24小时", result.TimeSourceHint);
        Assert.Equal(expectedTitle, result.Title);
    }

    [Theory]
    [InlineData("提前 10 分钟", 10, "10 分钟")]
    [InlineData("提前半小时", 30, "半小时")]
    [InlineData("提前一刻钟", 15, "一刻钟")]
    [InlineData("提前 1 小时", 60, "1 小时")]
    [InlineData("提前 2 小时", 120, "2 小时")]
    [InlineData("提前 1 天", 1440, "1 天")]
    public void Parse_ExtractsReminderOffsetExpressions(string input, int expectedMinutes, string expectedHintContains)
    {
        var result = SmartTodoParser.Parse(input, new DateTime(2026, 4, 1), new StubHolidayDateResolver());

        Assert.Equal(expectedMinutes, result.ReminderOffsetMinutes);
        Assert.NotNull(result.OffsetSourceHint);
        Assert.Contains(expectedHintContains, result.OffsetSourceHint!);
    }

    [Fact]
    public void Parse_CombinesDateTimeAndOffset()
    {
        var referenceDate = new DateTime(2026, 4, 1);
        var result = SmartTodoParser.Parse("明天 9点 提前 10 分钟 提交周报", referenceDate, new StubHolidayDateResolver());

        Assert.Equal(new DateTime(2026, 4, 2), result.DueDate);
        Assert.Equal(new TimeOnly(9, 0), result.DueTime);
        Assert.Equal(10, result.ReminderOffsetMinutes);
        Assert.Equal("提交周报", result.Title);
    }

    [Fact]
    public void Parse_StripsTimeAndOffsetFragmentsFromTitle()
    {
        var result = SmartTodoParser.Parse("明天下午 3点 提交周报 提前 5 分钟", new DateTime(2026, 4, 1), new StubHolidayDateResolver());

        Assert.Contains("提交周报", result.Title);
        Assert.DoesNotContain("明天下午", result.Title);
        Assert.DoesNotContain("3点", result.Title);
        Assert.DoesNotContain("提前", result.Title);
    }

    [Fact]
    public void Parse_StripsTimeQualifierFromTitle()
    {
        var result = SmartTodoParser.Parse("明天下午 提交周报", new DateTime(2026, 4, 1), new StubHolidayDateResolver());

        Assert.Contains("提交周报", result.Title);
        Assert.DoesNotContain("明天下午", result.Title);
    }

    [Fact]
    public void Parse_TimeOnlyInputProducesNullDueDate()
    {
        var result = SmartTodoParser.Parse("14:30 开会", new DateTime(2026, 4, 1), new StubHolidayDateResolver());

        Assert.Null(result.DueDate);
        Assert.Equal(new TimeOnly(14, 30), result.DueTime);
    }

    [Fact]
    public void Parse_CombinesDateAnd24HourTime()
    {
        var referenceDate = new DateTime(2026, 4, 1);
        var result = SmartTodoParser.Parse(
            "14：30分在405会议室举办2026年第一期\"青年学术沙龙\" 2026-06-12",
            referenceDate,
            new StubHolidayDateResolver());

        Assert.Equal(new DateTime(2026, 6, 12), result.DueDate);
        Assert.Equal(new TimeOnly(14, 30), result.DueTime);
        Assert.DoesNotContain("14：30", result.Title);
        Assert.DoesNotContain("2026-06-12", result.Title);
        Assert.Contains("405会议室", result.Title);
    }

    [Theory]
    [InlineData("四点半 开会", 4, 30, "开会")]
    [InlineData("两点 起床", 2, 0, "起床")]
    [InlineData("四点十五分 出发", 4, 15, "出发")]
    [InlineData("十一点半 吃饭", 11, 30, "吃饭")]
    [InlineData("十二点 午休", 12, 0, "午休")]
    public void Parse_ExtractsChineseNumeralTimeExpressions(string input, int expectedHour, int expectedMinute, string expectedTitle)
    {
        var result = SmartTodoParser.Parse(input, new DateTime(2026, 4, 1), new StubHolidayDateResolver());

        Assert.Equal(new TimeOnly(expectedHour, expectedMinute), result.DueTime);
        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal("中文时间", result.TimeSourceHint);
    }

    [Theory]
    [InlineData("下午四点半 开会", 16, 30, "开会")]
    [InlineData("晚上八点 看电影", 20, 0, "看电影")]
    [InlineData("上午两点 起床", 2, 0, "起床")]
    [InlineData("中午十二点 吃饭", 12, 0, "吃饭")]
    [InlineData("凌晨三点 起床", 3, 0, "起床")]
    public void Parse_ExtractsPeriodWithChineseNumeralTime(string input, int expectedHour, int expectedMinute, string expectedTitle)
    {
        var result = SmartTodoParser.Parse(input, new DateTime(2026, 4, 1), new StubHolidayDateResolver());

        Assert.Equal(new TimeOnly(expectedHour, expectedMinute), result.DueTime);
        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal("时段", result.TimeSourceHint);
    }

    [Theory]
    [InlineData("半小时后 开会", 30)]
    [InlineData("十分钟后 打电话", 10)]
    [InlineData("一小时后 提交", 60)]
    [InlineData("两小时后 休息", 120)]
    [InlineData("一天后 复查", 1440)]
    public void Parse_ExtractsRelativeFutureTime(string input, int offsetMinutes)
    {
        var referenceDate = new DateTime(2026, 4, 1, 14, 0, 0);
        var result = SmartTodoParser.Parse(input, referenceDate, new StubHolidayDateResolver());

        var expectedDateTime = referenceDate.AddMinutes(offsetMinutes);
        Assert.Equal(expectedDateTime.Date, result.DueDate);
        Assert.Equal(TimeOnly.FromDateTime(expectedDateTime), result.DueTime);
        Assert.Equal("相对当前", result.TimeSourceHint);
    }

    [Fact]
    public void Parse_StripsChineseNumeralTimeAndRelativeTimeFromTitle()
    {
        var chineseNumeralResult = SmartTodoParser.Parse("下午四点半 提交周报", new DateTime(2026, 4, 1), new StubHolidayDateResolver());
        Assert.Equal("提交周报", chineseNumeralResult.Title);
        Assert.DoesNotContain("下午四点半", chineseNumeralResult.Title);

        var relativeResult = SmartTodoParser.Parse("半小时后 开会", new DateTime(2026, 4, 1, 14, 0, 0), new StubHolidayDateResolver());
        Assert.Equal("开会", relativeResult.Title);
        Assert.DoesNotContain("半小时后", relativeResult.Title);
    }

    [Fact]
    public void Parse_CombinesDateAndChineseNumeralTime()
    {
        var referenceDate = new DateTime(2026, 4, 1);
        var result = SmartTodoParser.Parse("明天下午四点半 提交周报", referenceDate, new StubHolidayDateResolver());

        Assert.Equal(new DateTime(2026, 4, 2), result.DueDate);
        Assert.Equal(new TimeOnly(16, 30), result.DueTime);
        Assert.Equal("提交周报", result.Title);
    }

    private sealed class StubHolidayDateResolver : IHolidayDateResolver
    {
        private readonly Dictionary<(string HolidayText, HolidayDateRelation Relation), HolidayDateResolution> _dates = new();

        public StubHolidayDateResolver Add(string holidayText, HolidayDateRelation relation, DateTime dueDate, bool usesOfficialSchedule = true)
        {
            var canonicalName = HolidayCatalog.TryGetCanonicalName(holidayText, out var resolvedName) ? resolvedName : holidayText;
            _dates[(holidayText, relation)] = new HolidayDateResolution(dueDate.Date, canonicalName, usesOfficialSchedule);
            return this;
        }

        public HolidayDateResolution? ResolveHolidayDate(string holidayText, HolidayDateRelation relation, DateTime referenceDate)
        {
            return _dates.TryGetValue((holidayText, relation), out var resolution) ? resolution : null;
        }
    }
}
