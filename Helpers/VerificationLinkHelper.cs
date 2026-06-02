namespace LLB.Helpers
{
    public static class VerificationLinkHelper
    {
        private const string LiveBaseUrl = "https://llb.pfms.gov.zw/";

        public static string BuildLiveUrl(string relativePath)
        {
            return $"{LiveBaseUrl}{relativePath.TrimStart('/')}";
        }
    }
}
