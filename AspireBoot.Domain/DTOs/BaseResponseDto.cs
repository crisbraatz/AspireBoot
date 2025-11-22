namespace AspireBoot.Domain.DTOs;

public class BaseResponseDto
{
    public int? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public bool IsFailure { get; }

    protected BaseResponseDto(int? errorCode = null, string? errorMessage = null)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        IsFailure = ErrorCode is not null || !string.IsNullOrWhiteSpace(ErrorMessage);
    }

    public static BaseResponseDto Failure(int? errorCode, string? errorMessage) => new(errorCode, errorMessage);

    public static BaseResponseDto Success() => new();
}

public class BaseResponseDto<T> : BaseResponseDto
{
    public T? Data { get; }

    protected BaseResponseDto(T? data)
    {
        Data = data;
    }

    protected BaseResponseDto(int? errorCode, string? errorMessage) : base(errorCode, errorMessage)
    {
    }

#pragma warning disable CA1000
    public static new BaseResponseDto<T> Failure(int? errorCode, string? errorMessage) => new(errorCode, errorMessage);

    public static BaseResponseDto<T> Success(T? data = default) => new(data);
#pragma warning restore CA1000
}
