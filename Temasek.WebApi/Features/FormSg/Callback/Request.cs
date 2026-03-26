using System.ComponentModel.DataAnnotations;

namespace Temasek.WebApi.Features.FormSg.Callback;

public class Request
{
    [Required]
    public required string ClerkUserId { get; init; }

    [Required]
    public required string Nric { get; init; }

    [Required]
    public required string Name { get; init; }
}
