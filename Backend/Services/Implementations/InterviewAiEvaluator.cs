using Backend.Common.Configurations;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Backend.Services.Implementations
{
    public class InterviewAiEvaluator : IInterviewAiEvaluator
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiSettings _geminiSettings;
        private readonly ILogger<InterviewAiEvaluator> _logger;

        private static readonly string[] PreferredModels =
        {
            "gemini-flash-lite-latest",
            "gemini-2.5-flash-lite",
            "gemini-3.8-flash"
        };

        public InterviewAiEvaluator(
            IHttpClientFactory httpClientFactory,
            IOptions<GeminiSettings> geminiOptions,
            ILogger<InterviewAiEvaluator> logger)
        {
            _httpClientFactory = httpClientFactory;
            _geminiSettings = geminiOptions.Value;
            _logger = logger;
        }

        public async Task<AiGradingOutcome> EvaluateAnswerAsync(
            string topicName,
            string questionText,
            string difficulty,
            string? sampleAnswer,
            string? keywords,
            string studentAnswer)
        {
            if (string.IsNullOrWhiteSpace(studentAnswer))
            {
                return new AiGradingOutcome(0m, "Chưa đạt", "Học viên chưa cung cấp nội dung câu trả lời.", "AUTO_RUBRIC");
            }

            if (!string.IsNullOrWhiteSpace(_geminiSettings.ApiKey))
            {
                var geminiOutcome = await TryEvaluateWithGeminiAsync(
                    topicName, questionText, difficulty, sampleAnswer, keywords, studentAnswer);
                if (geminiOutcome != null)
                {
                    return geminiOutcome;
                }
            }

            return EvaluateWithFallbackRubric(questionText, sampleAnswer, keywords, studentAnswer);
        }

        private async Task<AiGradingOutcome?> TryEvaluateWithGeminiAsync(
            string topicName,
            string questionText,
            string difficulty,
            string? sampleAnswer,
            string? keywords,
            string studentAnswer)
        {
            var prompt = $@"Bạn là Senior Technical Interviewer tại Học viện đào tạo Fresher Academy.
Hãy chấm điểm câu trả lời phỏng vấn của học viên dựa trên câu hỏi và đáp án chuẩn.

THÔNG TIN CÂU HỎI:
- Lĩnh vực: {topicName}
- Độ khó: {difficulty}
- Câu hỏi: {questionText}
- Đáp án chuẩn tham khảo: {sampleAnswer ?? "(Đánh giá theo kiến thức chuyên môn chuẩn)"}
- Từ khóa cốt lõi cần có: {keywords ?? "(Không giới hạn)"}

CÂU TRẢ LỜI CỦA HỌC VIÊN:
{studentAnswer}

YÊU CẦU CHẤM ĐIỂM:
1. Chấm điểm công tâm trên thang 0.0 đến 10.0 (làm tròn tối đa 1 chữ số thập phân, VD: 8.5).
   - Trả lời lạc đề, vô nghĩa hoặc sai hoàn toàn kiến thức: 0 - 3.5 điểm.
   - Hiểu ý cơ bản nhưng thiếu nhiều ý chính hoặc chưa chính xác chi tiết: 4.0 - 6.5 điểm.
   - Đúng trọng tâm, nêu được hầu hết từ khóa/nguyên lý chính: 7.0 - 8.5 điểm.
   - Chính xác, đầy đủ, có ví dụ hoặc giải thích cơ chế sâu: 9.0 - 10.0 điểm.
2. Xếp loại ""verdict"" phải là 1 trong 4 giá trị chính xác sau:
   ""Xuất sắc"" | ""Đạt yêu cầu"" | ""Cần cải thiện"" | ""Chưa đạt""
3. Viết ""feedback"" bằng tiếng Việt khoảng 2-4 câu súc tích: nêu rõ điểm làm được và điểm cần bổ sung dựa trên câu hỏi.

Trả về DUY NHẤT JSON hợp lệ (không bọc markdown ```json):
{{
  ""score"": 8.5,
  ""verdict"": ""Đạt yêu cầu"",
  ""feedback"": ""Điểm mạnh: ... Điểm cần bổ sung: ...""
}}";

            var client = _httpClientFactory.CreateClient();

            foreach (var model in PreferredModels)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_geminiSettings.ApiKey}";

                    var payload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[] { new { text = prompt } }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.25,
                            maxOutputTokens = 700
                        }
                    };

                    var jsonPayload = JsonSerializer.Serialize(payload);
                    using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    using var response = await client.PostAsync(url, content, cts.Token);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errBody = await response.Content.ReadAsStringAsync(cts.Token);
                        _logger.LogWarning("Gemini ({Model}) returned {StatusCode}: {Body}", model, response.StatusCode, errBody);
                        continue;
                    }

                    var rawResponse = await response.Content.ReadAsStringAsync(cts.Token);
                    using var doc = JsonDocument.Parse(rawResponse);

                    if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                        continue;

                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    var cleanedJson = text.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                                          .Replace("```", "")
                                          .Trim();

                    using var evalDoc = JsonDocument.Parse(cleanedJson);
                    var root = evalDoc.RootElement;

                    decimal score = 7.0m;
                    if (root.TryGetProperty("score", out var scoreProp))
                    {
                        if (scoreProp.ValueKind == JsonValueKind.Number && scoreProp.TryGetDecimal(out var decVal))
                            score = Math.Clamp(Math.Round(decVal, 1), 0m, 10m);
                        else if (scoreProp.ValueKind == JsonValueKind.String && decimal.TryParse(scoreProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var strVal))
                            score = Math.Clamp(Math.Round(strVal, 1), 0m, 10m);
                    }

                    var verdict = root.TryGetProperty("verdict", out var verdictProp) && !string.IsNullOrWhiteSpace(verdictProp.GetString())
                        ? verdictProp.GetString()!
                        : ResolveVerdict(score);

                    var feedback = root.TryGetProperty("feedback", out var fbProp) && !string.IsNullOrWhiteSpace(fbProp.GetString())
                        ? fbProp.GetString()!
                        : "AI đã hoàn tất đánh giá câu trả lời.";

                    return new AiGradingOutcome(score, verdict, feedback, "GEMINI_AI");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gemini AI grading attempt failed on model {Model}", model);
                }
            }

            return null;
        }

        private static AiGradingOutcome EvaluateWithFallbackRubric(
            string questionText,
            string? sampleAnswer,
            string? keywords,
            string studentAnswer)
        {
            var normalizedStudent = studentAnswer.Trim().ToLowerInvariant();
            var keywordList = (keywords ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(k => k.Length > 1)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (keywordList.Count == 0 && !string.IsNullOrWhiteSpace(sampleAnswer))
            {
                keywordList = sampleAnswer
                    .Split(new[] { ' ', ',', '.', ';', ':', '(', ')', '/', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length >= 5)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(8)
                    .ToList();
            }

            var matched = keywordList.Where(k => normalizedStudent.Contains(k.ToLowerInvariant())).ToList();
            var missing = keywordList.Except(matched, StringComparer.OrdinalIgnoreCase).ToList();

            decimal keywordRatio = keywordList.Count > 0 ? (decimal)matched.Count / keywordList.Count : 0.5m;
            int wordCount = studentAnswer.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            decimal depthScore = wordCount switch
            {
                < 5 => 1.0m,
                < 15 => 2.5m,
                < 35 => 3.5m,
                _ => 4.0m
            };

            decimal rawScore = Math.Clamp(Math.Round(depthScore + keywordRatio * 6.0m, 1), 1.0m, 9.5m);
            if (wordCount < 4 && matched.Count == 0)
            {
                rawScore = 2.0m;
            }

            var verdict = ResolveVerdict(rawScore);

            var fbBuilder = new StringBuilder();
            if (matched.Count > 0)
            {
                fbBuilder.Append($"Điểm mạnh: Bạn đã đề cập được các khái niệm cốt lõi ({string.Join(", ", matched.Take(4))}). ");
            }
            else
            {
                fbBuilder.Append("Nhận xét: Câu trả lời còn khá chung chung, chưa nêu bật được thuật ngữ kỹ thuật trọng tâm của câu hỏi. ");
            }

            if (missing.Count > 0)
            {
                fbBuilder.Append($"Điểm cần bổ sung: Nên làm rõ thêm các ý/từ khóa quan trọng như: {string.Join(", ", missing.Take(4))}.");
            }
            else
            {
                fbBuilder.Append("Câu trả lời đã bao phủ rất đầy đủ các ý chính của câu hỏi!");
            }

            return new AiGradingOutcome(rawScore, verdict, fbBuilder.ToString().Trim(), "AUTO_RUBRIC");
        }

        private static string ResolveVerdict(decimal score)
        {
            if (score >= 8.5m) return "Xuất sắc";
            if (score >= 7.0m) return "Đạt yêu cầu";
            if (score >= 5.0m) return "Cần cải thiện";
            return "Chưa đạt";
        }
    }
}
