using System.Collections.Generic;
using MyTraceCare.Models;

namespace MyTraceCare.ViewModels
{
    public class AdminAssignmentViewModel
    {
        public List<User> Clinicians { get; set; } = new();
        public List<User> Patients { get; set; } = new();
        public List<ClinicianPatient> Assignments { get; set; } = new();
    }
}
