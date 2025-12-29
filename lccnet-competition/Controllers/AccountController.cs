using lccnet_competition.Data;
using lccnet_competition.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace lccnet_competition.Controllers
{

    public class AccountController : Controller
    {

        private readonly Lccnet20251124PythonContext _context;

        public AccountController(Lccnet20251124PythonContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return Ok("正常");
        }

        public IActionResult Login()
        {
            return View("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            string sha256 = ToSha256(password);
            int accountNum = _context.Accounts
                .Where(item => item.Username == username && item.Sha256 == sha256)
                .Count()!;
            
            if (accountNum == 1) {
                int accountId = _context.Accounts
                    .Where(item => item.Username == username && item.Sha256 == sha256)
                    .Select(item => item.Id)
                    .First();

                var claim = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, accountId.ToString()),
                    new Claim(ClaimTypes.Name, username)
                };

                var claimIdentity = new ClaimsIdentity(
                    claim,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimIdentity)
                );

                Console.WriteLine($"帳號：{username}、密碼：{password}、{accountId}");
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        public static string ToSha256(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

            var sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2")); // 轉成 hex

            return sb.ToString();
        }
    }
}
