using ClosedXML.Excel;
using QPS.Application.Common.Interfaces;

namespace QPS.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#1B2A3A");
    private static readonly XLColor HeaderText = XLColor.FromHtml("#F4F1E8");

    public byte[] BuildWorkbook(IReadOnlyList<ExportSheet> sheets)
    {
        using var workbook = new XLWorkbook();
        foreach (var sheet in sheets)
        {
            var ws = workbook.Worksheets.Add(SafeName(sheet.Name));
            WriteHeader(ws, 1, sheet.Headers);
            var r = 2;
            foreach (var row in sheet.Rows)
            {
                for (var c = 0; c < row.Count; c++)
                    SetValue(ws.Cell(r, c + 1), row[c]);
                r++;
            }
            ws.SheetView.FreezeRows(1);
            if (sheet.Rows.Count > 0)
                ws.Range(1, 1, r - 1, sheet.Headers.Count).SetAutoFilter();
            SizeColumns(ws, sheet.Headers, sheet.Rows);
        }
        return Save(workbook);
    }

    public byte[] QuizCalendarTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Quiz calendar");
        ws.Cell(1, 1).Value = "QUIZ CALENDAR";
        ws.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        var headers = new[] { "WEEK", "DATE", "SUBJECT", "SLOT", "GP", "LOCATION" };
        WriteHeader(ws, 2, headers);

        // Same layout as the university calendar, including merged cells for the groups.
        var rows = new[]
        {
            new[] { "Week 8", "Thursday, April 30, 2026", "Computer Networks", "Gap (3:15-3:45)", "GPI", "M1.105-M1.205" },
            new[] { "", "", "", "", "GPII", "A.020-A.128" },
            new[] { "", "", "", "", "GPIII", "A2.101-A2.201" },
            new[] { "", "Sunday, May 3, 2026", "Theoretical Computer Science", "5TH", "GPII", "0.14-0.31" },
            new[] { "", "Tuesday, May 5, 2026", "Mathematics", "1ST", "GPIII", "0.31-S1.006" },
            new[] { "", "", "", "5TH", "GPI", "C.101-C.102-C.201" },
        };
        for (var i = 0; i < rows.Length; i++)
            for (var c = 0; c < rows[i].Length; c++)
                if (rows[i][c].Length > 0) ws.Cell(3 + i, c + 1).Value = rows[i][c];

        ws.Range("A3:A8").Merge();
        ws.Range("B3:B5").Merge();
        ws.Range("C3:C5").Merge();
        ws.Range("D3:D5").Merge();
        ws.Range("B7:B8").Merge();
        ws.Range("C7:C8").Merge();
        ws.Range("A3:F8").Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Range("A2:F8").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
        foreach (var (col, width) in new[] { (1, 10), (2, 28), (3, 32), (4, 16), (5, 10), (6, 24) })
            ws.Column(col).Width = width;

        var help = workbook.Worksheets.Add("How to fill");
        var lines = new[]
        {
            "One row per student group. Merge WEEK, DATE, SUBJECT and SLOT across the groups that sit together, or repeat the values.",
            "DATE: e.g. Tuesday, April 21, 2026 (an Excel date also works).",
            "SLOT: 1ST, 2ND, 3RD, 4TH, 5TH or Gap (3:15-3:45). You may add START and END columns for custom times instead.",
            "LOCATION: rooms separated by a dash, e.g. C.101-C.102-C.201.",
            "Optional columns: PROCTORS (per room), TYPE (Quiz, Midterm, Final).",
            "Rows without a location (holidays, revision week) are ignored."
        };
        for (var i = 0; i < lines.Length; i++) help.Cell(i + 1, 1).Value = lines[i];
        help.Column(1).Width = 120;
        return Save(workbook);
    }

    public byte[] ScheduleTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("TA schedules");
        var headers = new[] { "Email", "Day", "Slot", "Course", "Type", "Room" };
        WriteHeader(ws, 1, headers);
        var rows = new[]
        {
            new object[] { "ahmed@qps.com", "Saturday", 1, "Cloud Computing", "Lab", "S.323" },
            new object[] { "ahmed@qps.com", "Monday", 3, "Network Security", "Tut", "M.327" },
            new object[] { "sara@qps.com", "Sunday", 2, "Database Systems", "Lab", "C.102" },
        };
        for (var i = 0; i < rows.Length; i++)
            for (var c = 0; c < rows[i].Length; c++)
                SetValue(ws.Cell(2 + i, c + 1), rows[i][c]);
        foreach (var (col, width) in new[] { (1, 28), (2, 14), (3, 8), (4, 34), (5, 10), (6, 12) })
            ws.Column(col).Width = width;
        ws.SheetView.FreezeRows(1);
        return Save(workbook);
    }

    private static void WriteHeader(IXLWorksheet ws, int row, IReadOnlyList<string> headers)
    {
        for (var c = 0; c < headers.Count; c++)
        {
            var cell = ws.Cell(row, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.SetBold().Font.SetFontColor(HeaderText).Fill.SetBackgroundColor(HeaderFill);
        }
    }

    private static void SetValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = "ddd d mmm yyyy";
                break;
            case int i:
                cell.Value = i;
                break;
            case long l:
                cell.Value = l;
                break;
            case double d:
                cell.Value = d;
                break;
            case bool b:
                cell.Value = b ? "Yes" : "No";
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    private static void SizeColumns(IXLWorksheet ws, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        for (var c = 0; c < headers.Count; c++)
        {
            var longest = headers[c].Length;
            foreach (var row in rows.Take(500))
            {
                var text = row.Count > c ? row[c] switch
                {
                    DateTime => "Wed 30 Sep 2026",
                    null => "",
                    var v => v.ToString() ?? ""
                } : "";
                longest = Math.Max(longest, text.Length);
            }
            ws.Column(c + 1).Width = Math.Clamp(longest * 1.15 + 2, 8, 60);
        }
    }

    private static string SafeName(string name)
    {
        var cleaned = new string(name.Where(ch => !"[]:*?/\\".Contains(ch)).ToArray());
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }

    private static byte[] Save(XLWorkbook workbook)
    {
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
