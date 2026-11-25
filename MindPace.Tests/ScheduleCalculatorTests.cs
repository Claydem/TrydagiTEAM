using System;
using System.Collections.Generic;
using MindPace.Models;
using MindPace.Services;
using Xunit;

namespace MindPace.Tests
{
    public class ScheduleCalculatorTests
    {
        [Fact]
        public void CalculateTotalDailyHours_SumsOnlyActiveIncompleteProjects()
        {
            var projects = new List<Project>
            {
                new Project { Title = "Активний 1", TotalEstimatedHours = 10, HoursWorked = 0, Deadline = DateTime.Today.AddDays(5), IsCompleted = false },
                new Project { Title = "Активний 2", TotalEstimatedHours = 6, HoursWorked = 0, Deadline = DateTime.Today.AddDays(3), IsCompleted = false },
                new Project { Title = "Завершений", TotalEstimatedHours = 12, HoursWorked = 0, Deadline = DateTime.Today.AddDays(4), IsCompleted = true }
            };

            double totalDaily = ScheduleCalculator.CalculateTotalDailyHours(projects);

            Assert.Equal(4.0, totalDaily);
        }

        [Fact]
        public void GenerateWeekSchedule_CreatesSevenDaysAndFiltersExpiredDeadlines()
        {
            var projects = new List<Project>
            {
                new Project { Title = "Короткий проект", TotalEstimatedHours = 4, HoursWorked = 0, Deadline = DateTime.Today.AddDays(1) },
                new Project { Title = "Довгий проект", TotalEstimatedHours = 14, HoursWorked = 0, Deadline = DateTime.Today.AddDays(7) }
            };

            var schedule = ScheduleCalculator.GenerateWeekSchedule(projects, DateTime.Today);

            Assert.Equal(7, schedule.Count);
            Assert.Equal(2, schedule[0].Tasks.Count);
            Assert.Equal(2, schedule[1].Tasks.Count);
            Assert.Single(schedule[2].Tasks);
        }

        [Theory]
        [InlineData("", "10", false)]
        [InlineData("   ", "10", false)]
        [InlineData("Проект", "-5", false)]
        [InlineData("Проект", "abc", false)]
        [InlineData("Проект", "12.5", true)]
        [InlineData("Проект", "12,5", true)]
        public void TryValidateProjectInput_ValidatesCorrectly(string title, string hoursText, bool expectedValid)
        {
            bool isValid = ScheduleCalculator.TryValidateProjectInput(
                title,
                hoursText,
                DateTime.Today.AddDays(3),
                out _,
                out _);

            Assert.Equal(expectedValid, isValid);
        }
    }
}
