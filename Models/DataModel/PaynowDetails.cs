namespace LLB.Models.DataModel
{
    public class PaynowDetails
    {
        public string Id { get; set; }
        public string ApplicationRef { get; set; }
        public string Payer { get; set; }
        public string PollUrl { get; set; }
        public string PaynowRef { get; set; }
        public string PaymentStatus { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransDate { get; set; }
        
        
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
