using System;
using System.Collections.Generic;

namespace MindPace.Models
{
    public class DayTask
    {
        public string ProjectTitle { get; set; } = string.Empty;
        public string DurationText { get; set; } = string.Empty;
        public string DeadlineText { get; set; } = string.Empty;
    }

    public class DaySchedule
    {
        public DateTime Date { get; set; }
        public string DayTitle { get; set; } = string.Empty;
        public string TotalTimeFormatted { get; set; } = string.Empty;
        public List<DayTask> Tasks { get; set; } = new();
    }
}
