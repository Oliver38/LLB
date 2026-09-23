using LLB.Models;

namespace LLB.Helpers
{
    public static class ApplicationQueryWorkflow
    {
        public const string OpenStatus = "Has Query";
        public const string ResolvedStatus = "Resolved";

        public const string ApplicantStage = "applicant";
        public const string OutletStage = "outlet";
        public const string ManagersStage = "managers";
        public const string AttachmentsStage = "attachments";
        public const string InspectionStage = "inspection";

        public static readonly string[] StageOrder =
        {
            ApplicantStage,
            OutletStage,
            ManagersStage,
            AttachmentsStage,
            InspectionStage
        };

        public static string BuildStageValue(string reviewerStage, string stageKey)
        {
            return $"{reviewerStage.Trim().ToLowerInvariant()}:{NormalizeStageKey(stageKey)}";
        }

        public static string NormalizeStageKey(string? stage)
        {
            var value = stage?.Trim().ToLowerInvariant() ?? string.Empty;

            if (value.Contains("attachment"))
            {
                return AttachmentsStage;
            }

            if (value.Contains("inspection"))
            {
                return InspectionStage;
            }

            if (value.Contains("manager"))
            {
                return ManagersStage;
            }

            if (value.Contains("outlet"))
            {
                return OutletStage;
            }

            if (value.Contains("applicant") || value.Contains("application"))
            {
                return ApplicantStage;
            }

            return string.Empty;
        }

        public static bool IsKnownStage(string? stage)
        {
            return StageOrder.Contains(NormalizeStageKey(stage));
        }

        public static string GetStageTitle(string stageKey)
        {
            return NormalizeStageKey(stageKey) switch
            {
                OutletStage => "Outlet Information",
                ManagersStage => "Managers Information",
                AttachmentsStage => "Attachments Information",
                InspectionStage => "Verification Inspection",
                _ => "Applicant Information"
            };
        }

        public static string GetStageDescription(string stageKey)
        {
            return NormalizeStageKey(stageKey) switch
            {
                OutletStage => "Trading premises, licence category, location and directors",
                ManagersStage => "Manager particulars and supporting identity documents",
                AttachmentsStage => "Required application documents and uploaded evidence",
                InspectionStage => "Premises corrections required after the verification inspection",
                _ => "Applicant identity, licence holder details and personal documents"
            };
        }

        public static string GetStageIcon(string stageKey)
        {
            return NormalizeStageKey(stageKey) switch
            {
                OutletStage => "bi-shop",
                ManagersStage => "bi-people",
                AttachmentsStage => "bi-paperclip",
                InspectionStage => "bi-clipboard2-pulse",
                _ => "bi-person-vcard"
            };
        }

        public static string GetStageAction(string stageKey)
        {
            return NormalizeStageKey(stageKey) switch
            {
                OutletStage => "OutletInfo",
                ManagersStage => "ManagersInfo",
                AttachmentsStage => "Attachments",
                InspectionStage => "InspectionQuery",
                _ => "Apply"
            };
        }

        public static string GetReviewerLabel(Queries query)
        {
            var stage = query.Stage?.Trim().ToLowerInvariant() ?? string.Empty;

            if (stage.StartsWith("verification-inspection:") || stage.StartsWith("inspection:"))
            {
                return "Verification inspection";
            }

            if (stage.StartsWith("recommendation:") || stage.StartsWith("recommend"))
            {
                return "Recommendation review";
            }

            if (stage.StartsWith("secretary:") || stage.StartsWith("approve"))
            {
                return "Secretary review";
            }

            return "Verification review";
        }

        public static bool IsOpen(Queries query)
        {
            return string.Equals(query.Status, OpenStatus, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsResolved(Queries query)
        {
            return string.Equals(query.Status, ResolvedStatus, StringComparison.OrdinalIgnoreCase);
        }
    }
}
