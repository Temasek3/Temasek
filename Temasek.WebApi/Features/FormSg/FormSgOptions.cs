namespace Temasek.WebApi.Features.FormSg;

public class FormSgOptions
{
    public string? CallbackApiKey { get; set; }

    /// <summary>
    /// Secret key to for JWT sent to the prefilled form field
    /// </summary>
    public string? SecretKey { get; set; }
    public string? FormId { get; set; }
    public string? PrefillFieldId { get; set; }
}
