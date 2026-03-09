namespace AspireBoot.ApiService.Validators;

public sealed class ValidatorResponse
{
    public int? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsFailure { get; init; }

    private ValidatorResponse(int? errorCode = null, string? errorMessage = null)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        IsFailure = !string.IsNullOrWhiteSpace(ErrorMessage);
    }

    public static ValidatorResponse Failure(int? errorCode, string? errorMessage) => new(errorCode, errorMessage);

    public static ValidatorResponse Success() => new();
}
