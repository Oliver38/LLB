using LLB.Models;

namespace LLB.Helpers
{
    public static class TemporaryRemovalHelper
    {
        public const string ServiceName = "Temporary Removal";
        public const string ServiceCode = "TRM";

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
