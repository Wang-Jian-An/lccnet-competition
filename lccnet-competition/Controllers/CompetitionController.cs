using lccnet_competition.Data;
using lccnet_competition.Models;
using lccnet_competition.ViewModels;
using Microsoft.AspNetCore.Authorization; // 用於取得 MIME Type
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
using System.Security.Claims;

namespace lccnet_competition.Controllers
{
    public class CompetitionController : Controller
    {

        private readonly IWebHostEnvironment _env;
        private readonly Lccnet20251124PythonContext _context;

        public CompetitionController(IWebHostEnvironment env, Lccnet20251124PythonContext context)
        {
            _env = env;
            _context = context;
        }

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
        private static string GetContentType(string path)
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

        private static double ComputeF1Score(ConfusionMatrix confusionMatrix)
        {
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
            return f1;
        }

        private static ConfusionMatrix ComputeConfusionMatrix(List<int> prediction, List<int> groundTruth)
        {
            int answerPredictionCount = prediction.Count;
            ConfusionMatrix confusionMatrix = new()
            {
                TP = 0,
                TN = 0,
                FP = 0,
                FN = 0
            };
            for (int j = 0; j < answerPredictionCount; j++)
            {
                if (prediction[j] == 1 && groundTruth[j] == 1)
                {
                    confusionMatrix.TP += 1;
                }
                else if (prediction[j] == 0 && groundTruth[j] == 0)
                {
                    confusionMatrix.TN += 1;
                }
                else if (prediction[j] == 1 && groundTruth[j] == 0)
                {
                    confusionMatrix.FP += 1;
                }
                else
                {
                    confusionMatrix.FN += 1;
                }
            }
            //Console.WriteLine($"{confusionMatrix.TP}, {confusionMatrix.FP}, {confusionMatrix.TN}, {confusionMatrix.FN}");
            return confusionMatrix;
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
                ConfusionMatrix confusionMatrix = ComputeConfusionMatrix(
                    isPredictionClass,
                    isActualClass
                );
                confusionMatrixs[cls] = confusionMatrix;
            }

            // 計算 F1-Score 們
            List<double> f1Scores = new();
            foreach (string cls in Classes)
            {
                ConfusionMatrix confusionMatrix = confusionMatrixs[cls];
                double f1 = ComputeF1Score(confusionMatrix);

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
                    Task = "classification",
                    CreateDatetime = DateTime.UtcNow.AddHours(8)
                };
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
        public IActionResult SubmissionSegmentation(IFormFile submissionFile)
        {
            string accountIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (string.IsNullOrEmpty(accountIdStr) || !int.TryParse(accountIdStr, out var accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (submissionFile == null || submissionFile.Length == 0)
            {
                TempData["SegSubmitError"] = "請先上傳資料";
                return RedirectToAction("Submission");
            }

            List<string> SegmentationMaskAnswerFileNames = [.. Directory.EnumerateFiles(Path.Combine(_env.WebRootPath, "File", "masks"))];
            List<double> finalIoU = [];

            // 讀取ZIP 壓縮檔案
            using (ZipArchive archive = new ZipArchive(submissionFile.OpenReadStream(), ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    if (IsImageFile(entry.Name))
                    {
                        string answerFileName = SegmentationMaskAnswerFileNames
                            .First(f => Path.GetFileName(f) == entry.FullName);
                        List<int> submittedData;
                        List<int> answerData;
                        using (Stream entryStream = entry.Open())
                        {
                            using (Image<L8> submittedImage = SixLabors.ImageSharp.Image.Load<L8>(entryStream))
                            {
                                submittedData = LoadImageFlow(submittedImage);
                            }
                        }
                        using (Stream entryStream = new FileStream(
                            answerFileName,
                            FileMode.Open,
                            FileAccess.Read
                        ))
                        {
                            using (Image<L8> answerImage = SixLabors.ImageSharp.Image.Load<L8>(entryStream))
                            {
                                answerData = LoadImageFlow(answerImage);
                            }
                        }

                        if (submittedData.Count != answerData.Count)
                        {
                            TempData["SegSubmitError"] = $"{entry.Name} 不符合答案所需之圖片大小，請重新檢查與確認。";
                            return RedirectToAction("Submission");
                        }

                        ConfusionMatrix confusionMatrix = ComputeConfusionMatrix(
                            submittedData,
                            answerData
                        );
                        double iou = ComputeIoU(confusionMatrix);
                        Console.WriteLine(iou);
                        finalIoU.Add(iou);

                    } else
                    {
                        TempData["ClassSubmitError"] = "壓縮檔中有非圖片檔案，請檢查後重新上傳。";
                        return RedirectToAction("Submission");
                    }
                }
            }
            double finalMeanIoU = finalIoU.Average();

            // Store the result
            var transaction = _context.Database.BeginTransaction();
            try
            {
                SubmissionRecord record = new()
                {
                    AccountId = accountId,
                    FileName = submissionFile.FileName,
                    IsSuccess = 1,
                    Score = (decimal)finalMeanIoU,
                    CreateDatetime = DateTime.UtcNow.AddHours(8),
                    Task = "segmentation"
                };
                _context.SubmissionRecords.Add(record);
                _context.SaveChanges();

                transaction.Commit();
            } catch (Exception ex)
            {
                transaction.Rollback();
                TempData["ClassSubmitError"] = ex.Message.ToString();
            }

            return RedirectToAction("Submission");
        }

        private static double ComputeIoU(ConfusionMatrix confusionMatrix)
        {
            int decorator = confusionMatrix.TP + confusionMatrix.FP + confusionMatrix.FN;
            if (decorator == 0)
            {
                return 0;
            } else
            {
                Console.WriteLine($"{confusionMatrix.TP}, {decorator}");
                double result = (double)confusionMatrix.TP / (double)decorator;
                return result;
            }
        }

        private static List<int> LoadImageFlow(Image<L8> image)
        {
            int width = image.Width;
            int height = image.Height;

            List<int> pixelData = [];
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < height; y++)
                {
                    Span<L8> row = accessor.GetRowSpan(y);

                    for (int x = 0; x < width; x++)
                    {
                        L8 pixel = row[x];

                        byte pixelValue = pixel.PackedValue;

                        if (pixelValue < 127)
                        {
                            pixelData.Add(0);
                        }
                        else
                        {
                            pixelData.Add(1);
                        }
                    }
                }
            });
            return pixelData;
        }

        private static Boolean IsImageFile(string fileName)
        {
            if (fileName.EndsWith(".jpg") || fileName.EndsWith(".png") || fileName.EndsWith(".jpeg")) 
            {
                return true;
            } else
            {
                return false;
            }
        }

        private List<Leaderboard> GetClassificationRank()
        {
            List<Leaderboard> leaderboards = _context.SubmissionRecords
                .Where(s => s.Task == "classification" && s.Score != null)
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
                .OrderByDescending(x => x.Score)
                .ToList();

            for (int i = 0; i < leaderboards.Count; i++)
            {
                leaderboards[i].Rank = i + 1;
            }
            return leaderboards;
        }

        private List<Leaderboard>  GetSegmentationRank()
        {
            List<Leaderboard> leaderboards = _context.SubmissionRecords
                .Where(s => s.Task == "segmentation" && s.Score != null)
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
                .OrderByDescending(x => x.Score)
                .ToList();

            for (int i = 0; i < leaderboards.Count; i++)
            {
                leaderboards[i].Rank = i + 1;
            }
            return leaderboards;
        }

        public IActionResult Leaderboard()
        {
            var vm = new LeaderboardViewModel
            {
                ClassificationLeaderboard = GetClassificationRank(),
                SegmentationLeaderboard = GetSegmentationRank()
            };

            return View("Leaderboard", vm);
        }
    }
}
