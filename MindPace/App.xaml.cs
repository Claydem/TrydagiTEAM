using System.Windows;
using MindPace.Services;

namespace MindPace
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppLogger.Initialize();
            AppLogger.Info("Запуск застосунку MindPace");
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLogger.Info("Завершення роботи застосунку MindPace");
            base.OnExit(e);
        }
    }
}
