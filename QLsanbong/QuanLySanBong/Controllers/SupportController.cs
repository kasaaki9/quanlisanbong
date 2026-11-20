using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QuanLySanBong.Data;

namespace QuanLySanBong.Controllers;

[Route("Support")]
public class SupportController : Controller
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupportController> _logger;

    public SupportController(AppDbContext db, IHttpClientFactory clients, IConfiguration configuration, ILogger<SupportController> logger)
    {
        _db = db;
        _clients = clients;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("Ask")]
    [EnableRateLimiting("support-chat")]
    public async Task<IActionResult> Ask([FromBody] SupportChatRequest? request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 1000)
            return BadRequest(new { reply = "Bạn nhập câu hỏi tối đa 1.000 ký tự nhé." });

        var context = await BuildPublicContext(cancellationToken);
        try
        {
            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = "Bạn là trợ lý hỗ trợ khách hàng của Sân Phủi 90. " +
                        "Luôn trả lời bằng tiếng Việt có dấu, tự nhiên, tối đa 2 câu ngắn. " +
                        "Nếu khách chỉ chào, chào lại bằng một câu và hỏi họ cần hỗ trợ gì. " +
                        "Chỉ đưa câu trả lời cuối; không trình bày suy luận, phân tích, kế hoạch hoặc lặp lại yêu cầu. " +
                        "Không bịa thông tin; nếu dữ liệu thiếu, nói rõ và hướng dẫn liên hệ 0909 123 456.\n\n" +
                        "DỮ LIỆU SÂN VÀ GIÁ:\n" + context
                }
            };
            messages.AddRange((request.History ?? new())
                .Where(turn => (turn.Role == "user" || turn.Role == "assistant") && !string.IsNullOrWhiteSpace(turn.Content))
                .TakeLast(8)
                .Select(turn => (object)new
                {
                    role = turn.Role,
                    content = turn.Content.Length > 1000 ? turn.Content[..1000] : turn.Content
                }));
            messages.Add(new { role = "user", content = request.Message });

            var payload = JsonSerializer.Serialize(new
            {
                model = _configuration["Ollama:Model"] ?? "gemma3:4b",
                messages,
                think = false,
                stream = false,
                options = new { num_predict = 120 }
            });

            using var client = _clients.CreateClient("Ollama");
            using var response = await client.PostAsync(
                "api/chat",
                new StringContent(payload, Encoding.UTF8, "application/json"),
                cancellationToken);

            if ((int)response.StatusCode == 404)
            {
                _logger.LogWarning("Ollama model {Model} was not found.", _configuration["Ollama:Model"] ?? "gemma3:4b");
                return Ok(new { reply = BuildFallbackReply(request.Message, context), mode = "faq-model-missing" });
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Local Ollama support request failed with HTTP {StatusCode}.", (int)response.StatusCode);
                return Ok(new { reply = BuildFallbackReply(request.Message, context), mode = "faq-ollama-error" });
            }

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var reply = ExtractReply(json.RootElement);
            return Ok(new { reply = string.IsNullOrWhiteSpace(reply) ? BuildFallbackReply(request.Message, context) : reply, mode = "ai" });
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning("Could not connect to local Ollama at {OllamaUrl}.", _configuration["Ollama:BaseUrl"] ?? "http://localhost:11434");
            return Ok(new { reply = BuildFallbackReply(request.Message, context), mode = "faq-ollama-unavailable" });
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Local Ollama support request timed out.");
            return Ok(new { reply = BuildFallbackReply(request.Message, context), mode = "faq-ollama-timeout" });
        }
        catch (JsonException)
        {
            _logger.LogWarning("Local Ollama support response could not be parsed.");
            return Ok(new { reply = BuildFallbackReply(request.Message, context), mode = "faq-ollama-error" });
        }
    }

    private async Task<string> BuildPublicContext(CancellationToken cancellationToken)
    {
        var courts = await _db.SanBongs.AsNoTracking().Include(x => x.LoaiSan).Include(x => x.BangGias)
            .OrderBy(x => x.TenSan).Take(30).ToListAsync(cancellationToken);
        if (courts.Count == 0) return "Chưa có sân được đăng trên hệ thống.";
        return string.Join("\n", courts.Select(court =>
        {
            var prices = court.BangGias.Count == 0 ? "chưa có giá niêm yết" : string.Join(", ", court.BangGias
                .OrderBy(price => price.GioBatDau)
                .Select(price => $"{price.GioBatDau:hh\\:mm}-{price.GioKetThuc:hh\\:mm}: {price.GiaTien:N0}đ ({price.LoaiNgay ?? "mọi ngày"})"));
            return $"{court.TenSan}; loại {court.LoaiSan?.TenLoaiSan ?? "chưa phân loại"}; địa chỉ {court.DiaChi ?? "chưa cập nhật"}; trạng thái {court.TrangThai}; giá {prices}.";
        }));
    }

    private static string BuildFallbackReply(string message, string context)
    {
        var question = message.ToLowerInvariant();
        if (question.Contains("giá") || question.Contains("bao nhiêu") || question.Contains("chi phí"))
            return "Thông tin sân và giá hiện có:\n" + context + "\nBạn mở mục Đặt sân để chọn ngày và giờ phù hợp nhé.";
        if (question.Contains("đặt") || question.Contains("booking") || question.Contains("thanh toán"))
            return "Bạn vào mục Đặt sân, chọn sân và ngày chơi, xem các khung giờ còn trống rồi bấm Đặt để sang bước thanh toán. Đặt sân chỉ được xác nhận sau khi hoàn tất quy trình trên trang.";
        if (question.Contains("địa điểm") || question.Contains("địa chỉ") || question.Contains("khu vực"))
            return "Bạn vào trang Đặt sân và chọn tỉnh/thành ở bộ lọc địa điểm. Danh sách sẽ lọc theo thông tin vị trí mà sân đã cập nhật.";
        if (question.Contains("đánh giá") || question.Contains("bình luận"))
            return "Bạn mở chi tiết sân để xem đánh giá. Nếu đã đặt sân, bạn có thể gửi nhận xét tại phần đánh giá của sân.";
        return "Mình có thể hỗ trợ tìm sân, xem giá, chọn giờ và hướng dẫn đặt sân. Bạn hỏi cụ thể hơn nhé; nếu cần nhân viên, gọi 0909 123 456.";
    }

    private static string ExtractReply(JsonElement root)
    {
        return root.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content)
                ? content.GetString() ?? ""
                : "";
    }
}

public class SupportChatRequest
{
    public string Message { get; set; } = "";
    public List<SupportChatTurn>? History { get; set; }
}

public class SupportChatTurn
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}
