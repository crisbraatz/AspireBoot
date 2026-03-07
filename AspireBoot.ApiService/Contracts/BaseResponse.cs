using System.Text.Json.Serialization;

namespace AspireBoot.ApiService.Contracts;

public class BaseResponse
{
    [JsonIgnore] public int? ErrorCode { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ErrorMessage { get; init; }

    [JsonIgnore] public bool IsFailure { get; init; }

    protected BaseResponse(int? errorCode = null, string? errorMessage = null)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        IsFailure = !string.IsNullOrWhiteSpace(ErrorMessage);
    }

    public static BaseResponse Failure(int? errorCode, string? errorMessage) => new(errorCode, errorMessage);

    public static BaseResponse Success() => new();
}

public class BaseResponse<T> : BaseResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Data { get; init; }

    protected BaseResponse(T? data)
    {
        Data = data;
    }

    protected BaseResponse(int? errorCode, string? errorMessage) : base(errorCode, errorMessage)
    {
    }

#pragma warning disable CA1000
    public static new BaseResponse<T> Failure(int? errorCode, string? errorMessage) => new(errorCode, errorMessage);

    public static BaseResponse<T> Success(T? data = default) => new(data);
#pragma warning restore CA1000
}
