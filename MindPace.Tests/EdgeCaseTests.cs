using System;
using MindPace.Models;
using Xunit;

namespace MindPace.Tests
{
    public class EdgeCaseTests
    {
        [Fact]
        public void RemainingHours_WhenHoursWorkedExceedsTotal_ReturnsZeroInsteadOfNegative()
        {
            var project = new Project
            {
                TotalEstimatedHours = 5,
                HoursWorked = 8.5
            };

            Assert.Equal(0, project.RemainingHours);
            Assert.Equal(0, project.RequiredDailyHours);
        }

        [Fact]
        public void DeadlineToday_DoesNotDivideByZero_ReturnsFullRemainingHours()
        {
            var project = new Project
            {
                TotalEstimatedHours = 6,
                HoursWorked = 2,
                Deadline = DateTime.Today
            };

            Assert.Equal(1, project.DaysRemaining);
            Assert.Equal(4.0, project.RequiredDailyHours);
        }

        [Fact]
        public void PastDeadline_ReturnsZeroDaysRemainingAndFullRemainingHours()
        {
            var project = new Project
            {
                TotalEstimatedHours = 10,
                HoursWorked = 3,
                Deadline = DateTime.Today.AddDays(-3)
            };

            Assert.Equal(0, project.DaysRemaining);
            Assert.Equal(7.0, project.RequiredDailyHours);
            Assert.Equal("Протерміновано", project.StatusSummary);
        }

        [Fact]
        public void CompletedProject_AlwaysReturnsZeroRequiredDailyHours()
        {
            var project = new Project
            {
                TotalEstimatedHours = 20,
                HoursWorked = 4,
                Deadline = DateTime.Today.AddDays(4),
                IsCompleted = true
            };

            Assert.Equal(0, project.RequiredDailyHours);
            Assert.Equal("Виконано ✓", project.DailyTimeFormatted);
            Assert.Equal("Завершено ✓", project.StatusSummary);
        }

        [Theory]
        [InlineData(0.0, "0 хв")]
        [InlineData(-2.5, "0 хв")]
        [InlineData(1.999, "2 год")]
        public void FormatHours_HandlesBoundaryAndNegativeValues(double hours, string expected)
        {
            Assert.Equal(expected, Project.FormatHours(hours));
        }
    }
}
