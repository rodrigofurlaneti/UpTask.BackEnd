using MediatR;

namespace UpTask.Application.Common.Models;

// Pagination
public sealed record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public sealed record PaginationParams(int Page = 1, int PageSize = 20)
{
    public int Skip => (Page - 1) * PageSize;
}

// Standard API Response
public sealed record ApiResponse<T>(bool Success, T? Data, string? Message = null, IEnumerable<string>? Errors = null)
{
    public static ApiResponse<T> Ok(T data, string? message = null) => new(true, data, message);
    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) => new(false, default, message, errors);
}

public sealed record ApiResponse(bool Success, string? Message = null, IEnumerable<string>? Errors = null)
{
    public static ApiResponse Ok(string? message = null) => new(true, message);
    public static ApiResponse Fail(string message, IEnumerable<string>? errors = null) => new(false, message, errors);
}
