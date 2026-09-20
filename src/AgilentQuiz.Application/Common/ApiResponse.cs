namespace AgilentQuiz.Application.Common;

/// <summary>
/// 统一 API 响应
/// </summary>
public class ApiResponse<T>
{
    public string Code { get; set; } = ErrorCodes.Success;
    public string Message { get; set; } = "success";
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data) => new() { Data = data };
    public static ApiResponse<T> Fail(string code, string message) => new() { Code = code, Message = message };
}

public class ApiResponse
{
    public string Code { get; set; } = ErrorCodes.Success;
    public string Message { get; set; } = "success";

    public static ApiResponse Ok() => new();
    public static ApiResponse Fail(string code, string message) => new() { Code = code, Message = message };
}
