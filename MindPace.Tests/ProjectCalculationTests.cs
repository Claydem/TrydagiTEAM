using System;
using MindPace.Models;
using Xunit;

namespace MindPace.Tests
{
    public class ProjectCalculationTests
    {
        [Fact]
        public void RemainingHours_CalculatesDifferenceBetweenEstimatedAndWorked()
        {
            var project = new Project
            {
                TotalEstimatedHours = 20,
                HoursWorked = 5.5
            };

            Assert.Equal(14.5, project.RemainingHours);
        }

        [Fact]
        public void DaysRemaining_ReturnsExactDaysUntilFutureDeadline()
        {
            var project = new Project
            {
                Deadline = DateTime.Today.AddDays(5)
            };

            Assert.Equal(5, project.DaysRemaining);
        }

        [Fact]
        public void RequiredDailyHours_DividesRemainingHoursByDaysRemaining()
        {
            var project = new Project
            {
                TotalEstimatedHours = 15,
                HoursWorked = 5,
                Deadline = DateTime.Today.AddDays(5)
            };

            Assert.Equal(2.0, project.RequiredDailyHours);
        }

        [Theory]
        [InlineData(2.5, "2 год 30 хв")]
        [InlineData(1.0, "1 год")]
        [InlineData(0.5, "30 хв")]
        [InlineData(0.25, "15 хв")]
        public void FormatHours_FormatsStandardDurationsCorrectly(double hours, string expected)
        {
            Assert.Equal(expected, Project.FormatHours(hours));
        }
    }
}
