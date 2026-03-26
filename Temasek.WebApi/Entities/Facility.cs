using FreeSql.DataAnnotations;

namespace Temasek.WebApi.Entities;

public class Facility
{
    [Column(IsIdentity = true, IsPrimary = true)]
    public long FacilityId { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
}
