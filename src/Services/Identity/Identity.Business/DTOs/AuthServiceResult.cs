namespace Identity.Business.DTOs;

public sealed record AuthServiceResult<T>(
    T? Value,
    int StatusCode,
    string? Error = null,
    IReadOnlyList<string>? Details = null)
    where T : class;