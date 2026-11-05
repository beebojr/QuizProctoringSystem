using System.Globalization;
using ClosedXML.Excel;
using QPS.Application.Common.Interfaces;

namespace QPS.Infrastructure.Services;

/// <summary>
/// Reads the university's Excel files. The quiz calendar merges WEEK, DATE, SUBJECT and SLOT
/// cells across the rows of every group, so each value is taken from the top-left cell of its
/// merged range. Banner rows such as "EID Vacation" (one cell merged across the columns) are skipped.
/// </summary>
public class ExcelParserService : IExcelParserService
{
    private enum Col { Week, Date, Subject, Slot, Group, Location, Start, End, Proctors, Type }

    private enum SCol { Email, Day, Slot, Course, Type, Room }

    public CalendarReadResult ReadQuizCalendar(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var notes = new List<string>();

        foreach (var sheet in workbook.Worksheets)
        {
            var used = sheet.RangeUsed();
            if (used is null) continue;
            var merged = new MergeMap(sheet);
            var lastRow = used.LastRow().RowNumber();
            var lastCol = used.LastColumn().ColumnNumber();

            var header = FindHeader(sheet, merged, lastRow, lastCol, MapCalendarHeader,
                required: new[] { Col.Date, Col.Subject, Col.Location });
            if (header is null) continue;
            var (headerRow, columns) = header.Value;
            if (!columns.ContainsKey(Col.Slot) && !(columns.ContainsKey(Col.Start) && columns.ContainsKey(Col.End)))
            {
                notes.Add($"Sheet \"{sheet.Name}\" has no SLOT column (or START/END times); it was skipped.");
                continue;
            }

            var rows = new List<RawCalendarRow>();
            for (var r = headerRow + 1; r <= lastRow; r++)
            {
                string? Get(Col c) => columns.TryGetValue(c, out var col) ? merged.Text(r, col) : null;

                var subject = Get(Col.Subject);
                var location = Get(Col.Location);
                if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(location)) continue;

                // A banner merged across SUBJECT…LOCATION (holidays, "Revision week", "Final exams").
                if (merged.SameRange(r, columns[Col.Subject], r, columns[Col.Location]))
                {
                    notes.Add($"Row {r}: \"{subject?.Trim()}\" spans the whole row — skipped.");
                    continue;
                }

                // A header row repeated further down the sheet.
                if (string.Equals(subject?.Trim(), "SUBJECT", StringComparison.OrdinalIgnoreCase)) continue;

                rows.Add(new RawCalendarRow(r, Get(Col.Week), Get(Col.Date), subject, Get(Col.Slot), Get(Col.Group),
                    location, Get(Col.Start), Get(Col.End), Get(Col.Proctors), Get(Col.Type)));
            }

            return new CalendarReadResult(rows, notes, sheet.Name);
        }

        throw new InvalidDataException("No sheet has the DATE, SUBJECT and LOCATION headers.");
    }

    public IReadOnlyList<RawScheduleRow> ReadSchedule(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        foreach (var sheet in workbook.Worksheets)
        {
            var used = sheet.RangeUsed();
            if (used is null) continue;
            var merged = new MergeMap(sheet);
            var lastRow = used.LastRow().RowNumber();
            var lastCol = used.LastColumn().ColumnNumber();
            var header = FindHeader(sheet, merged, lastRow, lastCol, MapScheduleHeader,
                required: new[] { SCol.Email, SCol.Day, SCol.Slot, SCol.Course });
            if (header is null) continue;
            var (headerRow, columns) = header.Value;

            var rows = new List<RawScheduleRow>();
            for (var r = headerRow + 1; r <= lastRow; r++)
            {
                string? Get(SCol c) => columns.TryGetValue(c, out var col) ? merged.Text(r, col) : null;
                var email = Get(SCol.Email);
                var day = Get(SCol.Day);
                if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(day)) continue;
                rows.Add(new RawScheduleRow(r, email, day, Get(SCol.Slot), Get(SCol.Course), Get(SCol.Type), Get(SCol.Room)));
            }
            return rows;
        }

        throw new InvalidDataException("No sheet has the Email, Day, Slot and Course headers.");
    }

    private static (int Row, Dictionary<TCol, int> Columns)? FindHeader<TCol>(
        IXLWorksheet sheet, MergeMap merged, int lastRow, int lastCol, Func<string, TCol?> map, TCol[] required)
        where TCol : struct, Enum
    {
        var scanTo = Math.Min(lastRow, 40);
        for (var r = 1; r <= scanTo; r++)
        {
            var columns = new Dictionary<TCol, int>();
            for (var c = 1; c <= lastCol; c++)
            {
                var text = merged.Text(r, c);
                if (string.IsNullOrWhiteSpace(text)) continue;
                var kind = map(text);
                if (kind is { } k && !columns.ContainsKey(k))
                    columns[k] = c;
            }
            if (required.All(columns.ContainsKey))
                return (r, columns);
        }
        return null;
    }

    private static Col? MapCalendarHeader(string text)
    {
        var t = text.Trim().ToLowerInvariant();
        if (t.Length > 40) return null;
        if (t.StartsWith("week")) return Col.Week;
        if (t.Contains("date")) return Col.Date;
        if (t.Contains("subject") || t.Contains("course")) return Col.Subject;
        if (t.Contains("slot") || t.Contains("period")) return Col.Slot;
        if (t is "gp" or "gp." || t.StartsWith("group")) return Col.Group;
        if (t.Contains("location") || t.Contains("room") || t.Contains("hall") || t.Contains("venue")) return Col.Location;
        if (t is "start" or "start time" or "from") return Col.Start;
        if (t is "end" or "end time" or "to") return Col.End;
        if (t.Contains("proctor") || t.Contains("invigilator")) return Col.Proctors;
        if (t is "type" or "exam type") return Col.Type;
        return null;
    }

    private static SCol? MapScheduleHeader(string text)
    {
        var t = text.Trim().ToLowerInvariant();
        if (t.Contains("email") || t.Contains("e-mail")) return SCol.Email;
        if (t is "day" or "weekday") return SCol.Day;
        if (t.Contains("slot")) return SCol.Slot;
        if (t.Contains("course") || t.Contains("subject")) return SCol.Course;
        if (t is "type" or "kind" or "session type") return SCol.Type;
        if (t.Contains("room") || t.Contains("location")) return SCol.Room;
        return null;
    }

    /// <summary>Resolves every cell to the value of the top-left cell of its merged range.</summary>
    private sealed class MergeMap
    {
        private readonly IXLWorksheet _sheet;
        private readonly Dictionary<(int, int), (int Row, int Col, int Id)> _owner = new();

        public MergeMap(IXLWorksheet sheet)
        {
            _sheet = sheet;
            var id = 0;
            foreach (var range in sheet.MergedRanges)
            {
                id++;
                var first = range.FirstCell().Address;
                var last = range.LastCell().Address;
                for (var r = first.RowNumber; r <= last.RowNumber; r++)
                for (var c = first.ColumnNumber; c <= last.ColumnNumber; c++)
                    _owner[(r, c)] = (first.RowNumber, first.ColumnNumber, id);
            }
        }

        public bool SameRange(int r1, int c1, int r2, int c2) =>
            _owner.TryGetValue((r1, c1), out var a) && _owner.TryGetValue((r2, c2), out var b) && a.Id == b.Id;

        public string? Text(int row, int col)
        {
            var (r, c) = _owner.TryGetValue((row, col), out var o) ? (o.Row, o.Col) : (row, col);
            var cell = _sheet.Cell(r, c);
            if (cell.IsEmpty()) return null;

            switch (cell.DataType)
            {
                case XLDataType.DateTime:
                    return cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                case XLDataType.TimeSpan:
                    var ts = cell.GetTimeSpan();
                    return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}";
                case XLDataType.Number:
                    // Integers (weeks, proctor counts) without trailing ".0".
                    var n = cell.GetDouble();
                    return n == Math.Floor(n) ? ((long)n).ToString(CultureInfo.InvariantCulture) : cell.GetFormattedString();
                default:
                    var text = cell.GetFormattedString();
                    return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
        }
    }
}
