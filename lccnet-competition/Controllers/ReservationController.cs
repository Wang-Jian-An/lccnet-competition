using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace lccnet_competition.Controllers
{
    public class ReservationController : Controller
    {
        // In-memory store for demo purposes
        private static readonly List<ReservationEntry> _reservations = new List<ReservationEntry>();
        private static readonly object _lock = new object();

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Reserve([FromBody] ReservationRequest req)
        {
            if (req == null)
            {
                return Json(new { success = false, error = "請提供完整的預約資料" });
            }

            DateTime start = req.Start;
            DateTime end = req.End;

            if (end <= start)
            {
                return Json(new { success = false, error = "結束時間必須晚於開始時間" });
            }

            lock (_lock)
            {
                // simple overlap check
                var conflict = _reservations.Any(r => start < r.End && end > r.Start);
                if (conflict)
                {
                    return Json(new { success = false, error = "該時段已被預約" });
                }

                var entry = new ReservationEntry
                {
                    Id = Guid.NewGuid(),
                    Title = string.IsNullOrWhiteSpace(req.Title) ? "已預約" : req.Title.Trim(),
                    Start = start,
                    End = end
                };
                _reservations.Add(entry);
            }

            return Json(new { success = true });
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
