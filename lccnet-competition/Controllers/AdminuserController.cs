using Microsoft.AspNetCore.Mvc;
using lccnet_competition.Data;
using System.Linq;

namespace lccnet_competition.Controllers
{
    public class AdminuserController : Controller
    {
        private readonly Lccnet20251124PythonContext _db;

        public AdminuserController(Lccnet20251124PythonContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var accounts = _db.Accounts.OrderBy(a => a.Id).ToList();
            return View(accounts);
        }
    }
}
