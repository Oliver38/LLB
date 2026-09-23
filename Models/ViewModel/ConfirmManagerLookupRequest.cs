namespace LLB.Models.ViewModel;

public sealed class ConfirmManagerLookupRequest
{
    public string? ApplicationId { get; set; }
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }
    public string? NationalId { get; set; }
    public IFormFile? NatIdCopy { get; set; }
    public IFormFile? Fingerprints { get; set; }
    public IFormFile? Form55 { get; set; }
}
