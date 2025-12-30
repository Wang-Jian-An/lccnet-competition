using System;
using System.Collections.Generic;

namespace lccnet_competition.Models;

public partial class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Sha256 { get; set; } = null!;

    public string EnvUrl { get; set; } = null!;

    public DateTime? CreateDatetime { get; set; }

    public DateTime? UpdateDatetime { get; set; }

    public string Role { get; set; } = null!;
}
