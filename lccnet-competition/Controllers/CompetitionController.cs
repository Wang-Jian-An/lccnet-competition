using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles; // 用於取得 MIME Type

namespace lccnet_competition.Controllers
{
    public class CompetitionController : Controller
    {

        private readonly IWebHostEnvironment _env;

        public CompetitionController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Data()
        {
            return View();
        }

        public IActionResult Download(string file)
        {
            var filePath = Path.Combine(_env.WebRootPath, "File", $"{file}.zip");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var contentType = GetContentType(filePath);

            return PhysicalFile(filePath, contentType, $"{file}.zip");
        }

        // 輔助方法：自動判斷 MIME Type
        private string GetContentType(string path)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out string? contentType))
            {
                contentType = "application/octet-stream"; // 預設二進位流
            }
            return contentType;
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
