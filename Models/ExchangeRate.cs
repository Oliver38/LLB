using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace LLB.Models
{
    public class ExchangeRate
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? Status  { get; set; }
        public double? ZWGrate { get; set; }
        
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }



    }

    public class Rate
    {
        public string currency { get; set; }
        public long last_checked { get; set; }
        public long last_updated { get; set; }
        public string name { get; set; }
        public double rate { get; set; }
        public string url { get; set; }
        // Readable DateTime properties
        public DateTime LastCheckedDateTime => DateTimeOffset.FromUnixTimeSeconds(last_checked).DateTime;
        public DateTime LastUpdatedDateTime => DateTimeOffset.FromUnixTimeSeconds(last_updated).DateTime;
    }

    public class RatesResponse
    {
        public List<Rate> USD { get; set; }
        public string info { get; set; }
    }


}
