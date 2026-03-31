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
