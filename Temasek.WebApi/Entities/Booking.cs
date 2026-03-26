using FreeSql.DataAnnotations;

namespace Temasek.WebApi.Entities;

public class Booking
{
    [Column(IsIdentity = true, IsPrimary = true)]
    public long BookingId { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string AvailabilityStmt { get; set; } = "true";

    public long FacilityId { get; set; }

    [Navigate(nameof(FacilityId))]
    public Facility Facility { get; set; }

    public long CreatedByUserId { get; set; }

    [Navigate(nameof(CreatedByUserId))]
    public User CreatedBy { get; set; }
}
