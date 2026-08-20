using Microsoft.EntityFrameworkCore;

namespace Paga.Application.Common;

/// <summary>
/// Extensões de paginação para <see cref="IQueryable{T}"/>.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Aplica paginação à query e retorna um <see cref="PagedResult{T}"/> com metadados.
    /// </summary>
    /// <typeparam name="T">Tipo dos itens projetados.</typeparam>
    /// <param name="query">Query base já filtrada e ordenada.</param>
    /// <param name="pageNumber">Número da página (normalizado para ≥ 1).</param>
    /// <param name="pageSize">Tamanho da página (limitado entre 1 e 100).</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Resultado paginado com itens e metadados.</returns>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, pageNumber, pageSize, totalCount, totalPages);
    }
}
