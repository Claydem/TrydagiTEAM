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
            return LoadFromFile(FilePath);
        }

        public static List<Project> LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                AppLogger.Warn("Передано порожній шлях до файлу збереження");
                return GetInitialSamples();
            }

            try
            {
                if (!File.Exists(path))
                {
                    AppLogger.Info("Файл projects.json не знайдено, завантажено початковий приклад");
                    return GetInitialSamples();
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    AppLogger.Warn("Файл projects.json порожній, використано початкові дані");
                    return GetInitialSamples();
                }

                var items = JsonSerializer.Deserialize<List<Project>>(json, JsonOptions);
                var result = items ?? GetInitialSamples();
                AppLogger.Info($"Завантажено проектів із файлу: {result.Count}");
                return result;
            }
            catch (JsonException ex)
            {
                AppLogger.Error("Пошкоджений формат JSON у файлі проектів", ex);
                TryCreateBackup(path);
                return GetInitialSamples();
            }
            catch (IOException ex)
            {
                AppLogger.Error("Помилка читання файлу projects.json", ex);
                return GetInitialSamples();
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.Error("Недостатньо прав доступу для читання projects.json", ex);
                return GetInitialSamples();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Неочікувана помилка при завантаженні projects.json", ex);
                return GetInitialSamples();
            }
        }

        public static void Save(IEnumerable<Project> projects)
        {
            SaveToFile(projects, FilePath);
        }

        public static bool SaveToFile(IEnumerable<Project> projects, string path)
        {
            if (projects == null || string.IsNullOrWhiteSpace(path))
            {
                AppLogger.Warn("Спроба збереження порожніх даних або некоректного шляху");
                return false;
            }

            string tempPath = path + ".tmp";

            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(projects, JsonOptions);
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, path, overwrite: true);

                AppLogger.Info("Список проектів збережено у projects.json");
                return true;
            }
            catch (IOException ex)
            {
                AppLogger.Error("Помилка запису у файл projects.json", ex);
                CleanupTempFile(tempPath);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.Error("Немає прав на запис файлу projects.json", ex);
                CleanupTempFile(tempPath);
                return false;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Неочікувана помилка збереження projects.json", ex);
                CleanupTempFile(tempPath);
                return false;
            }
        }

        private static void TryCreateBackup(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string backupPath = path + ".bak";
                    File.Copy(path, backupPath, overwrite: true);
                    AppLogger.Warn($"Створено резервну копію пошкодженого файлу: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Не вдалося створити резервну копію пошкодженого файлу", ex);
            }
        }

        private static void CleanupTempFile(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch { }
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
