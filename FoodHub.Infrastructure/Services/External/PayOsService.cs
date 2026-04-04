using System.Text.Json;
using FoodHub.Application.Interfaces.Branding;
using FoodHub.Application.Interfaces.External;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using FoodHub.Infrastructure.Settings;

namespace FoodHub.Infrastructure.Services.External
{
    public class PayOsService : IPaymentService
    {
        private const int MaxPaymentDescriptionLength = 25;
        private readonly PayOsSettings _settings;
        private readonly IBrandingSettingsProvider _brandingSettingsProvider;
        private readonly PayOSClient _payOsClient;
        private readonly ILogger<PayOsService> _logger;

        public PayOsService(
            IOptions<PayOsSettings> options,
            IBrandingSettingsProvider brandingSettingsProvider,
            ILogger<PayOsService> logger
        )
        {
            _settings = options.Value;
            _brandingSettingsProvider = brandingSettingsProvider;
            _logger = logger;

            _logger.LogInformation(
                "Initializing PayOS client. Section={Section}, ClientIdSet={ClientIdSet}, ApiKeySet={ApiKeySet}, ChecksumKeySet={ChecksumKeySet}, ReturnUrlSet={ReturnUrlSet}, CancelUrlSet={CancelUrlSet}",
                PayOsSettings.SectionName,
                !string.IsNullOrWhiteSpace(_settings.ClientId),
                !string.IsNullOrWhiteSpace(_settings.ApiKey),
                !string.IsNullOrWhiteSpace(_settings.ChecksumKey),
                !string.IsNullOrWhiteSpace(_settings.ReturnUrl),
                !string.IsNullOrWhiteSpace(_settings.CancelUrl)
            );
            _logger.LogDebug(
                "PayOS config values (masked). ClientIdTail={ClientIdTail}, ApiKeyTail={ApiKeyTail}, ChecksumKeyTail={ChecksumKeyTail}",
                MaskTail(_settings.ClientId),
                MaskTail(_settings.ApiKey),
                MaskTail(_settings.ChecksumKey)
            );

            var payOsOptions = new PayOSOptions
            {
                ClientId = _settings.ClientId,
                ApiKey = _settings.ApiKey,
                ChecksumKey = _settings.ChecksumKey
            };

            _payOsClient = new PayOSClient(payOsOptions);

            _logger.LogInformation("PayOS client initialized successfully.");
        }

        public async Task<PaymentLinkResponse> CreatePaymentLinkAsync(
            Order order,
            decimal amount,
            CancellationToken token = default
        )
        {
            var payAmount = (long)Math.Max(1000, amount);
            var branding = await _brandingSettingsProvider.GetOrCreateAsync(token);
            _logger.LogInformation(
                "Creating PayOS payment link. OrderCode={OrderCode}, Amount={Amount}, RestaurantName={RestaurantName}, CancelUrl={CancelUrl}, ReturnUrl={ReturnUrl}",
                order.TransactionCode,
                payAmount,
                branding.RestaurantName,
                _settings.CancelUrl,
                _settings.ReturnUrl
            );

            var request = new CreatePaymentLinkRequest
            {
                OrderCode = order.TransactionCode,
                Amount = payAmount,
                Description = BuildPaymentDescription(branding.RestaurantName, order.TransactionCode),
                CancelUrl = _settings.CancelUrl,
                ReturnUrl = _settings.ReturnUrl
            };

            try
            {
                var createPaymentResult = await _payOsClient.PaymentRequests.CreateAsync(request);

                _logger.LogInformation(
                    "PayOS CreateAsync succeeded. OrderCode={OrderCode}, CheckoutUrlExists={CheckoutUrlExists}, QrCodeExists={QrCodeExists}, Amount={Amount}",
                    order.TransactionCode,
                    !string.IsNullOrWhiteSpace(createPaymentResult.CheckoutUrl),
                    !string.IsNullOrWhiteSpace(createPaymentResult.QrCode),
                    createPaymentResult.Amount
                );

                return new PaymentLinkResponse
                {
                    CheckoutUrl = createPaymentResult.CheckoutUrl ?? string.Empty,
                    QrCode = createPaymentResult.QrCode ?? string.Empty,
                    Bin = createPaymentResult.Bin ?? string.Empty,
                    AccountNumber = createPaymentResult.AccountNumber ?? string.Empty,
                    AccountName = createPaymentResult.AccountName ?? string.Empty,
                    Amount = createPaymentResult.Amount,
                    Description = createPaymentResult.Description ?? string.Empty,
                    Currency = createPaymentResult.Currency ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "PayOS CreateAsync failed. OrderCode={OrderCode}, Amount={Amount}, RestaurantName={RestaurantName}",
                    order.TransactionCode,
                    payAmount,
                    branding.RestaurantName
                );
                throw;
            }
        }

        public async Task<long> VerifyWebhookDataAsync(string webhookBody)
        {
            var webhook = JsonSerializer.Deserialize<Webhook>(
                webhookBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (webhook == null)
            {
                _logger.LogWarning("PayOS webhook deserialization failed: body was null or invalid JSON.");
                throw new ArgumentException("Invalid webhook data");
            }

            _logger.LogInformation(
                "Verifying PayOS webhook. BodyLength={BodyLength}",
                webhookBody?.Length ?? 0
            );

            try
            {
                var verifiedData = await _payOsClient.Webhooks.VerifyAsync(webhook);
                if (verifiedData == null)
                {
                    _logger.LogWarning("PayOS webhook verification returned null.");
                    throw new UnauthorizedAccessException("Webhook verification failed");
                }

                _logger.LogInformation("PayOS webhook verification succeeded.");

                return verifiedData.OrderCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOS webhook verification failed.");
                throw;
            }
        }

        public async Task<string> GetPaymentStatusAsync(
            long orderCode,
            CancellationToken token = default
        )
        {
            _logger.LogInformation("Fetching PayOS payment status. OrderCode={OrderCode}", orderCode);

            try
            {
                var paymentInfo = await _payOsClient.PaymentRequests.GetAsync(orderCode);
                _logger.LogInformation(
                    "Fetched PayOS payment status successfully. OrderCode={OrderCode}, Status={Status}",
                    orderCode,
                    paymentInfo.Status
                );
                return paymentInfo.Status.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch PayOS payment status. OrderCode={OrderCode}", orderCode);
                throw;
            }
        }

        private static string MaskTail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "<empty>";
            }

            return value.Length <= 4 ? "****" : $"****{value[^4..]}";
        }

        private static string BuildPaymentDescription(string restaurantName, int orderCode)
        {
            var orderCodeText = orderCode.ToString();
            var safeRestaurantName = string.IsNullOrWhiteSpace(restaurantName)
                ? "FoodHub"
                : restaurantName.Trim();

            var maxRestaurantLength = MaxPaymentDescriptionLength - orderCodeText.Length - 1;
            if (maxRestaurantLength < 1)
            {
                return orderCodeText.Length > MaxPaymentDescriptionLength
                    ? orderCodeText[..MaxPaymentDescriptionLength]
                    : orderCodeText;
            }

            if (safeRestaurantName.Length > maxRestaurantLength)
            {
                safeRestaurantName = safeRestaurantName[..maxRestaurantLength].TrimEnd();
            }

            var description = $"{safeRestaurantName} {orderCodeText}";
            return description.Length <= MaxPaymentDescriptionLength
                ? description
                : description[..MaxPaymentDescriptionLength];
        }
    }
}
