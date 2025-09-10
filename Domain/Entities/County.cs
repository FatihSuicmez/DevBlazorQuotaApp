namespace QuotaApp.Domain.Entities;

public class County
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int ProvinceId { get; set; }
    public virtual Province Province { get; set; } = null!;
}