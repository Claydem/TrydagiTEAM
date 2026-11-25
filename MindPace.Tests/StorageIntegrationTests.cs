using System;
using System.Collections.Generic;
using MindPace.Models;
using MindPace.Services;
using Xunit;

namespace MindPace.Tests
{
    public class StorageIntegrationTests
    {
        [Fact]
        public void ProjectStorage_SaveAndLoad_PreservesAllProjectProperties()
        {
            var testId = Guid.NewGuid();
            var deadline = DateTime.Today.AddDays(6);

            var listToSave = new List<Project>
            {
                new Project
                {
                    Id = testId,
                    Title = "Інтеграційний тест збереження",
                    Description = "Перевірка серіалізації у файл projects.json",
                    TotalEstimatedHours = 14.5,
                    HoursWorked = 3.25,
                    Deadline = deadline,
                    IsCompleted = false
                }
            };

            ProjectStorage.Save(listToSave);
            var loadedList = ProjectStorage.Load();

            Assert.NotNull(loadedList);
            var loadedProject = loadedList.Find(p => p.Id == testId);
            Assert.NotNull(loadedProject);
            Assert.Equal("Інтеграційний тест збереження", loadedProject.Title);
            Assert.Equal("Перевірка серіалізації у файл projects.json", loadedProject.Description);
            Assert.Equal(14.5, loadedProject.TotalEstimatedHours);
            Assert.Equal(3.25, loadedProject.HoursWorked);
            Assert.Equal(deadline.Date, loadedProject.Deadline.Date);
            Assert.False(loadedProject.IsCompleted);
        }
    }
}
