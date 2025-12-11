namespace MyTraceCare.Models.ViewModels
{
    public class ClinicianPatientViewModel
    {
        public string PatientId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public int AlertCount { get; set; }
    }
}
