using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Driver;
using QuestPDF.Fluent;
using StoreMetrics.Models;
using StoreMetrics.Repositories;
using StoreMetrics.Services;
using StoreMetrics.ViewModels;
using SkiaSharp;



namespace StoreMetrics.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IEvaluationRepository _evals;
        private readonly IStoreRepository _stores;
        private readonly MongoDbService _mongo;

        public ReportsController(IEvaluationRepository evals, IStoreRepository stores, MongoDbService mongo)
        {
            _evals = evals;
            _stores = stores;
            _mongo = mongo;
        }
        [HttpGet]
        public async Task<IActionResult> StoresByPerformance(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest("Missing category.");

            var evals = (await _evals.GetAllAsync()).ToList();

            var summaries = evals
                .GroupBy(e => e.StoreId)
                .Select(g =>
                {
                    var latest = g.OrderByDescending(x => x.EvaluationDate).First();
                    double avg = g.Average(x => x.AverageRating);

                    return new EvaluationSummaryVm
                    {
                        StoreId = latest.StoreId,
                        StoreName = latest.StoreName ?? "(Unknown Store)",
                        AverageRating = Math.Round(avg, 2),
                        PerformancePercent = Math.Round((avg / 5.0) * 100.0, 2),
                        PerformanceDescription = ScoreToLabel(avg),
                        EvaluationDate = latest.EvaluationDate
                    };
                })
                .Where(s => s.PerformanceDescription == category)
                .OrderByDescending(s => s.AverageRating)
                .ToList();

            return PartialView("_StoresByPerformanceTable", summaries);
        }


        // ---------- OVERVIEW ----------
        [HttpGet]
        public async Task<IActionResult> Overview(string? storeId = null, int? year = null, string? quarter = null, string? search = null)
        {
            var evals = (await _evals.GetAllAsync()).ToList();
            var storeDict = (await _stores.GetAllAsync())
                .ToDictionary(s => s.Id ?? "", s => s.StoreName);

            // FILTER YEAR + QUARTER
            if (year.HasValue)
                evals = evals.Where(e => e.EvaluationDate.Year == year.Value).ToList();

            if (!string.IsNullOrEmpty(quarter) && quarter != "Quarter")
            {
                int startMonth = quarter switch
                {
                    "Q1" => 1,
                    "Q2" => 4,
                    "Q3" => 7,
                    "Q4" => 10,
                    _ => 1
                };

                evals = evals.Where(e =>
                    e.EvaluationDate.Month >= startMonth &&
                    e.EvaluationDate.Month <= startMonth + 2
                ).ToList();
            }

            // GROUP & SUMMARIZE
            var summaries = evals
                .GroupBy(e => e.StoreId)
                .Select(g =>
                {
                    var latest = g.OrderByDescending(x => x.EvaluationDate).First();
                    var name = !string.IsNullOrWhiteSpace(latest.StoreName)
                        ? latest.StoreName
                        : (storeDict.TryGetValue(latest.StoreId, out var n) ? n : "(Unknown Store)");

                    var avg = g.Average(x => x.AverageRating);

                    return new EvaluationSummaryVm
                    {
                        StoreId = latest.StoreId,
                        StoreName = name,
                        EvaluationDate = latest.EvaluationDate,
                        AverageRating = Math.Round(avg, 2),
                        PerformancePercent = Math.Round((avg / 5.0) * 100.0, 2),
                        PerformanceDescription = ScoreToLabel(avg),
                        EvaluationId = latest.Id ?? ""
                    };
                })
                .OrderByDescending(x => x.PerformancePercent)
                .ThenByDescending(x => x.AverageRating)
                .ToList();

            // ---------- GLOBAL RANKING (FIXED RANK ISSUE) ----------
            double? prevScore = null;
            int rankCounter = 0;

            for (int i = 0; i < summaries.Count; i++)
            {
                double score = summaries[i].PerformancePercent;

                if (prevScore == null || score != prevScore)
                    rankCounter++;

                summaries[i].Rank = rankCounter;
                prevScore = score;
            }

            // ---------- PERFORMANCE FILTER ----------
            var performanceFilter = Request.Query["performanceFilter"].ToString();

            if (!string.IsNullOrEmpty(performanceFilter))
            {
                summaries = summaries
                    .Where(s => s.PerformanceDescription == performanceFilter)
                    .ToList();
            }

            // TOP PERFORMERS (unchanged)
            if (summaries.Any())
            {
                double topScore = summaries.First().PerformancePercent;
                summaries.Where(s => s.PerformancePercent == topScore)
                         .ToList()
                         .ForEach(s => s.IsTopPerformer = true);
            }

            // STORE FILTER
            if (!string.IsNullOrEmpty(storeId))
                summaries = summaries.Where(s => s.StoreId == storeId).ToList();

            // SEARCH FILTER
            if (!string.IsNullOrEmpty(search))
            {
                string keyword = search.Trim().ToLower();
                summaries = summaries
                    .Where(s => s.StoreName != null && s.StoreName.ToLower().Contains(keyword))
                    .ToList();
            }

            // FILTER OPTIONS
            ViewBag.AllStores = storeDict.Select(kv => new { Key = kv.Key, Value = kv.Value }).ToList();
            ViewBag.SelectedYear = year;
            ViewBag.SelectedQuarter = quarter ?? "All";
            ViewBag.YearOptions = evals.Select(e => e.EvaluationDate.Year)
                                       .Distinct()
                                       .OrderByDescending(y => y)
                                       .ToList();
            ViewBag.Search = search;

            // LATEST MAP
            var latestByStore = evals
                .GroupBy(e => e.StoreId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var latest = g.OrderByDescending(x => x.EvaluationDate).First();
                        latest.StoreName = !string.IsNullOrWhiteSpace(latest.StoreName)
                            ? latest.StoreName
                            : (storeDict.TryGetValue(latest.StoreId, out var n) ? n : "(Unknown Store)");
                        return latest;
                    });

            // ---------- STORE MAP WITH FULL ADDRESS (STRING VERSION, SAFE FOR RAZOR) ----------
            var storeMapFull = (await _stores.GetAllAsync())
                .ToDictionary(
                    s => s.Id!,
                    s => $"{s.BuildingNumber} {s.StreetName}, {s.Brgy}, {s.City}, {s.Province} {s.PostalCode}"
                );

            ViewBag.StoreMap = storeMapFull;


            ViewBag.LatestMap = latestByStore;


            // ========== PAGINATION ==========
            int pageSize = 10;
            int page = int.TryParse(Request.Query["page"], out var p) ? p : 1;

            int totalItems = summaries.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            summaries = summaries
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View(summaries);
        }

        private static string ScoreToLabel(double avg) => avg switch
        {
            >= 4.5 => "Excellent",
            >= 3.5 => "Good",
            >= 2.5 => "Average",
            >= 1.5 => "Poor",
            _ => "Very Poor"
        };
        [HttpGet]
        public async Task<IActionResult> DownloadStoreReport(string storeId)
        {
            var store = await _stores.GetAsync(storeId);
            if (store == null) return NotFound("Store not found.");

            var eval = (await _evals.GetByStoreAsync(storeId))
                .OrderByDescending(e => e.EvaluationDate)
                .FirstOrDefault();

            if (eval == null) return NotFound("No evaluation found for this store.");

            // --- Build bar graph with SkiaSharp ---
            var bitmap = new SkiaSharp.SKBitmap(800, 500);
            using (var canvas = new SkiaSharp.SKCanvas(bitmap))
            {
                canvas.Clear(SkiaSharp.SKColors.White);

                int[] values =
                {
            eval.Cleanliness,
            eval.Condition,
            eval.CustomerEngagement,
            eval.PersonalGrooming,
            eval.Accuracy,
            eval.SpeedOfService,
            eval.ProductQuality
        };

                string[] labels =
                {
            "Cleanliness", "Condition", "Customer Engagement",
            "Personal Grooming", "Accuracy", "Speed of Service", "Product Quality"
        };

                var colors = new[]
                {
            SkiaSharp.SKColors.SkyBlue,
            SkiaSharp.SKColors.Goldenrod,
            SkiaSharp.SKColors.MediumTurquoise,
            SkiaSharp.SKColors.MediumPurple,
            SkiaSharp.SKColors.CornflowerBlue,
            SkiaSharp.SKColors.LightGreen,
            SkiaSharp.SKColors.Orange
        };

                int barWidth = 70;
                int spacing = 30;
                int startX = 50;

                // ============================================
                // GRID LINES (background)
                // ============================================
                var gridPaint = new SkiaSharp.SKPaint
                {
                    Color = SkiaSharp.SKColors.LightGray,
                    StrokeWidth = 2,
                    Style = SkiaSharp.SKPaintStyle.Stroke,
                    IsAntialias = true
                };

                int graphTop = 50;
                int graphBottom = 350;
                int totalLevels = 5; // rating 1–5

                for (int lvl = 1; lvl <= totalLevels; lvl++)
                {
                    float y = graphBottom - (lvl * 60); // evenly spaced levels
                    canvas.DrawLine(40, y, 760, y, gridPaint);
                }

                // ============================================
                // Y-AXIS NUMBERS (1–5)
                // ============================================
                var numberPaint = new SkiaSharp.SKPaint
                {
                    Color = SkiaSharp.SKColors.Black,
                    TextSize = 18,
                    IsAntialias = true,
                    TextAlign = SkiaSharp.SKTextAlign.Right
                };

                for (int lvl = 1; lvl <= totalLevels; lvl++)
                {
                    float y = graphBottom - (lvl * 60);
                    canvas.DrawText(lvl.ToString(), 35, y + 6, numberPaint);
                }

                // ============================================
                // DRAW BARS + LABELS
                // ============================================
                for (int i = 0; i < values.Length; i++)
                {
                    int barHeight = values[i] * 60;

                    var barPaint = new SkiaSharp.SKPaint
                    {
                        Color = colors[i],
                        IsAntialias = true
                    };

                    float x = startX + i * (barWidth + spacing);
                    float y = graphBottom - barHeight;

                    // Draw bar
                    canvas.DrawRect(x, y, barWidth, barHeight, barPaint);

                    // -------- Multi-line label logic --------
                    string label = labels[i];
                    var words = label.Split(' ');
                    List<string> lines = new();

                    if (words.Length == 1)
                        lines.Add(words[0]);
                    else if (words.Length == 2)
                        lines.AddRange(words);
                    else
                    {
                        lines.Add(words[0]);
                        lines.Add(words[1]);
                        lines.Add(string.Join(" ", words.Skip(2)));
                    }

                    var textPaint = new SkiaSharp.SKPaint
                    {
                        Color = SkiaSharp.SKColors.Black,
                        TextSize = 16,
                        IsAntialias = true,
                        TextAlign = SkiaSharp.SKTextAlign.Center
                    };

                    float textX = x + (barWidth / 2f);
                    float baseY = 385;

                    // Draw each line
                    for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                    {
                        float offsetY = baseY + (lineIndex * 18);
                        canvas.DrawText(lines[lineIndex], textX, offsetY, textPaint);
                    }
                }
            }

            // Convert to PNG byte array
            byte[] chartBytes;
            using (var image = SkiaSharp.SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
            {
                chartBytes = data.ToArray();
            }

            // Generate PDF
            var doc = new StoreReportPdfDocument(store, eval, chartBytes);
            var pdfBytes = doc.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"{store.StoreName}_Report.pdf");
        }


        // ---------- EDIT ----------
        [HttpPost]
        public async Task<IActionResult> EditEvaluation([FromBody] EvaluationVm updated)
        {
            if (string.IsNullOrEmpty(updated.StoreId))
                return BadRequest("Invalid store ID.");

            await _evals.UpdateAsync(updated);

            var db = _mongo.StoreCollection.Database;
            var ratings = db.GetCollection<StoreRating>("store_ratings");
            var performances = db.GetCollection<StorePerformance>("store_performances");

            var ratingFilter = Builders<StoreRating>.Filter.Eq(r => r.StoreId, updated.StoreId);
            var perfFilter = Builders<StorePerformance>.Filter.Eq(p => p.StoreId, updated.StoreId);

            var existingRating = await ratings.Find(ratingFilter).FirstOrDefaultAsync();
            var existingPerf = await performances.Find(perfFilter).FirstOrDefaultAsync();

            var storeRating = new StoreRating
            {
                Id = existingRating?.Id,
                StoreId = updated.StoreId,
                StoreName = updated.StoreName,
                Cleanliness = updated.Cleanliness,
                Condition = updated.Condition,
                CustomerEngagement = updated.CustomerEngagement,
                PersonalGrooming = updated.PersonalGrooming,
                Accuracy = updated.Accuracy,
                SpeedOfService = updated.SpeedOfService,
                ProductQuality = updated.ProductQuality,
                Remarks = updated.Remarks,
                EvaluationDate = updated.EvaluationDate
            };

            var storePerf = new StorePerformance
            {
                Id = existingPerf?.Id,
                StoreId = updated.StoreId,
                StoreName = updated.StoreName,
                AverageRating = updated.AverageRating,
                PerformancePercent = updated.PerformancePercent,
                PerformanceDescription = updated.PerformanceDescription,
                EvaluationDate = updated.EvaluationDate
            };

            if (existingRating != null)
                await ratings.ReplaceOneAsync(ratingFilter, storeRating);
            else
                await ratings.InsertOneAsync(storeRating);

            if (existingPerf != null)
                await performances.ReplaceOneAsync(perfFilter, storePerf);
            else
                await performances.InsertOneAsync(storePerf);

            return Ok(new { success = true });
        }

        // ---------- DELETE ----------
        [HttpPost]
        public async Task<IActionResult> DeleteEvaluation([FromBody] string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Missing ID.");

            var eval = await _evals.GetAsync(id);
            if (eval == null) return NotFound();

            await _evals.DeleteAsync(id);

            var db = _mongo.StoreCollection.Database;
            var ratings = db.GetCollection<StoreRating>("store_ratings");
            var performances = db.GetCollection<StorePerformance>("store_performances");

            await ratings.DeleteOneAsync(Builders<StoreRating>.Filter.Eq(r => r.StoreId, eval.StoreId));
            await performances.DeleteOneAsync(Builders<StorePerformance>.Filter.Eq(p => p.StoreId, eval.StoreId));

            return Ok(new { success = true });
        }

        // ---------- STORE REPORTS ----------
        [HttpGet]
        public async Task<IActionResult> StoreReports(string? storeId = null)
        {
            // GET STORES
            var stores = (await _stores.GetAllAsync()).ToList();

            // COUNT DUPLICATE STORE NAMES
            var nameCounts = stores
                .GroupBy(s => s.StoreName)
                .ToDictionary(g => g.Key, g => g.Count());

            // BUILD SELECT LIST (address shown ONLY if name duplicated)
            var storeOptions = stores.Select(s => new
            {
                Id = s.Id,
                Label = nameCounts[s.StoreName] > 1
                    ? $"{s.StoreName} — {(string.IsNullOrWhiteSpace(s.BuildingNumber) ? "" : s.BuildingNumber + " ")}{s.StreetName}, {s.Brgy}, {s.City}"
                    : s.StoreName
            }).ToList();

            // SEND TO VIEW
            ViewBag.StoreOptions = new SelectList(storeOptions, "Id", "Label", storeId);


            // SEND TO VIEW
            ViewBag.StoreOptions = new SelectList(storeOptions, "Id", "Label", storeId);


            var selectedId = storeId ?? stores.FirstOrDefault()?.Id;
            if (string.IsNullOrWhiteSpace(selectedId))
                return View(new StoreReportVm { HasData = false });

            var selectedName = stores.FirstOrDefault(s => s.Id == selectedId)?.StoreName ?? "(Unknown Store)";
            var evals = (await _evals.GetByStoreAsync(selectedId)).ToList();

            var vm = new StoreReportVm
            {
                StoreId = selectedId!,
                StoreName = selectedName,
                HasData = evals.Any()
            };

            if (vm.HasData)
            {
                vm.Cleanliness = Math.Round(evals.Average(e => (double)e.Cleanliness), 2);
                vm.Condition = Math.Round(evals.Average(e => (double)e.Condition), 2);
                vm.CustomerEngagement = Math.Round(evals.Average(e => (double)e.CustomerEngagement), 2);
                vm.PersonalGrooming = Math.Round(evals.Average(e => (double)e.PersonalGrooming), 2);
                vm.Accuracy = Math.Round(evals.Average(e => (double)e.Accuracy), 2);
                vm.SpeedOfService = Math.Round(evals.Average(e => (double)e.SpeedOfService), 2);
                vm.ProductQuality = Math.Round(evals.Average(e => (double)e.ProductQuality), 2);
            }

            return View(vm);
        }
        [HttpGet]
        public async Task<IActionResult> StorePerformance()
        {
            var evals = (await _evals.GetAllAsync()).ToList();

            var summaries = evals
                .GroupBy(e => e.StoreId)
                .Select(g =>
                {
                    var latest = g.OrderByDescending(x => x.EvaluationDate).First();
                    double avg = g.Average(x => x.AverageRating);

                    return new EvaluationSummaryVm
                    {
                        StoreId = latest.StoreId,
                        StoreName = latest.StoreName ?? "(Unknown Store)",
                        AverageRating = Math.Round(avg, 2),
                        PerformancePercent = Math.Round((avg / 5.0) * 100.0, 2),
                        PerformanceDescription = ScoreToLabel(avg),
                        EvaluationDate = latest.EvaluationDate
                    };
                })
                .ToList();

            var performanceCounts = summaries
                .GroupBy(s => s.PerformanceDescription)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.PerformanceCounts = performanceCounts;

            return View(summaries);
        }


    }
}
