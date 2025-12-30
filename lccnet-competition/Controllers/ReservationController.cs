using lccnet_competition.Data;
using lccnet_competition.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace lccnet_competition.Controllers
{
    [Authorize]
    public class ReservationController : Controller
    {
        // In-memory store for demo purposes
        private static readonly List<ReservationEntry> _reservations = new List<ReservationEntry>();
        private static readonly object _lock = new object();
        private readonly Lccnet20251124PythonContext _context;

        public ReservationController(Lccnet20251124PythonContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Ensure user is authenticated and has an identifier
            string? accountIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountIdStr) || !int.TryParse(accountIdStr, out var accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            List<EnvReversation> reservations = _context.EnvReversations
                .Where(item => item.AccountId == accountId)
                .ToList();

            return View(reservations);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Reserve(DateTime? startDate, DateTime? endDate)
        {
            // Use TempData so we can redirect to Index and still display messages
            TempData["ReserveError"] = null;
            TempData["ReserveSuccess"] = null;

            if (!startDate.HasValue || !endDate.HasValue)
            {
                TempData["ReserveError"] = "請輸入起始與結束時間";
                return RedirectToAction("Index");
            }

            DateTime s = startDate.Value;
            DateTime e = endDate.Value;

            // normalize to whole hours: start rounded down, end rounded up to next hour if not exact
            DateTime normalizedStart = new DateTime(s.Year, s.Month, s.Day, s.Hour, 0, 0, s.Kind);
            DateTime normalizedEnd = new DateTime(e.Year, e.Month, e.Day, e.Hour, 0, 0, e.Kind);
            if (e.Minute != 0 || e.Second != 0 || e.Millisecond != 0)
            {
                normalizedEnd = normalizedEnd.AddHours(1);
            }

            if (normalizedEnd <= normalizedStart)
            {
                TempData["ReserveError"] = "結束時間必須在起始時間之後（以整點為單位）";
                return RedirectToAction("Index");
            }

            // require start in the future (compare to now)
            if (normalizedStart < DateTime.Now)
            {
                TempData["ReserveError"] = "起始時間必須在未來";
                return RedirectToAction("Index");
            }

            string? accountIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountIdStr) || !int.TryParse(accountIdStr, out var accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            EnvReversation evaReversation = new EnvReversation
            {
                AccountId = accountId,
                BookStartDatetime = normalizedStart,
                BookEndDatetime = normalizedEnd,
            };

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                _context.EnvReversations.Add(evaReversation);
                _context.SaveChanges();
                transaction.Commit();

                TempData["ReserveSuccess"] = "預約成功！";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["ReserveError"] = "預約失敗，請稍後再試。";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Enter()
        {
            string accountIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            string envUrl = _context.Accounts
                .Where(item => item.Id == int.Parse(accountIdStr))
                .Select(item => item.EnvUrl)
                .First();

            return Redirect(envUrl);
        }
        public class ReservationEntry
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }
    }
}
