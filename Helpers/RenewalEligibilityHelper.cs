using System;

namespace LLB.Helpers
{
    public class RenewalEligibilityResult
    {
        public bool IsEligible { get; init; }
        public DateTime? EligibleFrom { get; init; }
        public DateTime? ExpiryDate { get; init; }
        public string WarningMessage { get; init; } = string.Empty;
    }

    public static class RenewalEligibilityHelper
    {
        public static RenewalEligibilityResult Evaluate(DateTime? expiryDate, DateTime currentDate)
        {
            if (!expiryDate.HasValue || expiryDate.Value == DateTime.MinValue)
            {
                return new RenewalEligibilityResult
                {
                    IsEligible = false,
                    WarningMessage = "Renewal is unavailable because the licence expiry date is missing from application information."
                };
            }

            var normalizedExpiryDate = expiryDate.Value.Date;
            var eligibleFrom = normalizedExpiryDate.AddMonths(-1);
            var isEligible = currentDate.Date >= eligibleFrom;

            return new RenewalEligibilityResult
            {
                IsEligible = isEligible,
                EligibleFrom = eligibleFrom,
                ExpiryDate = normalizedExpiryDate,
                WarningMessage = isEligible
                    ? string.Empty
                    : $"Renewal can only start within one month of expiry. This licence expires on {normalizedExpiryDate:dd MMMM yyyy}, so renewal opens on {eligibleFrom:dd MMMM yyyy}."
            };
        }
    }
}
