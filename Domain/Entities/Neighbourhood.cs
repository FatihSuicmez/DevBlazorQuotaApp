namespace QuotaApp.Domain.Entities;

public class Neighbourhood
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int CountyId { get; set; }
    public virtual County County { get; set; } = null!;
}