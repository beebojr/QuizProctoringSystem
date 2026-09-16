using Microsoft.EntityFrameworkCore;

namespace QPS.Application.Common.Models;

public class PaginatedList<T>
{
    public List<T> Items { get; private set; } = new();
    public int PageNumber { get; private set; }
    public int PageSize { get; private set; }
    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    private PaginatedList() { }

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var count = await source.CountAsync(ct);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedList<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = count
        };
    }
}