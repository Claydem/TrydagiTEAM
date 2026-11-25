using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;

namespace MindPace.Services
{
    public static class AppLogger
    {
        private static readonly object SyncRoot = new();
        private static bool _initialized;
        private static ILog _log = LogManager.GetLogger(typeof(AppLogger));

        public static string LogFilePath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "logs",
            "mindpace.log"
        );

        public static void Initialize()
        {
            if (_initialized) return;

            lock (SyncRoot)
            {
                if (_initialized) return;

                string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logsDir);
                GlobalContext.Properties["LogFilePath"] = LogFilePath;

                var repo = LogManager.GetRepository(Assembly.GetExecutingAssembly());
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");

                if (File.Exists(configPath))
                {
                    XmlConfigurator.Configure(repo, new FileInfo(configPath));
                }

                _log = LogManager.GetLogger(repo.Name, "MindPace");
                _initialized = true;
            }
        }

        public static void Info(string message)
        {
            Initialize();
            _log.Info(message);
        }

        public static void Warn(string message)
        {
            Initialize();
            _log.Warn(message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            Initialize();
            if (ex != null)
            {
                _log.Error(message, ex);
            }
            else
            {
                _log.Error(message);
            }
        }
    }
}
