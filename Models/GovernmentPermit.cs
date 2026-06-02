using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLB.Models
{
    public class GovernmentPermit
    {
        [Key]
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? ApplicationId { get; set; }
        public string? Reference { get; set; }
        public string? Status { get; set; }
        public string? LG30 { get; set; }
        public string? TitleOfAuthority { get; set; }
        public string? Ministry { get; set; }
        public string? LocationName { get; set; }
        public string? Address { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Council { get; set; }
        public double? Payment { get; set; }
        public string? PaymentStatus { get; set; }

        [Column("Letter_from_the_superior")]
        public string? LetterFromTheSuperior { get; set; }

        public string? VerifierId { get; set; }
        public DateTime? DateVerified { get; set; }
        public string? RecommenderId { get; set; }
        public DateTime? DateRecommended { get; set; }
        public string? ApproverId { get; set; }
        public DateTime? DateOfApproval { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
