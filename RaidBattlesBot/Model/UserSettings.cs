namespace RaidBattlesBot.Model
{ 
  public class UserSettings
  {
    public int UserId { get; set; }
    public string TimeZoneId { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lon { get; set; }
  }
}