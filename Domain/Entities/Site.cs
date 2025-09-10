namespace QuotaApp.Domain.Entities;

public class Site
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int NeighbourhoodId { get; set; }
    public virtual Neighbourhood Neighbourhood { get; set; } = null!;
}