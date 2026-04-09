using LLB.Models;

namespace LLB.Helpers
{
    public static class TemporaryTransferHelper
    {
        public const string ServiceName = "Temporary Transfer";
        public const string ServiceCode = "TTR";
        public const string TieAffidavitDocumentTitle = "Tie Affidavit";

        public static bool IsTemporaryTransferApplication(ApplicationInfo? application)
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
