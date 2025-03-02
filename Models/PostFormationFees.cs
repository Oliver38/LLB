

using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class PostFormationFees
    {
        [Key]
        public string Id { get; set; }

       public string  ProcessName { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public string Status { get; set; }
        public string UserId { get; set; }
        public double Fee { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
        


    }
}
