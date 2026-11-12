using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MindPace.Models;

namespace MindPace.Services
{
    public static class ProjectStorage
    {
        private static readonly string FilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "projects.json"
        );

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static List<Project> Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return GetInitialSamples();
                }

                string json = File.ReadAllText(FilePath);
                var items = JsonSerializer.Deserialize<List<Project>>(json, JsonOptions);
                return items ?? GetInitialSamples();
            }
            catch
            {
                return GetInitialSamples();
            }
        }

        public static void Save(IEnumerable<Project> projects)
        {
            try
            {
                string json = JsonSerializer.Serialize(projects, JsonOptions);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка збереження: {ex.Message}");
            }
        }

        private static List<Project> GetInitialSamples()
        {
            return new List<Project>
            {
                new Project
                {
                    Title = "Курсова робота",
                    Description = "Розробка архітектури та написання пояснювальної записки",
                    TotalEstimatedHours = 12,
                    HoursWorked = 2,
                    Deadline = DateTime.Today.AddDays(5)
                }
            };
        }
    }
}
