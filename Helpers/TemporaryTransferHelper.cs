using LLB.Models;

namespace LLB.Helpers
{
    public static class TemporaryTransferHelper
    {
        public const string ServiceName = "Temporary Transfer";
        public const string DisplayServiceName = "Transfer";
        public const string ServiceCode = "TTR";
        public const string ProofOfPublicationDocumentTitle = "Proof of publication in the government gazette and local paper.{L.G 2}";
        public const string TieAffidavitDocumentTitle = "Tie Affidavit";
        public const string TransferAffidavitDocumentTitle = "Transfer affidavit";
        public const string LeaseDocumentsDocumentTitle = "Lease documents, title deeds or any evidence of right of occupation.";
        public const string PermanentTransferType = "Permanent Transfer";
        public const string TemporaryTransferType = "Temporary Transfer";

        public static bool IsTemporaryTransferApplication(ApplicationInfo? application)
        {
            return string.Equals(application?.ExaminationStatus, ServiceName, StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeTransferType(string? transferType)
        {
            if (string.IsNullOrWhiteSpace(transferType))
            {
                return string.Empty;
            }

            var normalized = transferType.Trim().ToLowerInvariant();
            return normalized switch
            {
                "permanent" => PermanentTransferType,
                "permanent transfer" => PermanentTransferType,
                "temporary" => TemporaryTransferType,
                "temporary transfer" => TemporaryTransferType,
                _ => string.Empty
            };
        }

        public static string GetTransferType(ApplicationInfo? application)
        {
            var transferType = NormalizeTransferType(application?.PlaceOfBirth);
            return string.IsNullOrWhiteSpace(transferType)
                ? TemporaryTransferType
                : transferType;
        }

        public static string GetTransferTypeDisplayName(string? transferType)
        {
            var normalized = NormalizeTransferType(transferType);
            return string.IsNullOrWhiteSpace(normalized)
                ? TemporaryTransferType
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
