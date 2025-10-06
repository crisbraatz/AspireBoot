using System.Text.Json.Serialization;

namespace AspireBoot.Api.Contracts;

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

    private BaseResponse(T? data)
    {
        Data = data;
    }

    private BaseResponse(int? errorCode, string? errorMessage) : base(errorCode, errorMessage)
    {
    }

    public new static BaseResponse<T> Failure(int? errorCode, string? errorMessage) => new(errorCode, errorMessage);

    public static BaseResponse<T> Success(T? data = default) => new(data);
}