using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;

namespace ToyStoreCore.Web.Services;

/// <summary>
/// Gợi ý sản phẩm dựa trên thuật toán Apriori (phiên bản rút gọn). Khai thác bảng
/// UserActivityLog - mỗi "giao dịch" = tập sản phẩm khách quan tâm trong cùng một ngày.
/// Ported từ ToyStore cũ (Services/RecommendationService.cs).
/// </summary>
public class RecommendationService
{
    /// <summary>Ngưỡng hỗ trợ tối thiểu: cặp (currentProduct, B) phải xuất hiện chung ít nhất N giao dịch.</summary>
    private const int MinSupportCount = 2;

    private static readonly string[] RelevantActivityTypes = { "ViewProduct", "AddToCart" };

    private readonly AppDbContext _db;

    public RecommendationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<int>> GetRecommendedProductIdsAsync(int currentProductId, int limit = 4)
    {
        if (currentProductId <= 0 || limit <= 0)
        {
            return new List<int>();
        }

        // Bước 1: chuẩn bị tập giao dịch từ UserActivityLog.
        var transactions = await BuildTransactionsAsync();
        if (transactions.Count == 0)
        {
            return new List<int>();
        }

        // Bước 2: Apriori rút gọn - chỉ sinh tập phổ biến kích thước 2 (cặp sản phẩm).
        var transactionsContainingCurrent = transactions.Where(t => t.Contains(currentProductId)).ToList();
        if (transactionsContainingCurrent.Count == 0)
        {
            return new List<int>();
        }

        var supportA = transactionsContainingCurrent.Count;
        var pairSupportCounts = new Dictionary<int, int>();

        foreach (var transaction in transactionsContainingCurrent)
        {
            foreach (var productId in transaction)
            {
                if (productId == currentProductId)
                {
                    continue;
                }

                pairSupportCounts.TryGetValue(productId, out var count);
                pairSupportCounts[productId] = count + 1;
            }
        }

        return pairSupportCounts
            .Where(kv => kv.Value >= MinSupportCount)
            .Select(kv => new
            {
                ProductId = kv.Key,
                SupportCount = kv.Value,
                Confidence = (double)kv.Value / supportA,
            })
            .OrderByDescending(x => x.Confidence)
            .ThenByDescending(x => x.SupportCount)
            .Take(limit)
            .Select(x => x.ProductId)
            .ToList();
    }

    private async Task<List<HashSet<int>>> BuildTransactionsAsync()
    {
        var rawLogs = await _db.UserActivityLogs
            .AsNoTracking()
            .Where(log => RelevantActivityTypes.Contains(log.ActivityType) && log.ProductId != null && log.ProductId > 0)
            .Select(log => new { log.UserId, ProductId = log.ProductId!.Value, SessionDate = log.Timestamp.Date })
            .ToListAsync();

        // Gom theo (UserId, Ngày) -> một "phiên duyệt web".
        return rawLogs
            .GroupBy(x => new { x.UserId, x.SessionDate })
            .Select(group => group.Select(e => e.ProductId).ToHashSet())
            .Where(set => set.Count > 0)
            .ToList();
    }
}
