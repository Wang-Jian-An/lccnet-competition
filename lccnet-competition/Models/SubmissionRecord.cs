using System;
using System.Collections.Generic;

namespace lccnet_competition.Models;

public class ClassSubmissionFormat
{
    public string Id { get; set; } = null!;
    public string Answer { get; set; } = null!;
}

public partial class SubmissionRecord
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public string? FileName { get; set; }

    public sbyte IsSuccess { get; set; }

    public decimal? Score { get; set; }

    public DateTime? CreateDatetime { get; set; }

    public DateTime? UpdateDatetime { get; set; }

    public string Task { get; set; } = null!;
}
