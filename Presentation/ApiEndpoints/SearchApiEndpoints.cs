// Presentation/ApiEndpoints/SearchApiEndpoints.cs

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc; // [FromQuery], [FromBody], [FromServices] için gereklidir.
using QuotaApp.Application;
using QuotaApp.Application.DTOs;
using QuotaApp.Application.Settings;
using QuotaApp.Infrastructure;

namespace QuotaApp.Presentation.ApiEndpoints;

public class SearchRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir il seçiniz.")]
    public int ProvinceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir ilçe seçiniz.")]
    public int CountyId { get; set; }

    public int? NeighbourhoodId { get; set; }

    public bool HasStreet { get; set; }

    [RequiredIf(nameof(HasStreet), ErrorMessage = "Lütfen bir cadde seçiniz.")]
    public int? StreetId { get; set; }

    public bool HasSite { get; set; }

    [RequiredIf(nameof(HasSite), ErrorMessage = "Lütfen bir site seçiniz.")]
    public int? SiteId { get; set; }
}

public class RequiredIfAttribute : ValidationAttribute
{
    private readonly string _propertyName;

    public RequiredIfAttribute(string propertyName)
    {
        _propertyName = propertyName;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(_propertyName);
        if (property == null)
        {
            return new ValidationResult($"Bilinmeyen özellik: {_propertyName}");
        }

        var propertyValue = (bool?)property.GetValue(validationContext.ObjectInstance);

        if (propertyValue == true && (value == null || (value is int intValue && intValue <= 0)))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}


public static class SearchApiEndpoints
{
    public static void MapSearchApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        group.MapGet("/usage", async (
            ClaimsPrincipal user,
            [FromServices] IQuotaService quotaService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var usage = await quotaService.GetUsageAsync(userId);
            return Results.Ok(usage);
        });

        group.MapPost("/search", async (
            [FromBody] SearchRequestDto request,
            ClaimsPrincipal user,
            HttpContext context,
            [FromServices] IQuotaService quotaService,
            [FromServices] QuotaSettings quotaSettings) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var (usageInfo, searchResults) = await quotaService.TryConsumeAndSearchAsync(userId, request);

                context.Response.Headers.Append("X-RateLimit-Limit-Day", quotaSettings.DailyLimit.ToString());
                context.Response.Headers.Append("X-RateLimit-Remaining-Day", usageInfo.DayRemaining.ToString());
                context.Response.Headers.Append("X-RateLimit-Limit-Month", quotaSettings.MonthlyLimit.ToString());
                context.Response.Headers.Append("X-RateLimit-Remaining-Month", usageInfo.MonthRemaining.ToString());

                var response = new
                {
                    items = searchResults,
                    usage = usageInfo
                };

                return Results.Ok(response);
            }
            catch (QuotaException ex)
            {
                var errorResponse = new { code = ex.Code, message = ex.Message };
                return Results.Json(errorResponse, statusCode: StatusCodes.Status429TooManyRequests);
            }
        });

        group.MapGet("/lookups/provinces", async ([FromServices] ApplicationDbContext db) =>
        {
            var provinces = await db.Provinces
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();
            return Results.Ok(provinces);
        });

        group.MapGet("/lookups/counties", async (
            [FromQuery] int provinceId,
            [FromServices] ApplicationDbContext db) =>
        {
            if (provinceId <= 0)
            {
                return Results.BadRequest("Geçerli bir il ID'si gereklidir.");
            }

            var provinceExists = await db.Provinces.AnyAsync(p => p.Id == provinceId);
            if (!provinceExists)
            {
                return Results.NotFound("Belirtilen ID'ye sahip bir il bulunamadı.");
            }

            var counties = await db.Counties
                .Where(c => c.ProvinceId == provinceId)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            return Results.Ok(counties);
        });

        group.MapGet("/lookups/neighbourhoods", async (
            [FromQuery] int countyId,
            [FromServices] ApplicationDbContext db) =>
        {
            if (countyId <= 0)
            {
                return Results.BadRequest("Geçerli bir ilçe ID'si gereklidir.");
            }
            var neighbourhoods = await db.Neighbourhoods
                .Where(n => n.CountyId == countyId)
                .OrderBy(n => n.Name)
                .Select(n => new { n.Id, n.Name })
                .ToListAsync();
            return Results.Ok(neighbourhoods);
        });

        group.MapGet("/lookups/streets", async (
            [FromQuery] int neighbourhoodId,
            [FromServices] ApplicationDbContext db) =>
        {
            if (neighbourhoodId <= 0)
            {
                return Results.BadRequest("Geçerli bir mahalle ID'si gereklidir.");
            }
            var streets = await db.Streets
                .Where(s => s.NeighbourhoodId == neighbourhoodId)
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();
            return Results.Ok(streets);
        });

        group.MapGet("/lookups/sites", async (
            [FromQuery] int neighbourhoodId,
            [FromServices] ApplicationDbContext db) =>
        {
            if (neighbourhoodId <= 0)
            {
                return Results.BadRequest("Geçerli bir mahalle ID'si gereklidir.");
            }
            var sites = await db.Sites
                .Where(s => s.NeighbourhoodId == neighbourhoodId)
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();
            return Results.Ok(sites);
        });
    }
}