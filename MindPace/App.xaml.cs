using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MindPace.Services;

namespace MindPace
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppLogger.Initialize();
            AppLogger.Info("Запуск застосунку MindPace");

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLogger.Info("Завершення роботи застосунку MindPace");
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppLogger.Error("Необроблена помилка інтерфейсу", e.Exception);
            MessageBox.Show(
                "Сталася неочікувана помилка. Деталі записано в журнал роботи програми.",
                "MindPace — Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                AppLogger.Error("Критична помилка домену застосунку", ex);
            }
            else
            {
                AppLogger.Error("Критична невідома помилка домену застосунку");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            AppLogger.Error("Помилка у фоновій задачі", e.Exception);
            e.SetObserved();
        }
    }
}
