using LLB.Data;
using LLB.Models;
using System.Globalization;
using Webdev.Payments;

namespace LLB.Helpers
{
    public static class PaynowCurrencyHelper
    {
        public const string UsdCurrency = "USD";
        public const string ZwgCurrency = "ZWG";
        public const string ReturnBaseUrl = "https://llb.pfms.gov.zw";

        private const string LegacyIntegrationId = "7175";
        private const string LegacyIntegrationKey = "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0";
        private const string UsdIntegrationId = "24194";
        private const string UsdIntegrationKey = "7ba282f6-f810-49e1-add3-9bca84019424";
        private const string ZwgIntegrationId = "24195";
        private const string ZwgIntegrationKey = "7c72bdc4-f068-48f2-acf4-cfb60b4e6bf3";
        private const string MetadataPrefix = "PAYNOW_CURRENCY|";

        public static PaynowCurrencyContext BuildPaymentContext(AppDbContext db, decimal usdAmount, string? currency)
        {
            var normalizedCurrency = NormalizeCurrency(currency);
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
                    ZwgIntegrationId);
            }

            return new PaynowCurrencyContext(
                UsdCurrency,
                Math.Round(usdAmount, 2, MidpointRounding.AwayFromZero),
                usdAmount,
                null,
                UsdIntegrationId);
        }

        public static Paynow CreatePaynow(PaynowCurrencyContext context)
        {
            return CreatePaynow(context.Currency);
        }

        public static Paynow CreatePaynow(Payments? payment)
        {
            var currency = GetStoredCurrency(payment);
            return string.IsNullOrWhiteSpace(currency)
                ? CreateLegacyPaynow()
                : CreatePaynow(currency);
        }

        public static Paynow CreatePaynow(string? currency)
        {
            return NormalizeCurrency(currency) == ZwgCurrency
                ? new Paynow(ZwgIntegrationId, ZwgIntegrationKey)
                : new Paynow(UsdIntegrationId, UsdIntegrationKey);
        }

        public static string NormalizeCurrency(string? currency)
        {
            return string.Equals(currency, ZwgCurrency, StringComparison.OrdinalIgnoreCase)
                ? ZwgCurrency
                : UsdCurrency;
        }

        public static string BuildReturnUrl(string pathAndQuery)
        {
            var path = string.IsNullOrWhiteSpace(pathAndQuery) ? "/" : pathAndQuery;
            if (path.StartsWith(ReturnBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            if (!path.StartsWith("/", StringComparison.Ordinal))
            {
                path = "/" + path;
            }

            return ReturnBaseUrl.TrimEnd('/') + path;
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

            if (string.IsNullOrWhiteSpace(storedCurrency))
            {
                labels.Add("LEGACY");
            }
            else
            {
                labels.Add(storedCurrency);
            }

            labels.Add(UsdCurrency);
            labels.Add(ZwgCurrency);
            labels.Add("LEGACY");

            foreach (var label in labels.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                yield return string.Equals(label, "LEGACY", StringComparison.OrdinalIgnoreCase)
                    ? CreateLegacyPaynow()
                    : CreatePaynow(label);
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

            if (string.IsNullOrWhiteSpace(payment?.PopDoc)
                || !payment.PopDoc.StartsWith(MetadataPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var metadata = payment.PopDoc.Substring(MetadataPrefix.Length)
                .Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in metadata)
            {
                var parts = item.Split('=', 2);
                if (parts.Length == 2 && string.Equals(parts[0], "currency", StringComparison.OrdinalIgnoreCase))
                {
                    return NormalizeCurrency(parts[1]);
                }
            }

            return null;
        }

        private static string BuildMetadata(PaynowCurrencyContext context)
        {
            return MetadataPrefix
                + "currency=" + context.Currency
                + ";usdAmount=" + context.UsdAmount.ToString("0.00", CultureInfo.InvariantCulture)
                + ";exchangeRate=" + (context.ExchangeRate?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty)
                + ";integrationId=" + context.IntegrationId;
        }
    }

    public record PaynowCurrencyContext(
        string Currency,
        decimal PaynowAmount,
        decimal UsdAmount,
        decimal? ExchangeRate,
        string IntegrationId);
}
