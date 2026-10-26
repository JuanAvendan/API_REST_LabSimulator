using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LabSimulator.Api.Common;

public sealed record PageQuery
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = 20;
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

internal static class PagingExtensions
{
    public static async Task<PagedResponse<TResult>> ToPagedResponseAsync<TEntity, TResult>(
        this IQueryable<TEntity> query, PageQuery page, Func<TEntity, TResult> map, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page.Page - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TResult>(items.Select(map).ToList(), page.Page, page.PageSize, total);
    }
}
