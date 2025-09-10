// Application/Services/QuotaService.cs

using Microsoft.EntityFrameworkCore;
using QuotaApp.Application.DTOs;
using QuotaApp.Application.Settings; // Ayar sınıfı için yeni using ifadesi
using QuotaApp.Domain.Entities;
using QuotaApp.Infrastructure;
using QuotaApp.Presentation.ApiEndpoints;
using System.Linq;

namespace QuotaApp.Application.Services;

public class QuotaService : IQuotaService
{
    private readonly ApplicationDbContext _db;
    private readonly QuotaSettings _quotaSettings; // Sabit sayıların yerini alan ayar nesnesi
    private readonly TimeSpan _istanbulOffset = TimeSpan.FromHours(3);

    // Constructor (Yapıcı Metot), DbContext ile birlikte artık QuotaSettings'i de alıyor.
    public QuotaService(ApplicationDbContext db, QuotaSettings quotaSettings)
    {
        _db = db;
        _quotaSettings = quotaSettings;
    }

    public async Task<UsageInfo> GetUsageAsync(string userId)
    {
        var utcNow = DateTime.UtcNow;
        var (dayStartUtc, _, monthStartUtc, _, dayResetLocal, monthResetLocal) = CalculateTimeWindows(utcNow);
        
        var dailyUsed = await _db.SearchLogs.CountAsync(q => q.UserId == userId && q.CreatedAtUtc >= dayStartUtc);
        var monthlyUsed = await _db.SearchLogs.CountAsync(q => q.UserId == userId && q.CreatedAtUtc >= monthStartUtc);

        return new UsageInfo
        {
            DayUsed = dailyUsed,
            DayRemaining = Math.Max(0, _quotaSettings.DailyLimit - dailyUsed), // Sabit '5' yerine ayar kullanılıyor
            MonthUsed = monthlyUsed,
            MonthRemaining = Math.Max(0, _quotaSettings.MonthlyLimit - monthlyUsed), // Sabit '20' yerine ayar kullanılıyor
            DayResetAtLocal = dayResetLocal,
            MonthResetAtLocal = monthResetLocal
        };
    }

    public async Task<(UsageInfo usage, List<string> results)> TryConsumeAndSearchAsync(string userId, SearchRequestDto request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var currentUsage = await GetUsageAsync(userId);

        if (currentUsage.DayRemaining <= 0)
        {
            throw new QuotaException("DAILY_LIMIT_EXCEEDED", "Günlük limitiniz (5) doldu. Yarın tekrar deneyin.");
        }
            
        if (currentUsage.MonthRemaining <= 0)
        {
            throw new QuotaException("MONTHLY_LIMIT_EXCEEDED", "Aylık toplam hakkınız (20) doldu. Bir sonraki ay tekrar deneyin.");
        }

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
        _db.SearchLogs.Add(searchLog);
        await _db.SaveChangesAsync();
        
        List<string> results;
        var neighbourhoodId = request.NeighbourhoodId;

        if (request.HasStreet && request.StreetId.HasValue && request.StreetId > 0)
        {
            results = await _db.Streets
                .Where(s => s.Id == request.StreetId.Value)
                .Select(s => $"[Cadde Sonucu] {s.Name}")
                .Take(20)
                .ToListAsync();
        }
        else if (request.HasSite && request.SiteId.HasValue && request.SiteId > 0)
        {
             results = await _db.Sites
                .Where(s => s.Id == request.SiteId.Value)
                .Select(s => $"[Site Sonucu] {s.Name}")
                .Take(20)
                .ToListAsync();
        }
        else if (neighbourhoodId.HasValue && neighbourhoodId > 0)
        {
            results = await _db.Streets
                .Where(s => s.NeighbourhoodId == neighbourhoodId)
                .OrderBy(s => s.Name)
                .Select(s => $"[Örnek Cadde] {s.Name}")
                .Take(3)
                .ToListAsync();
        }
        else
        {
            results = new List<string> { "Lütfen daha detaylı bir arama yapınız (örn: mahalle seçiniz)." };
        }
        
        if (!results.Any())
        {
            results.Add("Aramanızla eşleşen sonuç bulunamadı.");
        }

        await transaction.CommitAsync();
        
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