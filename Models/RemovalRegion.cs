

using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class RemovalRegion
    {
        [Key]
        public string? Id { get; set; }

       public string? RemovalName { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? UserId { get; set; }
        public DateTime DateAdded { get; set; }
        


    }
}
