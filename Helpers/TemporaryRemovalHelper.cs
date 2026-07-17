using LLB.Models;

namespace LLB.Helpers
{
    public static class TemporaryRemovalHelper
    {
        public const string ServiceName = "Temporary Removal";
        public const string DisplayServiceName = "Removal";
        public const string ServiceCode = "TRM";
        public const string PermanentRemovalType = "Permanent Removal";
        public const string TemporaryRemovalType = "Temporary Removal";

        public static readonly string[] RequiredDocumentTitles =
        {
            "Local Authority Letter of approval",
            "Proof of publication in the Government Gazette",
            "Lease/Deed documents",
            "A3 Plan approved by local Environmental Health",
            "An inspection report from the local healthy officer on LG25 approving the premises & inspector of premises"
        };

        public static bool IsTemporaryRemovalApplication(ApplicationInfo? application)
        {
            return string.Equals(application?.ExaminationStatus, ServiceName, StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeRemovalType(string? removalType)
        {
            if (string.IsNullOrWhiteSpace(removalType))
            {
                return string.Empty;
            }

            var normalized = removalType.Trim().ToLowerInvariant();
            return normalized switch
            {
                "permanent" => PermanentRemovalType,
                "permanent removal" => PermanentRemovalType,
                "temporary" => TemporaryRemovalType,
                "temporary removal" => TemporaryRemovalType,
                _ => string.Empty
            };
        }

        public static string GetRemovalType(ApplicationInfo? application)
        {
            var removalType = NormalizeRemovalType(application?.PlaceOfBirth);
            return string.IsNullOrWhiteSpace(removalType)
                ? TemporaryRemovalType
                : removalType;
        }

        public static string GetRemovalTypeDisplayName(string? removalType)
        {
            var normalized = NormalizeRemovalType(removalType);
            return string.IsNullOrWhiteSpace(normalized)
                ? TemporaryRemovalType
                : normalized;
        }

        public static bool IsFinalStatus(string? status)
        {
            return string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesService(string? service)
        {
            return string.Equals(service, ServiceName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
