using Microsoft.AspNetCore.Mvc;

namespace lccnet_competition.Controllers
{
    public class CompetitionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Data()
        {
            return View();
        }

        public IActionResult Submission()
        {
            return View();
        }

        public IActionResult Leaderboard()
        {
            return View();
        }
    }
}
