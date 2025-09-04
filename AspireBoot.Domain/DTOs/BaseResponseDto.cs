namespace AspireBoot.Domain.DTOs;

public class BaseResponseDto<T>
{
    public T? Data { get; init; }
    public int? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsFailure { get; init; }

    private BaseResponseDto(T? data)
    {
        Data = data;
    }

    private BaseResponseDto(int errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        IsFailure = !string.IsNullOrWhiteSpace(ErrorMessage);
    }

    public static BaseResponseDto<T> Failure(int errorCode, string errorMessage) => new(errorCode, errorMessage);

    public static BaseResponseDto<T> Success(T? data = default) => new(data);
}