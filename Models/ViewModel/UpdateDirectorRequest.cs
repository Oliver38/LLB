namespace LLB.Models.ViewModel;

public sealed class UpdateDirectorRequest
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? NationalId { get; set; }
    public string? Address { get; set; }
    public IFormFile? NatIdCopy { get; set; }
    public IFormFile? Fingerprints { get; set; }
    public IFormFile? Form55 { get; set; }
}
