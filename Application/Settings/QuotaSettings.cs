namespace QuotaApp.Application.Settings;

public class QuotaSettings
{
    public const string SectionName = "QuotaSettings"; 
    public int DailyLimit { get; set; }
    public int MonthlyLimit { get; set; }
}