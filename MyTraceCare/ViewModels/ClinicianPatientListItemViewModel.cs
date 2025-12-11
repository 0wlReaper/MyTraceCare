using MyTraceCare.Models;

namespace MyTraceCare.ViewModels
{
    public class ClinicianPatientListItemViewModel
    {
        public string PatientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public DateTime? LastDataDate { get; set; }
    }
}
