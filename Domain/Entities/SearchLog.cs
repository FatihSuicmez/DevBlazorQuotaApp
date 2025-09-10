using System.ComponentModel.DataAnnotations.Schema;

namespace QuotaApp.Domain.Entities;

public class SearchLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    
    // Aramanın yapıldığı coğrafi bilgiler
    public int ProvinceId { get; set; }
    public int CountyId { get; set; }
    public int? NeighbourhoodId { get; set; } // Mahalle seçimi opsiyonel olabilir
    
    // Arama tipi ve detayları
    public bool HasStreet { get; set; }
    public int? StreetId { get; set; }
    
    public bool HasSite { get; set; }
    public int? SiteId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;
}