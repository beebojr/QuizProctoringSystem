using QPS.Domain.Entities;

namespace QPS.Application.Common.Interfaces;

public interface IExcelParserService
{
    Task<List<Quiz>> ParseQuizExcelAsync(Stream fileStream, Guid semesterId);
}