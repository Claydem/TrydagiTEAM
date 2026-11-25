using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MindPace.Models;

namespace MindPace.Services
{
    public static class ScheduleCalculator
    {
        public static List<Project> GetActiveProjects(IEnumerable<Project> projects)
        {
            return projects
                .Where(p => !p.IsCompleted && p.RemainingHours > 0)
                .ToList();
        }

        public static double CalculateTotalDailyHours(IEnumerable<Project> projects)
        {
            return Math.Round(GetActiveProjects(projects).Sum(p => p.RequiredDailyHours), 2);
        }

        public static List<DaySchedule> GenerateWeekSchedule(IEnumerable<Project> projects, DateTime startDate)
        {
            var activeProjects = GetActiveProjects(projects);
            var ukCulture = CultureInfo.GetCultureInfo("uk-UA");
            var daysSchedule = new List<DaySchedule>();

            for (int i = 0; i < 7; i++)
            {
                var date = startDate.Date.AddDays(i);
                string dayTitle = i switch
                {
                    0 => $"Сьогодні ({date:dd.MM})",
                    1 => $"Завтра ({date:dd.MM})",
                    _ => Capitalize(date.ToString("dddd", ukCulture)) + $" ({date:dd.MM})"
                };

                var relevantProjects = activeProjects
                    .Where(p => p.Deadline.Date >= date.Date)
                    .ToList();

                var tasksForDay = relevantProjects
                    .Select(p => new DayTask
                    {
                        ProjectTitle = p.Title,
                        DurationText = Project.FormatHours(p.RequiredDailyHours),
                        DeadlineText = $"до {p.Deadline:dd.MM}"
                    })
                    .ToList();

                double dayTotalHours = relevantProjects.Sum(p => p.RequiredDailyHours);

                string totalText = tasksForDay.Count > 0
                    ? $"Разом: {Project.FormatHours(dayTotalHours)}"
                    : "Немає завдань";

                daysSchedule.Add(new DaySchedule
                {
                    Date = date,
                    DayTitle = dayTitle,
                    TotalTimeFormatted = totalText,
                    Tasks = tasksForDay
                });
            }

            return daysSchedule;
        }

        public static bool TryValidateProjectInput(
            string? title,
            string? hoursText,
            DateTime? deadline,
            out double parsedHours,
            out string errorMessage)
        {
            parsedHours = 0;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                errorMessage = "Будь ласка, введіть назву проекту.";
                return false;
            }

            string normalizedHours = (hoursText ?? string.Empty).Trim().Replace(',', '.');
            if (!double.TryParse(normalizedHours, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedHours) || parsedHours <= 0)
            {
                errorMessage = "Введіть коректну оцінку часу в годинах (більше 0).";
                return false;
            }

            if (deadline.HasValue && deadline.Value.Date < DateTime.Today)
            {
                errorMessage = "Дедлайн не може бути у минулому.";
                return false;
            }

            return true;
        }

        private static string Capitalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input[1..];
        }
    }
}
