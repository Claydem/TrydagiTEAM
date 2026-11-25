using System;
using System.IO;
using MindPace.Services;
using Xunit;

namespace MindPace.Tests
{
    public class ErrorHandlingTests
    {
        [Fact]
        public void LoadFromFile_CorruptedJson_CreatesBackupAndReturnsFallback()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"mindpace-err-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            string filePath = Path.Combine(tempDir, "projects.json");

            try
            {
                File.WriteAllText(filePath, "{ broken json content !!!");

                var loaded = ProjectStorage.LoadFromFile(filePath);

                Assert.NotNull(loaded);
                Assert.NotEmpty(loaded);
                Assert.True(File.Exists(filePath + ".bak"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        [Fact]
        public void LoadFromFile_MissingOrEmptyPath_ReturnsFallbackWithoutException()
        {
            var fromEmpty = ProjectStorage.LoadFromFile("");
            var fromMissing = ProjectStorage.LoadFromFile(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"));

            Assert.NotEmpty(fromEmpty);
            Assert.NotEmpty(fromMissing);
        }

        [Fact]
        public void SaveToFile_EmptyPath_ReturnsFalseSafely()
        {
            bool saved = ProjectStorage.SaveToFile(Array.Empty<MindPace.Models.Project>(), "");
            Assert.False(saved);
        }
    }
}
