using Stripe;

namespace OsitoPolar.IAM.Service.Application.Internal.OutboundServices;

/// <summary>
/// Stripe payment provider implementation for registration payments.
/// </summary>
public class StripePaymentProvider : IPaymentProvider
{
    private readonly ILogger<StripePaymentProvider> _logger;
    private readonly IConfiguration _configuration;

    public StripePaymentProvider(ILogger<StripePaymentProvider> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Configure Stripe API key
        var stripeSecretKey = _configuration["Stripe:SecretKey"];
        if (!string.IsNullOrEmpty(stripeSecretKey))
        {
            StripeConfiguration.ApiKey = stripeSecretKey;
        }
    }

    public string ProviderName => "Stripe";

    public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request)
    {
        try
        {
            _logger.LogInformation("[Stripe] Creating payment for {Amount} {Currency}",
                request.Amount, request.Currency);

            // Create payment method from token
            var paymentMethodService = new PaymentMethodService();

            // Create payment intent
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Convert to cents
                Currency = request.Currency.ToLower(),
                PaymentMethod = request.PaymentToken,
                Confirm = true,
                Description = request.Description,
                ReceiptEmail = request.CustomerEmail,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                },
                Metadata = request.Metadata
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation("[Stripe] Payment intent created: {PaymentIntentId}, Status: {Status}",
                paymentIntent.Id, paymentIntent.Status);

            var status = paymentIntent.Status switch
            {
                "succeeded" => ProviderPaymentStatus.Succeeded,
                "processing" => ProviderPaymentStatus.Processing,
                "requires_payment_method" => ProviderPaymentStatus.Failed,
                "requires_confirmation" => ProviderPaymentStatus.Pending,
                "canceled" => ProviderPaymentStatus.Canceled,
                _ => ProviderPaymentStatus.Pending
            };

            return new PaymentResult
            {
                Success = paymentIntent.Status == "succeeded",
                TransactionId = paymentIntent.Id,
                Status = status,
                Amount = request.Amount,
                Currency = request.Currency
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[Stripe] Payment failed: {Message}", ex.Message);
            return new PaymentResult
            {
                Success = false,
                Status = ProviderPaymentStatus.Failed,
                ErrorMessage = ex.Message,
                Amount = request.Amount,
                Currency = request.Currency
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Stripe] Unexpected error during payment");
            return new PaymentResult
            {
                Success = false,
                Status = ProviderPaymentStatus.Failed,
                ErrorMessage = ex.Message,
                Amount = request.Amount,
                Currency = request.Currency
            };
        }
    }

    public async Task<ProviderPaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(transactionId);

            return paymentIntent.Status switch
            {
                "succeeded" => ProviderPaymentStatus.Succeeded,
                "processing" => ProviderPaymentStatus.Processing,
                "requires_payment_method" => ProviderPaymentStatus.Failed,
                "canceled" => ProviderPaymentStatus.Canceled,
                _ => ProviderPaymentStatus.Pending
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Stripe] Error getting payment status for {TransactionId}", transactionId);
            return ProviderPaymentStatus.Failed;
        }
    }
}
