using System.ComponentModel.DataAnnotations;

namespace bumevent.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        public string EventTitle { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public string EventPlace { get; set; }

        [Required]
        public string CoordinatorName { get; set; }

        [Required]
        public int StudentCount { get; set; }

        [Required]
        public string DepartmentName { get; set; }

        public string Objective { get; set; }

        public string ImagePath { get; set; }
    }
}
