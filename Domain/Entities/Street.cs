namespace QuotaApp.Domain.Entities;

public class Street
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int NeighbourhoodId { get; set; }
    public virtual Neighbourhood Neighbourhood { get; set; } = null!;
}