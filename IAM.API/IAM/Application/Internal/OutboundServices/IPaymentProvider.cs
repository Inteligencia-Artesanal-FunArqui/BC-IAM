namespace OsitoPolar.IAM.Service.Application.Internal.OutboundServices;

/// <summary>
/// Payment provider interface for registration payments.
/// </summary>
public interface IPaymentProvider
{
    string ProviderName { get; }
    Task<PaymentResult> CreatePaymentAsync(PaymentRequest request);
    Task<ProviderPaymentStatus> GetPaymentStatusAsync(string transactionId);
}

public record PaymentRequest
{
    public required decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public required string Description { get; init; }
    public required string CustomerEmail { get; init; }
    public required string CustomerName { get; init; }
    public required string PaymentToken { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public record PaymentResult
{
    public required bool Success { get; init; }
    public string? TransactionId { get; init; }
    public required ProviderPaymentStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
}

public enum ProviderPaymentStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Canceled,
    Refunded
}
