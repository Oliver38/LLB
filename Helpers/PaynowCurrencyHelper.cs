using LLB.Data;
using LLB.Models;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using Webdev.Payments;

namespace LLB.Helpers
{
    public static class PaynowCurrencyHelper
    {
        public const string UsdCurrency = "USD";
        public const string ZwgCurrency = "ZWG";
        public const string LiveMode = "LIVE";
        public const string TestMode = "TEST";
        public const string ReturnBaseUrl = "https://llb.pfms.gov.zw";
        public const string LiveReturnBaseUrl = "https://llb.pfms.gov.zw";
        public const string TestReturnBaseUrl = "http://localhost:5046";

        private const string LegacyIntegrationId = "7175";
        private const string LegacyIntegrationKey = "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0";
        private const string UsdIntegrationId = "24194";
        private const string UsdIntegrationKey = "7ba282f6-f810-49e1-add3-9bca84019424";
        private const string ZwgIntegrationId = "24195";
        private const string ZwgIntegrationKey = "7c72bdc4-f068-48f2-acf4-cfb60b4e6bf3";
        private const string MetadataPrefix = "PAYNOW_CURRENCY|";
        private static string CurrentPaymentMode = LiveMode;
        private static readonly Dictionary<string, PaynowIntegrationCredentials> IntegrationCredentials = new(StringComparer.OrdinalIgnoreCase)
        {
            [BuildCredentialKey(UsdCurrency, LiveMode)] = new PaynowIntegrationCredentials(UsdIntegrationId, UsdIntegrationKey),
            [BuildCredentialKey(ZwgCurrency, LiveMode)] = new PaynowIntegrationCredentials(ZwgIntegrationId, ZwgIntegrationKey)
        };

        public static void Configure(IConfigurationSection configuration)
        {
            foreach (var currencySection in configuration.GetSection("Currencies").GetChildren())
            {
                var currency = NormalizeCurrency(currencySection.Key);
                foreach (var modeSection in currencySection.GetChildren())
                {
                    var mode = NormalizePaymentMode(modeSection.Key);
                    var integrationId = modeSection["IntegrationId"];
                    var integrationKey = modeSection["IntegrationKey"];

                    if (!string.IsNullOrWhiteSpace(integrationId) && !string.IsNullOrWhiteSpace(integrationKey))
                    {
                        IntegrationCredentials[BuildCredentialKey(currency, mode)] = new PaynowIntegrationCredentials(integrationId, integrationKey);
                    }
                }
            }
        }

        public static void SetCurrentPaymentMode(string? paymentMode)
        {
            CurrentPaymentMode = string.Equals(paymentMode, TestMode, StringComparison.OrdinalIgnoreCase)
                ? TestMode
                : LiveMode;
        }

        public static void UpdateCredentials(string? currency, string? paymentMode, string? integrationId, string? integrationKey)
        {
            var normalizedCurrency = NormalizeCurrency(currency);
            var normalizedPaymentMode = NormalizePaymentMode(paymentMode);
            var credentialKey = BuildCredentialKey(normalizedCurrency, normalizedPaymentMode);

            if (string.IsNullOrWhiteSpace(integrationId) || string.IsNullOrWhiteSpace(integrationKey))
            {
                IntegrationCredentials.Remove(credentialKey);
                return;
            }

            IntegrationCredentials[credentialKey] = new PaynowIntegrationCredentials(integrationId, integrationKey);
        }

        public static PaynowCurrencyContext BuildPaymentContext(AppDbContext db, decimal usdAmount, string? currency, string? paymentMode = null)
        {
            var normalizedCurrency = NormalizeCurrency(currency);
            var normalizedPaymentMode = NormalizePaymentMode(paymentMode);
            var credentials = GetCredentials(normalizedCurrency, normalizedPaymentMode);
            if (normalizedCurrency == ZwgCurrency)
            {
                var latestRate = db.ExchangeRate
                    .Where(rate => rate.ZWGrate.HasValue && rate.ZWGrate.Value > 0)
                    .OrderByDescending(rate => rate.DateUpdated)
                    .ThenByDescending(rate => rate.DateAdded)
                    .FirstOrDefault();

                if (latestRate?.ZWGrate == null || latestRate.ZWGrate.Value <= 0)
                {
                    throw new InvalidOperationException("The ZWG exchange rate has not been configured. Update the exchange rate before making a ZWG payment.");
                }

                var exchangeRate = Convert.ToDecimal(latestRate.ZWGrate.Value);
                return new PaynowCurrencyContext(
                    ZwgCurrency,
                    Math.Round(usdAmount * exchangeRate, 2, MidpointRounding.AwayFromZero),
                    usdAmount,
                    exchangeRate,
                    credentials.IntegrationId,
                    normalizedPaymentMode);
            }

            return new PaynowCurrencyContext(
                UsdCurrency,
                Math.Round(usdAmount, 2, MidpointRounding.AwayFromZero),
                usdAmount,
                null,
                credentials.IntegrationId,
                normalizedPaymentMode);
        }

        public static Paynow CreatePaynow(PaynowCurrencyContext context)
        {
            return CreatePaynow(context.Currency, context.PaymentMode);
        }

        public static Paynow CreatePaynow(Payments? payment)
        {
            var currency = GetStoredCurrency(payment);
            var paymentMode = GetStoredPaymentMode(payment);
            return string.IsNullOrWhiteSpace(currency)
                ? CreateLegacyPaynow()
                : CreatePaynow(currency, paymentMode);
        }

        public static Paynow CreatePaynow(string? currency, string? paymentMode = null)
        {
            var normalizedCurrency = NormalizeCurrency(currency);
            var normalizedPaymentMode = NormalizePaymentMode(paymentMode);
            var credentials = GetCredentials(normalizedCurrency, normalizedPaymentMode);
            return new Paynow(credentials.IntegrationId, credentials.IntegrationKey);
        }

        public static string NormalizeCurrency(string? currency)
        {
            return string.Equals(currency, ZwgCurrency, StringComparison.OrdinalIgnoreCase)
                ? ZwgCurrency
                : UsdCurrency;
        }

        public static string NormalizePaymentMode(string? paymentMode)
        {
            if (string.IsNullOrWhiteSpace(paymentMode))
            {
                return CurrentPaymentMode;
            }

            return string.Equals(paymentMode, TestMode, StringComparison.OrdinalIgnoreCase)
                ? TestMode
                : LiveMode;
        }

        public static bool IsPaymentModeConfigured(string? paymentMode)
        {
            var normalizedPaymentMode = NormalizePaymentMode(paymentMode);
            return HasCredentials(UsdCurrency, normalizedPaymentMode)
                && HasCredentials(ZwgCurrency, normalizedPaymentMode);
        }

        public static string BuildReturnUrl(string pathAndQuery)
        {
            return BuildReturnUrl(pathAndQuery, CurrentPaymentMode);
        }

        public static string BuildReturnUrl(string pathAndQuery, string? paymentMode)
        {
            var path = string.IsNullOrWhiteSpace(pathAndQuery) ? "/" : pathAndQuery;
            if (path.StartsWith(LiveReturnBaseUrl, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(TestReturnBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            if (!path.StartsWith("/", StringComparison.Ordinal))
            {
                path = "/" + path;
            }

            return GetReturnBaseUrl(paymentMode).TrimEnd('/') + path;
        }

        public static string GetReturnBaseUrl(string? paymentMode)
        {
            return string.Equals(NormalizePaymentMode(paymentMode), TestMode, StringComparison.OrdinalIgnoreCase)
                ? TestReturnBaseUrl
                : LiveReturnBaseUrl;
        }

        public static void ApplyCurrency(Payments transaction, PaynowCurrencyContext context)
        {
            transaction.Amount = context.PaynowAmount;
            transaction.UsdAmount = context.UsdAmount;
            transaction.ExchangeRate = context.ExchangeRate;
            transaction.Currency = context.Currency;
            transaction.IntegrationId = context.IntegrationId;
            transaction.PopDoc = BuildMetadata(context);
        }

        public static dynamic PollTransaction(Payments? payment)
        {
            if (payment == null || string.IsNullOrWhiteSpace(payment.PollUrl))
            {
                throw new InvalidOperationException("The Paynow transaction could not be found.");
            }

            Exception? lastHashMismatch = null;
            foreach (var paynow in CreatePollingPaynows(payment))
            {
                try
                {
                    return paynow.PollTransaction(payment.PollUrl);
                }
                catch (Exception ex) when (IsHashMismatchException(ex))
                {
                    lastHashMismatch = ex;
                }
            }

            throw lastHashMismatch ?? new InvalidOperationException("The Paynow transaction could not be verified.");
        }

        public static bool IsHashMismatchException(Exception ex)
        {
            if (string.Equals(ex.GetType().Name, "HashMismatchException", StringComparison.Ordinal))
            {
                return true;
            }

            if (ex is AggregateException aggregateException)
            {
                return aggregateException.Flatten().InnerExceptions.Any(IsHashMismatchException);
            }

            return ex.InnerException != null && IsHashMismatchException(ex.InnerException);
        }

        private static IEnumerable<Paynow> CreatePollingPaynows(Payments payment)
        {
            var storedCurrency = GetStoredCurrency(payment);
            var labels = new List<string>();
            string primaryLabel;

            if (string.IsNullOrWhiteSpace(storedCurrency))
            {
                primaryLabel = "LEGACY";
            }
            else
            {
                primaryLabel = BuildCredentialLabel(storedCurrency, GetStoredPaymentMode(payment));
            }

            labels.Add(primaryLabel);
            labels.Add(BuildCredentialLabel(UsdCurrency, LiveMode));
            labels.Add(BuildCredentialLabel(ZwgCurrency, LiveMode));
            labels.Add(BuildCredentialLabel(UsdCurrency, TestMode));
            labels.Add(BuildCredentialLabel(ZwgCurrency, TestMode));
            labels.Add("LEGACY");

            foreach (var label in labels.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(label, "LEGACY", StringComparison.OrdinalIgnoreCase))
                {
                    yield return CreateLegacyPaynow();
                    continue;
                }

                Paynow paynow;
                try
                {
                    paynow = CreatePaynow(ParseCurrencyLabel(label), ParseModeLabel(label));
                }
                catch (InvalidOperationException) when (!string.Equals(label, primaryLabel, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return paynow;
            }
        }

        private static Paynow CreateLegacyPaynow()
        {
            return new Paynow(LegacyIntegrationId, LegacyIntegrationKey);
        }

        private static string? GetStoredCurrency(Payments? payment)
        {
            if (!string.IsNullOrWhiteSpace(payment?.Currency))
            {
                return payment.Currency;
            }

            return TryGetMetadataValue(payment, "currency", out var currency)
                ? NormalizeCurrency(currency)
                : null;
        }

        private static string GetStoredPaymentMode(Payments? payment)
        {
            return TryGetMetadataValue(payment, "paymentMode", out var paymentMode)
                ? NormalizePaymentMode(paymentMode)
                : LiveMode;
        }

        private static bool TryGetMetadataValue(Payments? payment, string key, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(payment?.PopDoc)
                || !payment.PopDoc.StartsWith(MetadataPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var metadata = payment.PopDoc.Substring(MetadataPrefix.Length)
                .Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in metadata)
            {
                var parts = item.Split('=', 2);
                if (parts.Length == 2 && string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
                {
                    value = parts[1];
                    return true;
                }
            }

            return false;
        }

        private static string BuildMetadata(PaynowCurrencyContext context)
        {
            return MetadataPrefix
                + "currency=" + context.Currency
                + ";paymentMode=" + context.PaymentMode
                + ";usdAmount=" + context.UsdAmount.ToString("0.00", CultureInfo.InvariantCulture)
                + ";exchangeRate=" + (context.ExchangeRate?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty)
                + ";integrationId=" + context.IntegrationId;
        }

        private static PaynowIntegrationCredentials GetCredentials(string currency, string paymentMode)
        {
            var normalizedCurrency = NormalizeCurrency(currency);
            var normalizedPaymentMode = NormalizePaymentMode(paymentMode);
            if (IntegrationCredentials.TryGetValue(BuildCredentialKey(normalizedCurrency, normalizedPaymentMode), out var credentials))
            {
                return credentials;
            }

            throw new InvalidOperationException($"Paynow {normalizedPaymentMode.ToLowerInvariant()} credentials for {normalizedCurrency} have not been configured.");
        }

        private static bool HasCredentials(string currency, string paymentMode)
        {
            return IntegrationCredentials.ContainsKey(BuildCredentialKey(currency, paymentMode));
        }

        private static string BuildCredentialKey(string currency, string paymentMode)
        {
            return NormalizeCurrency(currency) + ":" + NormalizePaymentMode(paymentMode);
        }

        private static string BuildCredentialLabel(string currency, string paymentMode)
        {
            return BuildCredentialKey(currency, paymentMode);
        }

        private static string ParseCurrencyLabel(string label)
        {
            return label.Split(':', 2)[0];
        }

        private static string ParseModeLabel(string label)
        {
            var parts = label.Split(':', 2);
            return parts.Length == 2 ? parts[1] : LiveMode;
        }
    }

    public record PaynowCurrencyContext(
        string Currency,
        decimal PaynowAmount,
        decimal UsdAmount,
        decimal? ExchangeRate,
        string IntegrationId,
        string PaymentMode);

    public record PaynowIntegrationCredentials(string IntegrationId, string IntegrationKey);
}
