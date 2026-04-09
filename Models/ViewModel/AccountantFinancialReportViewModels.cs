namespace LLB.Models.ViewModel
{
    public class AccountantFinancialReportsViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? ServiceFilter { get; set; }
        public string? ChannelFilter { get; set; }
        public string? StatusFilter { get; set; }
        public List<string> ServiceOptions { get; set; } = new();
        public List<string> ChannelOptions { get; set; } = new();
        public List<string> StatusOptions { get; set; } = new();
        public int TotalTransactions { get; set; }
        public int FilteredTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal FilteredAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal FailedAmount { get; set; }
        public List<AccountantFinancialSummaryItemViewModel> ServiceBreakdown { get; set; } = new();
        public List<AccountantFinancialSummaryItemViewModel> ChannelBreakdown { get; set; } = new();
        public List<AccountantFinancialSummaryItemViewModel> StatusBreakdown { get; set; } = new();
        public List<AccountantFinancialReportRowViewModel> Transactions { get; set; } = new();
    }

    public class AccountantFinancialSummaryItemViewModel
    {
        public string Name { get; set; } = "Unspecified";
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class AccountantFinancialReportRowViewModel
    {
        public string PaymentId { get; set; } = string.Empty;
        public string Reference { get; set; } = "N/A";
        public string ApplicationReference { get; set; } = "N/A";
        public string TradingName { get; set; } = "N/A";
        public string Payer { get; set; } = "N/A";
        public string Service { get; set; } = "Unspecified";
        public string Channel { get; set; } = "Unspecified";
        public string Status { get; set; } = "Unknown";
        public string PaynowReference { get; set; } = "N/A";
        public string SystemReference { get; set; } = "N/A";
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
