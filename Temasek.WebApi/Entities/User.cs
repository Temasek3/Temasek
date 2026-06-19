using System;
using FreeSql.DataAnnotations;

namespace Temasek.WebApi.Entities;

[Index("idx_user_nric", nameof(Nric), true)]
public class User
{
    [Column(IsIdentity = true, IsPrimary = true)]
    public long UserId { get; set; }
    public UserNric Nric { get; set; } = UserNric.Empty;
    public UserName Name { get; set; } = UserName.Empty;
    public UserUnit Unit { get; set; } = UserUnit.Empty;
}
