using LLB.Data;
using LLB.Models;

namespace LLB.Helpers
{
    public static class PostFormationQueryWorkflow
    {
        public const string StagePrefix = "postformation|";

        public static string BuildStage(Tasks task, string? previousStatus)
        {
            var stage = GetReviewStage(task);
            var encodedStatus = Uri.EscapeDataString(previousStatus?.Trim() ?? string.Empty);
            return $"{StagePrefix}{stage}|{encodedStatus}";
        }

        public static bool IsPostFormationQuery(Queries query)
        {
            return query.Stage?.StartsWith(StagePrefix, StringComparison.OrdinalIgnoreCase) == true;
        }

        public static string GetReviewStage(Tasks? task)
        {
            if (task == null)
            {
                return "verification";
            }

            if (!string.IsNullOrWhiteSpace(task.ExaminationStatus))
            {
                var normalized = task.ExaminationStatus.Trim().ToLowerInvariant();
                if (normalized.Contains("approv") || normalized.Contains("secretary"))
                {
                    return "approval";
                }

                if (normalized.Contains("recommend"))
                {
                    return "recommendation";
                }

                if (normalized.Contains("verif") || normalized.Contains("inspect"))
                {
                    return "verification";
                }
            }

            if (!string.IsNullOrWhiteSpace(task.ApproverId))
            {
                return "approval";
            }

            if (!string.IsNullOrWhiteSpace(task.RecommenderId))
            {
                return "recommendation";
            }

            return "verification";
        }

        public static string GetReviewStage(Queries query)
        {
            var parts = (query.Stage ?? string.Empty).Split('|');
            return parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
                ? parts[1].Trim().ToLowerInvariant()
                : "verification";
        }

        public static string GetPreviousStatus(Queries query, string service)
        {
            var parts = (query.Stage ?? string.Empty).Split('|');
            if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]))
            {
                return Uri.UnescapeDataString(parts[2]);
            }

            return GetDefaultStatus(service, GetReviewStage(query));
        }

        public static string GetReviewerLabel(Queries query)
        {
            return GetReviewStage(query) switch
            {
                "approval" => "Secretary examination",
                "recommendation" => "Recommendation examination",
                _ => "Verification examination"
            };
        }

        public static string GetDefaultStatus(string? service, string reviewStage)
        {
            if (string.Equals(service, TemporaryTransferHelper.ServiceName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(service, TemporaryRemovalHelper.ServiceName, StringComparison.OrdinalIgnoreCase))
            {
                return "awaiting approval";
            }

            return reviewStage switch
            {
                "approval" when string.Equals(service, "Manager Change", StringComparison.OrdinalIgnoreCase) => "submitted",
                "approval" => "recommended",
                "recommendation" => "verified",
                _ => "submitted"
            };
        }

        public static bool HasOpenQueries(AppDbContext db, string? recordId)
        {
            if (string.IsNullOrWhiteSpace(recordId))
            {
                return false;
            }

            return db.Queries.Any(query => query.ApplicationId == recordId
                && query.Status == ApplicationQueryWorkflow.OpenStatus
                && query.Stage != null
                && query.Stage.StartsWith(StagePrefix));
        }
    }
}
