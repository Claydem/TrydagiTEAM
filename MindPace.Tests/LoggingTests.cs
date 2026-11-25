using System;
using System.IO;
using MindPace.Services;
using Xunit;

namespace MindPace.Tests
{
    public class LoggingTests
    {
        [Fact]
        public void AppLogger_WritesInfoAndErrorEntriesToLogFile()
        {
            string marker = $"test-log-{Guid.NewGuid():N}";

            AppLogger.Info($"Перевірка журналювання {marker}");
            AppLogger.Error($"Тестова помилка {marker}", new InvalidOperationException("demo"));

            Assert.True(File.Exists(AppLogger.LogFilePath));
            string content = File.ReadAllText(AppLogger.LogFilePath);
            Assert.Contains(marker, content);
            Assert.Contains("INFO", content);
            Assert.Contains("ERROR", content);
        }
    }
}
