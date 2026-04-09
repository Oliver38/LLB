using System;
using System.Collections.Generic;

namespace LLB.Models.ViewModel
{
    public class AccountantExchangeRateViewModel
    {
        public string SourceName { get; set; } = "Reserve Bank of Zimbabwe";
        public string SourceUrl { get; set; } = "https://www.rbz.co.zw/index.php";
        public string Market { get; set; } = "Interbank Rates";
        public string CurrencyPair { get; set; } = "USD/ZWG";
        public DateTime? ExchangeDate { get; set; }
        public decimal? Bid { get; set; }
        public decimal? Ask { get; set; }
        public decimal? Average { get; set; }
        public DateTime RetrievedAt { get; set; } = DateTime.Now;
        public string? ErrorMessage { get; set; }
        public AccountantExchangeRateHistoryItemViewModel? TodayStoredRate { get; set; }
        public List<AccountantExchangeRateHistoryItemViewModel> StoredRates { get; set; } = new();

        public bool HasRate =>
            ExchangeDate.HasValue
            && Bid.HasValue
            && Ask.HasValue
            && Average.HasValue;

        public bool HasStoredRateForToday => TodayStoredRate != null;
    }

    public class AccountantExchangeRateHistoryItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string UpdaterUserName { get; set; } = string.Empty;
        public decimal? ExchangeRate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
