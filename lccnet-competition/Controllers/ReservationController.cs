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

            string accountIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            List<EnvReversation> reservations = _context.EnvReversations
                .Where(item => item.AccountId == int.Parse(accountIdStr))
                .ToList();

            return View(reservations);
        }

        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public IActionResult Reserve(DateTime? startDate, DateTime? endDate)
        {
            ViewData["ReserveError"] = null;
            ViewData["ReserveSuccess"] = null;

            if (!startDate.HasValue || !endDate.HasValue)
            {
                ViewData["ReserveError"] = "請輸入起始與結束時間";
                return View();
            }

            // 兩個都有值
            DateTime s = startDate.Value;
            DateTime e = endDate.Value;

            // The end date must be after the start date
            if (e <= s)
            {
                ViewData["ReserveError"] = "結束時間必須在起始時間之後";
                return View("Index");
            }

            if (s < DateTime.Now)
            {
                ViewData["ReserveError"] = "起始時間必須在未來";
                return View("Index");
            }
            
            // Get Account Id
            string accountIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            double totalTime = _context.EnvReversations
                .Where(item => item.AccountId == int.Parse(accountIdStr))
                .Sum(item =>
                    EF.Functions.DateDiffSecond(
                        item.BookStartDatetime,
                        item.BookEndDatetime
                    )
                );
            double newTotalTime = totalTime + EF.Functions.DateDiffSecond(s, e);
            
            if (accountIdStr == null)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                EnvReversation evaReversation = new EnvReversation
                {
                    AccountId = int.Parse(accountIdStr),
                    BookStartDatetime = s,
                    BookEndDatetime = e,
                };

                using var transaction = _context.Database.BeginTransaction();

                try
                {
                    _context.EnvReversations.Add(evaReversation);
                    _context.SaveChanges();
                    transaction.Commit();

                    ViewData["ReserveSuccess"] = "預約成功！";
                    return View("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewData["ReserveError"] = "預約失敗，請稍後再試。";
                    return View();
                }
            }
        }

        public class ReservationRequest
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Title { get; set; }
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
