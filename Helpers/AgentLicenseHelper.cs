using LLB.Models;

namespace LLB.Helpers
{
    public static class AgentLicenseHelper
    {
        public const string ServiceName = "Agent License";
        public const string ServiceCode = "AGL";
        public const string PublicationDocumentTitle = "Advert and Proof of publication in the government gazette and local paper.{L.G 2}";
        public const string TieAffidavitDocumentTitle = "Tie Affidavit";
        public const string WholesaleLicenseDocumentTitle = "Wholesale license";

        public static bool IsAgentLicenseApplication(ApplicationInfo? application)
        {
            return string.Equals(application?.ExaminationStatus, ServiceName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsWholesaleLicense(LicenseTypes? license)
        {
            return ContainsWholesale(license?.LicenseName)
                || ContainsWholesale(license?.LicenseCode)
                || ContainsWholesale(license?.Description);
        }

        public static string DisplayValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "N/A" : value;
        }

        private static bool ContainsWholesale(string? value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Contains("wholesale", StringComparison.OrdinalIgnoreCase);
        }
    }
}
