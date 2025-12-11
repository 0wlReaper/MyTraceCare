using MyTraceCare.Models;

public class PatientDevice
{
    public int Id { get; set; }

    public string? UserId { get; set; }      // Identity User
    public User? User { get; set; }

    public string DevicePatientId { get; set; } = string.Empty;  // e.g., "1c0fd777"

    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}
