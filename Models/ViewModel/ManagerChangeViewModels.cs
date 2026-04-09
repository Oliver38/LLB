using System;
using System.Collections.Generic;
using LLB.Models;

namespace LLB.Models.ViewModel
{
    public class ManagerChangePageViewModel
    {
        public string Process { get; set; } = "APM";
        public ApplicationInfo? Application { get; set; }
        public OutletInfo? Outlet { get; set; }
        public LicenseTypes? License { get; set; }
        public LicenseRegion? Region { get; set; }
        public ChangeManaager? ChangeApplication { get; set; }
        public Payments? Payment { get; set; }
        public string InitiatedByUserName { get; set; } = "N/A";
        public double FeePerManager { get; set; }
        public int NewManagersCount { get; set; }
        public double TotalFee { get; set; }
        public bool HasChanges { get; set; }
        public bool CanEditDraft { get; set; }
        public bool CanSubmitDraft { get; set; }
        public bool CanMakePayment { get; set; }
        public bool CanContinue { get; set; }
        public bool IsPaid { get; set; }
        public bool PaymentRequired => TotalFee > 0;
        public List<ManagerChangeManagerItemViewModel> Managers { get; set; } = new();
        public List<ManagerChangeManagerItemViewModel> Changes { get; set; } = new();
    }

    public class ManagerChangeManagerItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DisplayStatus { get; set; } = string.Empty;
        public string Attachment { get; set; } = string.Empty;
        public string Fingerprints { get; set; } = string.Empty;
        public string Form55 { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public DateTime DissmisalDate { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
        public bool IsDraftAddition { get; set; }
        public bool CanEditDetails { get; set; }
        public bool CanEditStatus { get; set; }
        public bool CanDelete { get; set; }
    }

    public class ManagerChangeReviewViewModel
    {
        public ChangeManaager? ChangeApplication { get; set; }
        public ApplicationInfo? Application { get; set; }
        public OutletInfo? Outlet { get; set; }
        public LicenseTypes? License { get; set; }
        public LicenseRegion? Region { get; set; }
        public Payments? Payment { get; set; }
        public string InitiatedByUserName { get; set; } = "N/A";
        public int NewManagersCount { get; set; }
        public double TotalFee { get; set; }
        public List<ManagerChangeManagerItemViewModel> Changes { get; set; } = new();
    }
}
