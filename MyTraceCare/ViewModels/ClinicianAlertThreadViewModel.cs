using MyTraceCare.Models;

namespace MyTraceCare.Models.ViewModels
{
    public class ClinicianAlertThreadViewModel
    {
        public Alert Alert { get; set; } = null!;
        public List<CommentThreadItemViewModel> Thread { get; set; } = new();
        public string PatientName { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
    }

    public class CommentThreadItemViewModel
    {
        public PatientComment Comment { get; set; } = null!;
        public int Level { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int AlertId { get; set; }
        public List<CommentThreadItemViewModel> Replies { get; set; } = new();
    }
}
