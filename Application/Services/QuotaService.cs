using Microsoft.EntityFrameworkCore;
using QuotaApp.Application.DTOs;
using QuotaApp.Application.Settings;
using QuotaApp.Domain.Entities;
using QuotaApp.Infrastructure;
using QuotaApp.Presentation.ApiEndpoints;
using System.Linq;

namespace QuotaApp.Application.Services;

public class QuotaService : IQuotaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly QuotaSettings _quotaSettings;
    private readonly TimeSpan _istanbulOffset = TimeSpan.FromHours(3);

    public QuotaService(IDbContextFactory<ApplicationDbContext> dbContextFactory, QuotaSettings quotaSettings)
    {
        _dbContextFactory = dbContextFactory;
        _quotaSettings = quotaSettings;
    }

    // Bu metot, sadece kota bilgisini GÖRÜNTÜLEMEK için kullanılır.
    public async Task<UsageInfo> GetUsageAsync(string userId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var utcNow = DateTime.UtcNow;
        var (dayStartUtc, _, monthStartUtc, _, dayResetLocal, monthResetLocal) = CalculateTimeWindows(utcNow);
        
        var dailyUsed = await db.SearchLogs.CountAsync(q => q.UserId == userId && q.CreatedAtUtc >= dayStartUtc);
        var monthlyUsed = await db.SearchLogs.CountAsync(q => q.UserId == userId && q.CreatedAtUtc >= monthStartUtc);

        return new UsageInfo
        {
            DayUsed = dailyUsed,
            DayRemaining = Math.Max(0, _quotaSettings.DailyLimit - dailyUsed),
            MonthUsed = monthlyUsed,
            MonthRemaining = Math.Max(0, _quotaSettings.MonthlyLimit - monthlyUsed),
            DayResetAtLocal = dayResetLocal,
            MonthResetAtLocal = monthResetLocal
        };
    }

    // Bu metot, sorgulama işleminin tamamını TEK BİR İŞLEM olarak yürütür.
    public async Task<(UsageInfo usage, List<string> results)> TryConsumeAndSearchAsync(string userId, SearchRequestDto request)
    {
        // 1. Tüm operasyon için tek bir DbContext oluştur.
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        
        // 2. Tüm operasyon için tek bir Transaction başlat.
        await using var transaction = await db.Database.BeginTransactionAsync();

        var utcNow = DateTime.UtcNow;
        var (dayStartUtc, _, monthStartUtc, _, _, _) = CalculateTimeWindows(utcNow);
        
        // 3. Kota kontrolünü bu DbContext üzerinden yap.
        var dailyUsed = await db.SearchLogs.CountAsync(q => q.UserId == userId && q.CreatedAtUtc >= dayStartUtc);
        if (dailyUsed >= _quotaSettings.DailyLimit)
        {
            throw new QuotaException("DAILY_LIMIT_EXCEEDED", $"Günlük limitiniz ({_quotaSettings.DailyLimit}) doldu. Yarın tekrar deneyin.");
        }
            
        var monthlyUsed = await db.SearchLogs.CountAsync(q => q.UserId == userId && q.CreatedAtUtc >= monthStartUtc);
        if (monthlyUsed >= _quotaSettings.MonthlyLimit)
        {
            throw new QuotaException("MONTHLY_LIMIT_EXCEEDED", $"Aylık toplam hakkınız ({_quotaSettings.MonthlyLimit}) doldu. Bir sonraki ay tekrar deneyin.");
        }

        // 4. Log kaydını bu DbContext'e ekle.
        var searchLog = new SearchLog
        {
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            ProvinceId = request.ProvinceId,
            CountyId = request.CountyId,
            NeighbourhoodId = request.NeighbourhoodId,
            HasStreet = request.HasStreet,
            StreetId = request.StreetId,
            HasSite = request.HasSite,
            SiteId = request.SiteId
        };
        db.SearchLogs.Add(searchLog);
        
        // 5. Değişiklikleri veritabanına kaydet. Hata burada oluşuyordu.
        await db.SaveChangesAsync();
        
        // 6. Arama sorgusunu bu DbContext üzerinden yap.
        List<string> results;
        if (request.HasStreet && request.StreetId.HasValue && request.StreetId > 0)
        {
            results = await db.Streets
                .Where(s => s.Id == request.StreetId.Value)
                .Select(s => $"[Cadde Sonucu] {s.Name}")
                .Take(20).ToListAsync();
        }
        else if (request.HasSite && request.SiteId.HasValue && request.SiteId > 0)
        {
             results = await db.Sites
                .Where(s => s.Id == request.SiteId.Value)
                .Select(s => $"[Site Sonucu] {s.Name}")
                .Take(20).ToListAsync();
        }
        else if (request.NeighbourhoodId.HasValue && request.NeighbourhoodId > 0)
        {
            results = await db.Streets
                .Where(s => s.NeighbourhoodId == request.NeighbourhoodId)
                .OrderBy(s => s.Name)
                .Select(s => $"[Örnek Cadde] {s.Name}")
                .Take(3).ToListAsync();
        }
        else
        {
            results = new List<string> { "Lütfen daha detaylı bir arama yapınız (örn: mahalle seçiniz)." };
        }
        
        if (!results.Any())
        {
            results.Add("Aramanızla eşleşen sonuç bulunamadı.");
        }

        // 7. Her şey başarılıysa Transaction'ı onayla.
        await transaction.CommitAsync();
        
        // 8. En güncel kota bilgisini almak için ana metodu çağır.
        var finalUsage = await GetUsageAsync(userId);
        return (finalUsage, results);
    }
    
    private (DateTime, DateTime, DateTime, DateTime, DateTime, DateTime) CalculateTimeWindows(DateTime utcNow)
    {
        var localTime = utcNow + _istanbulOffset;
        var dayStartLocal = localTime.Date;
        var monthStartLocal = new DateTime(localTime.Year, localTime.Month, 1);
        var dayResetLocal = dayStartLocal.AddDays(1);
        var monthResetLocal = monthStartLocal.AddMonths(1);
        return (dayStartLocal - _istanbulOffset, dayResetLocal - _istanbulOffset, monthStartLocal - _istanbulOffset, monthResetLocal - _istanbulOffset, dayResetLocal, monthResetLocal);
    }
}