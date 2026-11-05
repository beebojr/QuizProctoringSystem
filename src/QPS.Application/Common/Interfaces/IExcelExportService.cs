namespace QPS.Application.Common.Interfaces;

public sealed record ExportSheet(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<object?>> Rows);

public interface IExcelExportService
{
    byte[] BuildWorkbook(IReadOnlyList<ExportSheet> sheets);
    byte[] QuizCalendarTemplate();
    byte[] ScheduleTemplate();
}
