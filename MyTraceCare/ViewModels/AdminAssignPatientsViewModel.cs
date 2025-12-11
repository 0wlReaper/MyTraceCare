namespace MyTraceCare.Models.ViewModels
{
    public class AdminAssignPatientsViewModel
    {
        public List<User> Clinicians { get; set; } = new();
        public List<User> Patients { get; set; } = new();
        public List<ClinicianPatient> Assignments { get; set; } = new();
    }
}
