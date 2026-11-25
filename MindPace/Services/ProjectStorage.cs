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
                    AppLogger.Info("Файл projects.json не знайдено, завантажено початковий приклад");
                    return GetInitialSamples();
                }

                string json = File.ReadAllText(FilePath);
                var items = JsonSerializer.Deserialize<List<Project>>(json, JsonOptions);
                var result = items ?? GetInitialSamples();
                AppLogger.Info($"Завантажено проектів із файлу: {result.Count}");
                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Помилка при завантаженні projects.json", ex);
                return GetInitialSamples();
            }
        }

        public static void Save(IEnumerable<Project> projects)
        {
            try
            {
                string json = JsonSerializer.Serialize(projects, JsonOptions);
                File.WriteAllText(FilePath, json);
                AppLogger.Info("Список проектів збережено у projects.json");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Помилка збереження projects.json", ex);
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
