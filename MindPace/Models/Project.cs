using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MindPace.Models
{
    public class Project : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private string _title = string.Empty;
        private string _description = string.Empty;
        private double _totalEstimatedHours;
        private double _hoursWorked;
        private DateTime _deadline = DateTime.Today.AddDays(7);
        private DateTime _createdAt = DateTime.Now;
        private bool _isCompleted;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyCalculatedProperties()
        {
            OnPropertyChanged(nameof(RemainingHours));
            OnPropertyChanged(nameof(DaysRemaining));
            OnPropertyChanged(nameof(RequiredDailyHours));
            OnPropertyChanged(nameof(DailyTimeFormatted));
            OnPropertyChanged(nameof(StatusSummary));
            OnPropertyChanged(nameof(CompletionButtonText));
        }

        public Guid Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public double TotalEstimatedHours
        {
            get => _totalEstimatedHours;
            set
            {
                _totalEstimatedHours = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        public double HoursWorked
        {
            get => _hoursWorked;
            set
            {
                _hoursWorked = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        public DateTime Deadline
        {
            get => _deadline;
            set
            {
                _deadline = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
                NotifyCalculatedProperties();
            }
        }

        [JsonIgnore]
        public double RemainingHours => Math.Max(0, Math.Round(TotalEstimatedHours - HoursWorked, 2));

        [JsonIgnore]
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

        [JsonIgnore]
        public double RequiredDailyHours
        {
            get
            {
                if (IsCompleted || RemainingHours <= 0)
                    return 0;

                if (DaysRemaining <= 1)
                    return RemainingHours;

                return Math.Round(RemainingHours / DaysRemaining, 2);
            }
        }

        [JsonIgnore]
        public string DailyTimeFormatted
        {
            get
            {
                if (IsCompleted) return "Виконано ✓";
                if (RemainingHours <= 0) return "Всі години відпрацьовано ✓";
                if (DaysRemaining == 0) return $"Сьогодні дедлайн! ({FormatHours(RemainingHours)})";

                var daily = RequiredDailyHours;
                return $"{FormatHours(daily)} / день";
            }
        }

        [JsonIgnore]
        public string StatusSummary
        {
            get
            {
                if (IsCompleted) return "Завершено ✓";
                if (Deadline.Date < DateTime.Today) return "Протерміновано";
                if (Deadline.Date == DateTime.Today && RemainingHours > 0) return "Дедлайн сьогодні!";
                return $"Залишилось {DaysRemaining} дн.";
            }
        }

        [JsonIgnore]
        public string CompletionButtonText => IsCompleted ? "Відновити" : "✓ Завершити";

        public static string FormatHours(double hours)
        {
            if (hours <= 0) return "0 хв";
            int h = (int)hours;
            int m = (int)Math.Round((hours - h) * 60);
            if (m >= 60)
            {
                h += 1;
                m = 0;
            }

            if (h > 0 && m > 0) return $"{h} год {m} хв";
            if (h > 0) return $"{h} год";
            return $"{m} хв";
        }
    }
}
