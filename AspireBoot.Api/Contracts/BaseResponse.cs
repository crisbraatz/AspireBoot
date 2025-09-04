using System.Text.Json.Serialization;

namespace AspireBoot.Api.Contracts;

public class BaseResponse<T>
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Data { get; init; }

    [JsonIgnore] public int? ErrorCode { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ErrorMessage { get; init; }

    [JsonIgnore] public bool IsFailure { get; init; }

    public BaseResponse()
    {
    }

    private BaseResponse(T? data)
    {
        Data = data;
    }

    private BaseResponse(int? errorCode, string? errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        IsFailure = !string.IsNullOrWhiteSpace(ErrorMessage);
    }

    public static BaseResponse<T> Failure(int? errorCode, string? errorMessage) => new(errorCode, errorMessage);

    public static BaseResponse<T> Success(T? data = default) => new(data);
}