using System;
using System.Collections.Generic;

namespace lccnet_competition.Models;

public partial class EnvReversation
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public DateTime BookStartDatetime { get; set; }

    public DateTime BookEndDatetime { get; set; }

    public DateTime? CreateDatetime { get; set; }

    public DateTime? UpdateDatetime { get; set; }
}
