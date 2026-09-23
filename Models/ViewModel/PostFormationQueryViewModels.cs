namespace LLB.Models.ViewModel
{
    public sealed class PostFormationExaminerQueryPanelViewModel
    {
        public string RecordId { get; init; } = string.Empty;
        public string TaskId { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
    }

    public sealed class PostFormationQueriesViewModel
    {
        public string RecordId { get; init; } = string.Empty;
        public string Reference { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public string TradingName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public bool IsCorrectionMode { get; init; }
        public IReadOnlyList<ApplicationQueryItemViewModel> Queries { get; init; } = Array.Empty<ApplicationQueryItemViewModel>();

        public int OpenQueries => Queries.Count(query => !query.IsResolved);
        public bool CanResolve => OpenQueries > 0 && IsCorrectionMode;
        public bool CanResubmit => Queries.Count > 0 && OpenQueries == 0 && IsCorrectionMode;
    }
}
