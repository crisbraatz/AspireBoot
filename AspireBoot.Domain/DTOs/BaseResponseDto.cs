namespace AspireBoot.Domain.DTOs;

public class BaseResponseDto
{
    public int? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsFailure { get; init; }

    protected BaseResponseDto(int? errorCode = null, string? errorMessage = null)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        IsFailure = !string.IsNullOrWhiteSpace(ErrorMessage);
    }

    public static BaseResponseDto Failure(int? errorCode, string? errorMessage) => new(errorCode, errorMessage);

    public static BaseResponseDto Success() => new();
}

public class BaseResponseDto<T> : BaseResponseDto
{
    public T? Data { get; init; }

    private BaseResponseDto(T? data)
    {
        Data = data;
    }

    private BaseResponseDto(int? errorCode, string? errorMessage) : base(errorCode, errorMessage)
    {
    }

    public new static BaseResponseDto<T> Failure(int? errorCode, string? errorMessage) => new(errorCode, errorMessage);

    public static BaseResponseDto<T> Success(T? data = default) => new(data);
}