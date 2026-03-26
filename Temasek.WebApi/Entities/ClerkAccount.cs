using FreeSql.DataAnnotations;

namespace Temasek.WebApi.Entities;

[Index("idx_clerkid", nameof(ClerkId), true)]
public class ClerkAccount
{
    [Column(IsIdentity = true, IsPrimary = true)]
    public long ClerkAccountId { get; set; }
    public string ClerkId { get; set; }

    public long UserId { get; set; }

    [Navigate(nameof(UserId))]
    public User User { get; set; }
}
