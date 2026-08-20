namespace Paga.Application.Common;

/// <summary>
/// Envelope de paginação genérico retornado por todas as listagens da API.
/// </summary>
/// <typeparam name="T">Tipo dos itens na página.</typeparam>
/// <param name="Items">Itens da página corrente.</param>
/// <param name="PageNumber">Número da página (começa em 1).</param>
/// <param name="PageSize">Tamanho da página solicitado (1–100).</param>
/// <param name="TotalCount">Total de registros que satisfazem os filtros.</param>
/// <param name="TotalPages">Total de páginas disponíveis.</param>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages
);
