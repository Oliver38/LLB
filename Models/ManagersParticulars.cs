
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;

namespace LLB.Models
{
    public class ManagersParticulars
    {
        [Key]
        public string Id { get; set; }

       public string  ApplicationID { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Address { get; set; }
        public DateTime DateAdded { get; set; }
        public string status { get; set; }


    }
}
