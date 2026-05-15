using System.ComponentModel.DataAnnotations;

namespace NoNameLogger.Demo.Samples.Models.cs;

/// <summary>
/// Request model used in the demo to showcase validation + structured context.
/// </summary>
public class SampleRequest
{
    [Required]
    public string? PropertyId { get; set; }

    [Required]
    public string? ProviderCode { get; set; }

    public int? PageNumber { get; set; }
}