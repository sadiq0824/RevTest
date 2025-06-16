using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevTest.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RevTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveController : ControllerBase
    {
        private readonly FinalTestContext _db;

        public LeaveController(FinalTestContext db)
        {
            _db = db;
        }

        

        // POST: Apply Leave
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyLeave([FromBody] LeaveRequest leave)
        {
            var user = await _db.Users.FindAsync(leave.UserId);
            if (user == null || user.IsAdmin)
                return Unauthorized("Only employees can apply.");

            if (leave.FromDate < DateTime.Today)
                return BadRequest("Cannot apply for past dates.");

            if (leave.ToDate < leave.FromDate)
                return BadRequest("End date must be after start date.");

            // Calculate working days excluding weekends
            int totalDays = Enumerable.Range(0, (leave.ToDate - leave.FromDate).Days + 1)
                .Select(offset => leave.FromDate.AddDays(offset))
                .Count(date => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday);

            // Paid leave quota calculation
            int usedPaidLeaves = await _db.LeaveRequests
                .Where(l => l.UserId == leave.UserId && l.IsPaid && l.Status == "Approved")
                .SumAsync(l => (int?)l.NumberOfDays) ?? 0;

            int remainingQuota = 12 - usedPaidLeaves;

            leave.NumberOfDays = totalDays;
            leave.Status = "Pending";
            leave.AppliedDate = DateTime.Today;
            leave.IsPaid = totalDays <= remainingQuota;

            _db.LeaveRequests.Add(leave);
            await _db.SaveChangesAsync();

            return Ok("Leave applied successfully.");
        }

        // GET: View own leaves
        [HttpGet("my-leaves/{userId}")]
        public async Task<IActionResult> GetMyLeaves(int userId)
        {
            var leaves = await _db.LeaveRequests
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.AppliedDate)
                .ToListAsync();

            return Ok(leaves);
        }

        // DELETE: Cancel leave (only if not started)
        [HttpDelete("cancel/{id}")]
        public async Task<IActionResult> CancelLeave(int id)
        {
            var leave = await _db.LeaveRequests.FindAsync(id);
            if (leave == null)
                return NotFound("Leave not found.");

            if (DateTime.Today >= leave.FromDate)
                return BadRequest("Cannot cancel leave that already started.");

            leave.Status = "Cancelled";
            await _db.SaveChangesAsync();

            return Ok("Leave cancelled.");
        }

        // GET: Leave summary (Paid/Unpaid/Remaining)
        [HttpGet("summary/{userId}")]
        public async Task<IActionResult> GetLeaveSummary(int userId)
        {
            var approvedLeaves = await _db.LeaveRequests
                .Where(l => l.UserId == userId && l.Status == "Approved")
                .ToListAsync();

            int paidUsed = approvedLeaves.Where(l => l.IsPaid).Sum(l => l.NumberOfDays);
            int unpaidUsed = approvedLeaves.Where(l => !l.IsPaid).Sum(l => l.NumberOfDays);
            int remaining = 12 - paidUsed;

            return Ok(new
            {
                PaidUsed = paidUsed,
                Unpaid = unpaidUsed,
                Remaining = remaining
            });
        }

        

        // GET: All leave requests
        [HttpGet("all")]
        public async Task<IActionResult> GetAllLeaves()
        {
            var leaves = await _db.LeaveRequests
                .Include(l => l.User)
                .OrderByDescending(l => l.AppliedDate)
                .Select(l => new
                {
                    l.Id,
                    l.UserId,
                    l.User.Username,
                    l.LeaveType,
                    l.Reason,
                    l.FromDate,
                    l.ToDate,
                    l.NumberOfDays,
                    l.IsPaid,
                    l.Status,
                    l.AdminRemarks,
                    l.AppliedDate
                })
                .ToListAsync();

            return Ok(leaves);
        }

        // PUT: Approve or Reject leave
        [HttpPut("update-status/{id}")]
        public async Task<IActionResult> UpdateLeaveStatus(int id, [FromQuery] string status, [FromBody] string remarks)
        {
            if (!(new[] { "Approved", "Rejected" }.Contains(status)))
                return BadRequest("Status must be 'Approved' or 'Rejected'.");

            var leave = await _db.LeaveRequests.FindAsync(id);
            if (leave == null)
                return NotFound("Leave not found.");

            leave.Status = status;
            leave.AdminRemarks = remarks;

            await _db.SaveChangesAsync();
            return Ok($"Leave {status.ToLower()} successfully.");
        }
    }
}
