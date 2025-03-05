using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class TransferwmanagerTypes
    {
        [Key]
        public string? Id { get; set; }
        public string? TransferwmanagerName { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? UserId { get; set; }
        public string? LicenseCode { get; set; }
        public double CityFee { get; set; }
        public double MunicipaltyFee { get; set; }
        public double TownFee { get; set; }
        public double RDCFee { get; set; }

        public string TransferwmanagerConditionList { get; set; }
        public string TransferwmanagerInstructions { get; set; }


        //public double FeeId { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
        
    }
}
