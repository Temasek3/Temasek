using System;
using FreeSql.DataAnnotations;

namespace Temasek.WebApi.Entities;

[Index("idx_user_nric", nameof(Nric), true)]
public class User
{
    [Column(IsIdentity = true, IsPrimary = true)]
    public long UserId { get; set; }
    public string Nric { get; set; }
    public string Name { get; set; }
    public string Unit { get; set; }
}
