namespace LLB.Models.DataModel
{
    public class TaskDetails
    {
        public string Id { get; set; }
        public string ExaminerName{ get; set; }
        public string RefNumber { get; set; }
        public string BarName { get; set; }
        public DateTime DateSubmitted { get; set; }
        public string TaskStatus { get; set; }
        public string Jobtatus { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
