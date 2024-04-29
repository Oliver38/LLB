namespace LLB.Models.DataModel
{
    public class Branches
    {
        public int Id { get; set; }
        public string BranchName { get; set; }
        public string Location { get; set; }
        public int CreatorId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
