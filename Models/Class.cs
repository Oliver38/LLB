namespace LLB.Models
{
    public class Class
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string TransferInitiator { get; set;}
        public string TransferAuthoriser { get; set; }
        public string Status { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
