namespace Temasek.WebApi.Features.Pokedex.List;

public class Response
{
    public List<ResponseUser> Users { get; set; }
}

public class ResponseUser
{
    public Guid UserId { get; set; }
    public string Nric { get; set; }
    public string Name { get; set; }
    public string Unit { get; set; }
}
