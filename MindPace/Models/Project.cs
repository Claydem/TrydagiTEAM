using System;

namespace MindPace.Models
{
    public class Project
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double TotalEstimatedHours { get; set; } = 10;
        public double HoursWorked { get; set; } = 0;
        public DateTime Deadline { get; set; } = DateTime.Today.AddDays(7);
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsCompleted { get; set; } = false;

        public double RemainingHours => Math.Max(0, Math.Round(TotalEstimatedHours - HoursWorked, 2));

        public int DaysRemaining
        {
            get
            {
                var diff = (Deadline.Date - DateTime.Today).TotalDays;
                if (diff < 0) return 0;
                if (diff < 1) return 1;
                return (int)Math.Ceiling(diff);
            }
        }

        public double RequiredDailyHours
        {
            get
            {
                if (IsCompleted || RemainingHours <= 0) return 0;
                if (DaysRemaining <= 1) return RemainingHours;
                return Math.Round(RemainingHours / DaysRemaining, 2);
            }
        }

        public string DailyTimeFormatted
        {
            get
            {
                if (IsCompleted) return "Виконано ✓";
                if (RemainingHours <= 0) return "Всі години відпрацьовано ✓";
                if (DaysRemaining == 0) return $"Сьогодні дедлайн! ({FormatHours(RemainingHours)})";
                return $"{FormatHours(RequiredDailyHours)} / день";
            }
        }

        public static string FormatHours(double hours)
        {
            if (hours <= 0) return "0 хв";
            int h = (int)hours;
            int m = (int)Math.Round((hours - h) * 60);
            if (m >= 60) { h++; m = 0; }
            if (h > 0 && m > 0) return $"{h} год {m} хв";
            if (h > 0) return $"{h} год";
            return $"{m} хв";
        }
    }
}
