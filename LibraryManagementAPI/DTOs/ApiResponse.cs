namespace LibraryManagementAPI.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResponse(T? data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> FailureResponse(string message, IEnumerable<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors?.ToList() ?? new List<string>() };
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Create(IEnumerable<T> items, int page, int pageSize, int totalCount) => new()
    {
        Items = items.ToList(),
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
