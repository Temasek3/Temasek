using System;

namespace Temasek.WebApi.Features.FormSg.Validate;

public class Response
{
    public required string FormId { get; init; }
    public required string PrefillFieldId { get; init; }
    public required string ClerkUserId { get; init; }
}
