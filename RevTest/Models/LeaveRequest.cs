using System;
using System.Collections.Generic;

namespace RevTest.Models;

public partial class LeaveRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string LeaveType { get; set; }
    public string Reason { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int NumberOfDays { get; set; }
    public bool IsPaid { get; set; }
    public string Status { get; set; }
    public string AdminRemarks { get; set; }
    public DateTime AppliedDate { get; set; }

    public virtual User User { get; set; }
}
