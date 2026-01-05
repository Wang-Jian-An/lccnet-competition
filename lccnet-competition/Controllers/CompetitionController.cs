using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using lccnet_competition.Data;
using lccnet_competition.Models;
using System.Security.Claims;
using Microsoft.Identity.Client;
using System.Transactions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // 用於取得 MIME Type

namespace lccnet_competition.Controllers
{
    public class CompetitionController : Controller
    {

        private readonly IWebHostEnvironment _env;
        private readonly Lccnet20251124PythonContext _context;
        private string[] Classes = new string[]
        {
            "glioma",
            "meningioma",
            "no_tumor",
            "pituitary"
        };
        public class ConfusionMatrix
        {
            public int TP { get; set; }
            public int TN { get; set; }
            public int FP { get; set; }
            public int FN { get; set; }
        }

        public CompetitionController(IWebHostEnvironment env, Lccnet20251124PythonContext context)
        {
            _env = env;
            _context = context;
        }

        [Authorize]
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
            string fileName = "";
            if (file == "train-classification" || file == "train-segmentation" || file == "test-classification" || file == "test-segmentation")
            {
                fileName = $"{file}.zip";
            }
            else if (file == "classification-submission")
            {
                fileName = $"{file}.csv";
            }
            else
            {
                return NotFound();
            }

            var filePath = Path.Combine(_env.WebRootPath, "File", fileName);

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

        [Authorize]
        public IActionResult Submission()
        {
            string? accountIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountIdStr) || !int.TryParse(accountIdStr, out var accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var data = _context.SubmissionRecords
                .Where(item => item.AccountId == accountId)
                .ToList();
            return View(data);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult SubmissionClassification(IFormFile submissionFile)
        {
            // 檢查是否有登入
            string? accountIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountIdStr) || !int.TryParse(accountIdStr, out var accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            // 檢查是否有上傳檔案
            if (submissionFile == null || submissionFile.Length == 0)
            {
                TempData["ClassSubmitError"] = "請先上傳資料";
                return RedirectToAction("Submission");
            }
            var file = submissionFile;
            string filename = submissionFile.FileName;

            // 讀取 CSV 檔案
            List<ClassSubmissionFormat> rawPredictions = new List<ClassSubmissionFormat>();
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null || line == "ID,Answer") continue;
                    string[] values = line!.Split(',');
                    rawPredictions.Add(
                        new ClassSubmissionFormat()
                        {
                            Id = values[0],
                            Answer = values[1]
                        }
                    );
                }
            }

            // 讀取正確答案
            string answerPath = Path.Combine(_env.WebRootPath, "File", "classification-answers.csv");
            List<ClassSubmissionFormat> rawAnswers = new List<ClassSubmissionFormat>();
            using (var reader = new StreamReader(answerPath))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null || line == "ID,Answer") continue;
                    string[] values = line!.Split(',');
                    rawAnswers.Add(
                        new ClassSubmissionFormat()
                        {
                            Id = values[0],
                            Answer = values[1]
                        }
                    );
                }
            }

            // 如果上傳的答案數量與正確答案數量不符
            if (rawPredictions.Count != rawAnswers.Count)
            {
                TempData["ClassSubmitError"] = "資料筆數有不一致";
                return RedirectToAction("Submission");
            }

            // 按照Key 進行排序，並只挑選出Value
            List<string> predictions = rawPredictions
                .OrderBy(item => item.Id)
                .Select(item => item.Answer)
                .ToList();
            List<string> answers = rawAnswers
                .OrderBy(item => item.Id)
                .Select(item => item.Answer)
                .ToList();

            // 依照四種不同類別，各自計算混淆矩陣
            int answerPredictionCount = predictions.Count();
            Dictionary<string, ConfusionMatrix> confusionMatrixs =
                new Dictionary<string, ConfusionMatrix>();
            foreach (var cls in Classes)
            {
                List<int> isPredictionClass = predictions
                    .Select(item => item == cls ? 1 : 0)
                    .ToList();
                List<int> isActualClass = answers
                    .Select(item => item == cls ? 1 : 0)
                    .ToList();
                confusionMatrixs[cls] = new ConfusionMatrix()
                {
                    TP = 0,
                    TN = 0,
                    FP = 0,
                    FN = 0
                };
                for (int j = 0; j < answerPredictionCount; j++)
                {
                    if (isPredictionClass[j] == 1 && isActualClass[j] == 1)
                    {
                        confusionMatrixs[cls].TP += 1;
                    }
                    else if (isPredictionClass[j] == 0 && isActualClass[j] == 0)
                    {
                        confusionMatrixs[cls].TN += 1;
                    }
                    else if (isPredictionClass[j] == 1 && isActualClass[j] == 0)
                    {
                        confusionMatrixs[cls].FP += 1;
                    }
                    else
                    {
                        confusionMatrixs[cls].FN += 1;
                    }
                }
            }

            // 計算 F1-Score 們
            List<double> f1Scores = new();
            foreach (string cls in Classes)
            {
                ConfusionMatrix confusionMatrix = confusionMatrixs[cls];
                double precision = 0.0;
                double recall = 0.0;

                // 如果沒有預測為該類別，precision 視為 0
                if (confusionMatrix.TP + confusionMatrix.FP > 0)
                {
                    precision = (double)confusionMatrix.TP / (confusionMatrix.TP + confusionMatrix.FP);
                }

                // 如果沒有實際為該類別，recall 視為 0
                if (confusionMatrix.TP + confusionMatrix.FN > 0)
                {
                    recall = (double)confusionMatrix.TP / (confusionMatrix.TP + confusionMatrix.FN);
                }

                double f1 = 0.0;
                if (precision + recall > 0)
                {
                    f1 = (2 * precision * recall) / (precision + recall);
                }

                f1Scores.Add(f1);
            }

            double macroF1Score = f1Scores.Any() ? f1Scores.Average() : 0.0;

            // 保險檢查：如果計算出來是 NaN 或 Infinity，設為 0（資料庫無法存無限大）
            if (double.IsNaN(macroF1Score) || double.IsInfinity(macroF1Score))
            {
                macroF1Score = 0.0;
            }

            // 透過交易將結果回傳至資料庫
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                SubmissionRecord data = new SubmissionRecord()
                {
                    AccountId = accountId,
                    IsSuccess = 1,
                    FileName = filename,
                    Score = (decimal)macroF1Score,
                    Task = "classification"
                };
                Console.WriteLine(data);
                _context.SubmissionRecords.Add(data);
                _context.SaveChanges();
                transaction.Commit();


            }
            catch (Exception ex)
            {
                TempData["ClassSubmitError"] = ex.Message;
                transaction.Rollback();
            }

            return RedirectToAction("Submission");

        }

        public IActionResult DownloadClassificationSubmission()
        {
            string fileName = "classification-submission.csv";
            var filePath = Path.Combine(_env.WebRootPath, "File", fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var contentType = GetContentType(filePath);
            return PhysicalFile(filePath, contentType, fileName);
        }

        [HttpPost]
        public IActionResult SubmissionSegmentation()
        {
            return View();
        }

        public IActionResult Leaderboard(string task)
        {
            List<Leaderboard> leaderboards = _context.SubmissionRecords
                .Where(s => s.Task == task && s.Score != null)
                .Join(
                    _context.Accounts,
                    s => s.AccountId,
                    a => a.Id,
                    (s, a) => new { s, a }
                )
                .GroupBy(x => new { x.a.Id, x.a.Username })
                .Select(g => new Leaderboard
                {
                    Rank = 0,
                    Username = g.Key.Username,

                    // 最低分 / 最佳分（依你需求）
                    Score = g.Max(x => (double)x.s.Score!),

                    SubmissionCount = g.Count(),

                    // 最後一次提交
                    LastSubmissionDateTime = g.Max(x => x.s.CreateDatetime)
                })
                .OrderBy(x => x.Score)
                .ToList();

            for (int i = 0; i < leaderboards.Count; i++)
            {
                leaderboards[i].Rank = i + 1;
            }
            Console.WriteLine(leaderboards.Count);

            return View("Leaderboard", leaderboards);
        }
    }
}
