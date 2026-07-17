namespace LLB.Models.ViewModel
{
    public class PaynowIntegrationSettingsViewModel
    {
        public List<PaynowIntegrationSettingItemViewModel> Integrations { get; set; } = new();
    }

    public class PaynowIntegrationSettingItemViewModel
    {
        public string Currency { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string IntegrationId { get; set; } = string.Empty;
        public string MaskedIntegrationKey { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
    }
}
