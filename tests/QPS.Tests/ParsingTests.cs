using ClosedXML.Excel;
using QPS.Application.Common.Services;
using QPS.Domain.Scheduling;
using QPS.Infrastructure.Services;

namespace QPS.Tests;

public static class ParsingTests
{
    [Test]
    public static void Slot_labels_from_the_calendar()
    {
        Assert.True(SlotCatalog.TryParse("5TH", out var s5) && s5.Number == 5, "5TH");
        Assert.True(SlotCatalog.TryParse("1st", out var s1) && s1.Start == new TimeOnly(8, 30), "1st");
        Assert.True(SlotCatalog.TryParse("Gap (3:15-3:45)", out var gap) && gap.IsGap && gap.Start == new TimeOnly(15, 15), "gap");
        Assert.True(SlotCatalog.TryParse("Slot 3", out var s3) && s3.Number == 3, "Slot 3");
        Assert.True(!SlotCatalog.TryParse("7TH", out _), "7TH is not a slot");
    }

    [Test]
    public static void Dates_in_university_formats()
    {
        Assert.True(CalendarText.TryParseDate("Tuesday, April 21, 2026", out var a) && a == new DateOnly(2026, 4, 21), "long form");
        Assert.True(CalendarText.TryParseDate("April 21, 2026", out var b) && b == new DateOnly(2026, 4, 21), "no weekday");
        Assert.True(CalendarText.TryParseDate("2026-04-21", out var c) && c == new DateOnly(2026, 4, 21), "ISO");
        Assert.True(CalendarText.TryParseDate("21/4/2026", out var d) && d == new DateOnly(2026, 4, 21), "day first");
        Assert.True(CalendarText.TryParseDate("Sunday,  May 3,  2026 ", out var e) && e == new DateOnly(2026, 5, 3), "extra spaces");
        Assert.True(!CalendarText.TryParseDate("EID Vacation", out _), "not a date");
    }

    [Test]
    public static void Rooms_are_split_and_cleaned()
    {
        Assert.Equal("C.101|C.102|C.201", string.Join("|", CalendarText.SplitRooms("C.101-C.102-C.201")), "dash list");
        Assert.Equal("C.201", string.Join("|", CalendarText.SplitRooms("In the lecture C.201")), "phrase");
        Assert.Equal("0.14|0.31", string.Join("|", CalendarText.SplitRooms("0.14-0.31")), "numeric rooms");
        Assert.Equal("A2.201|A.20", string.Join("|", CalendarText.SplitRooms("A2.201 - A.20 - a2.201")), "dedupe");
    }

    [Test]
    public static void Explicit_time_ranges_are_read_as_afternoon_when_needed()
    {
        Assert.True(CalendarText.TryParseSlot("3:15-3:45", out var s, out var e, out var n) && s == new TimeOnly(15, 15) && n == 0, "gap by time");
        Assert.True(CalendarText.TryParseSlot("9:00 - 11:00", out var s2, out var e2, out var n2) && s2 == new TimeOnly(9, 0) && e2 == new TimeOnly(11, 0) && n2 is null, "custom");
    }

    [Test]
    public static void Excel_calendar_with_merged_cells_is_expanded()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Calendar");
        ws.Cell(1, 2).Value = "INFORMATICS QUIZ CALENDAR";
        ws.Range(1, 2, 1, 6).Merge();
        var headers = new[] { "WEEK", "DATE", "SUBJECT", "SLOT", "GP", "LOCATION" };
        for (var i = 0; i < headers.Length; i++) ws.Cell(2, i + 1).Value = headers[i];
        ws.Cell(3, 1).Value = "Week 8";
        ws.Cell(3, 2).Value = "Thursday, April 30, 2026";
        ws.Cell(3, 3).Value = "Computer Networks";
        ws.Cell(3, 4).Value = "Gap (3:15-3:45)";
        ws.Cell(3, 5).Value = "GPI"; ws.Cell(3, 6).Value = "M1.105-M1.205";
        ws.Cell(4, 5).Value = "GPII"; ws.Cell(4, 6).Value = "A.020-A.128";
        ws.Cell(5, 5).Value = "GPIII"; ws.Cell(5, 6).Value = "A2.101-A2.201";
        ws.Range(3, 1, 6, 1).Merge(); ws.Range(3, 2, 5, 2).Merge(); ws.Range(3, 3, 5, 3).Merge(); ws.Range(3, 4, 5, 4).Merge();
        ws.Cell(6, 2).Value = new DateTime(2026, 5, 2);
        ws.Cell(6, 3).Value = "EID Vacation";
        ws.Range(6, 3, 6, 6).Merge();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        var result = new ExcelParserService().ReadQuizCalendar(ms);

        Assert.Equal(3, result.Rows.Count, "three group rows");
        Assert.True(result.Rows.All(r => r.Subject == "Computer Networks" && r.Week == "Week 8" && r.Slot == "Gap (3:15-3:45)"), "merged values copied to every row");
        Assert.Equal("GPIII", result.Rows[2].Group, "group of last row");
        Assert.True(result.Notes.Any(n => n.Contains("EID Vacation")), "banner skipped with a note");
    }
}
