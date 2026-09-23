namespace LLB.Models.ViewModel
{
    public sealed class ApplicationQueriesViewModel
    {
        public string ApplicationId { get; init; } = string.Empty;
        public string Reference { get; init; } = string.Empty;
        public string TradingName { get; init; } = string.Empty;
        public string ApplicationStatus { get; init; } = string.Empty;
        public string ExaminationStatus { get; init; } = string.Empty;
        public IReadOnlyList<ApplicationQueryStageViewModel> Stages { get; init; } = Array.Empty<ApplicationQueryStageViewModel>();

        public int TotalQueries => Stages.Sum(stage => stage.Queries.Count);
        public int OpenQueries => Stages.Sum(stage => stage.OpenQueries);
        public int ResolvedQueries => TotalQueries - OpenQueries;
        public bool IsVerificationInspectionQuery => string.Equals(
            ExaminationStatus,
            "Verification Inspection Query",
            StringComparison.OrdinalIgnoreCase);
        public bool CanResubmit => TotalQueries > 0
            && OpenQueries == 0
            && string.Equals(ApplicationStatus, "Has Query", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class ApplicationQueryStageViewModel
    {
        public string Key { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Icon { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public IReadOnlyList<ApplicationQueryItemViewModel> Queries { get; init; } = Array.Empty<ApplicationQueryItemViewModel>();

        public int OpenQueries => Queries.Count(query => !query.IsResolved);
        public bool IsResolved => Queries.Count > 0 && OpenQueries == 0;
    }

    public sealed class ApplicationQueryItemViewModel
    {
        public string Id { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string ReviewerStage { get; init; } = string.Empty;
        public DateTime DateRaised { get; init; }
        public DateTime? DateResolved { get; init; }
        public bool IsResolved { get; init; }
    }

    public sealed class ExaminerQueryPanelViewModel
    {
        public string ControllerName { get; init; } = string.Empty;
        public string ApplicationId { get; init; } = string.Empty;
        public string TaskId { get; init; } = string.Empty;
        public string StageKey { get; init; } = string.Empty;
        public string Heading { get; init; } = string.Empty;
        public string SupportingText { get; init; } = string.Empty;
    }

    public sealed class ApplicantQueryStagePanelViewModel
    {
        public string ApplicationId { get; init; } = string.Empty;
        public string StageKey { get; init; } = string.Empty;
        public string StageTitle { get; init; } = string.Empty;
        public IReadOnlyList<ApplicationQueryItemViewModel> Queries { get; init; } = Array.Empty<ApplicationQueryItemViewModel>();
    }

    public sealed class VerificationInspectionQueryViewModel
    {
        public string ApplicationId { get; init; } = string.Empty;
        public string Reference { get; init; } = string.Empty;
        public string TradingName { get; init; } = string.Empty;
        public string InspectionReference { get; init; } = string.Empty;
        public DateTime InspectionDate { get; init; }
        public string InspectorComments { get; init; } = string.Empty;
        public IReadOnlyList<string> FailedItems { get; init; } = Array.Empty<string>();
        public IReadOnlyList<ApplicationQueryItemViewModel> Queries { get; init; } = Array.Empty<ApplicationQueryItemViewModel>();
    }
}
