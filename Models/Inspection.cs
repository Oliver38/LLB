


using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class Inspection
    {
        [Key]
        public string? Id { get; set; }

       public string? Service { get; set; }
        public string? Application { get; set; }
        public string? Status { get; set; }
        public string? UserId { get; set; }
        public string? ApplicationId { get; set; }
        public string? InspectorId { get; set; }
        public string? renewalId { get; set; }
        public DateTime? InspectionSchedule { get; set; }
        public DateTime InspectionDate { get; set; }
        public DateTime DateApplied { get; set; }
        public DateTime DateUpdate { get; set; }

        public string? Ventilation { get; set; }

        public string? Lighting { get; set; }
        public string? SewageDisposalAndDrainage { get; set; }
        public string? Toilets { get; set; }
        public string? WaterSupply { get; set; }
        public string? RubbishDisposal { get; set; }
        public string? StandardOfFood { get; set; }
        public string? FoodStorageArrangements { get; set; }
        public string? StaffUniformsAndAccommodation { get; set; }
        public string? EquipmentAndAppointments { get; set; }
        public string? HygieneStandards { get; set; }
        public string? Comments { get; set; }
        public string? Overall { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime PaymentDate { get; set; }




    }
}
