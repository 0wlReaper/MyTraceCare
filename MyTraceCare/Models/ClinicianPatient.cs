namespace MyTraceCare.Models
{
    public class ClinicianPatient
    {
        public int Id { get; set; }

        public string ClinicianId { get; set; } = string.Empty;
        public User? Clinician { get; set; }

        public string PatientId { get; set; } = string.Empty;
        public User? Patient { get; set; }
    }
}
