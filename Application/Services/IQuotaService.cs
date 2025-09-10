// Application/IQuotaService.cs

using QuotaApp.Application.DTOs;
using QuotaApp.Presentation.ApiEndpoints; // SearchRequestDto için

namespace QuotaApp.Application; // DÜZELTME: Doğru namespace

public interface IQuotaService
{
    Task<UsageInfo> GetUsageAsync(string userId);
    
    // DÜZELTME: Metodu yeni arama modeline göre güncelliyoruz
    Task<(UsageInfo usage, List<string> results)> TryConsumeAndSearchAsync(string userId, SearchRequestDto request);
}