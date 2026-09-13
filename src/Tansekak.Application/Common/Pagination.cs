namespace Tansekak.Application.Common;

public static class Pagination
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, PaginationConstants.MaxPageSize));
}
