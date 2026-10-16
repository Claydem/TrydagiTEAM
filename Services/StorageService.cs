using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MindPace.Models;

namespace MindPace.Services
{
    public static class StorageService
    {
        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "projects.json");

        private static readonly JsonSerializerOptions Options = new()
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
                    return GetDefaultSamples();
                }

                string json = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<List<Project>>(json, Options);
                return data ?? GetDefaultSamples();
            }
            catch
            {
                return GetDefaultSamples();
            }
        }

        public static void Save(IEnumerable<Project> projects)
        {
            try
            {
                string json = JsonSerializer.Serialize(projects, Options);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка запису: {ex.Message}");
            }
        }

        private static List<Project> GetDefaultSamples()
        {
            return new List<Project>
            {
                new Project
                {
                    Title = "Курсова робота",
                    Description = "Розробка модулів системи та оформлення звіту",
                    TotalEstimatedHours = 12,
                    HoursWorked = 2,
                    Deadline = DateTime.Today.AddDays(5)
                }
            };
        }
    }
}
