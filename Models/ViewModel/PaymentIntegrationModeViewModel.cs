namespace LLB.Models.ViewModel
{
    public class PaymentIntegrationModeViewModel
    {
        public string CurrentMode { get; set; } = "Live";
        public List<PaymentModeStatusViewModel> ModeStatus { get; set; } = new();
    }

    public class PaymentModeStatusViewModel
    {
        public string Currency { get; set; } = string.Empty;
        public bool IsTestModeConfigured { get; set; }
        public bool IsLiveModeConfigured { get; set; }
        public string CurrentMode { get; set; } = "Live";
    }
}
