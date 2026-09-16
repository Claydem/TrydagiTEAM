using System;
using System.Windows;
using System.Windows.Threading;

namespace MindPace
{
    public partial class MainWindow : Window
    {
        // Таймер для зворотного відліку
        private readonly DispatcherTimer _timer = new();
        // 25 хвилин у секундах
        private int _seconds = 25 * 60;
        private bool _isRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            dpDeadline.SelectedDate = DateTime.Today.AddDays(7);

            // Налаштовуємо таймер на спрацьовування щосекунди
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        // Обробник кожної секунди таймера
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_seconds > 0)
            {
                _seconds--;
                int m = _seconds / 60;
                int s = _seconds % 60;
                lblTimer.Text = $"{m:D2}:{s:D2}";
            }
            else
            {
                _timer.Stop();
                _isRunning = false;
            }
        }

        // Кнопка запуску відліку
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                _timer.Start();
                _isRunning = true;
            }
        }

        // Кнопка паузи таймера
        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                _timer.Stop();
                _isRunning = false;
            }
        }

        // Обробник додавання проекту до списку
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                lstProjects.Items.Add(txtTitle.Text.Trim());
                txtTitle.Text = string.Empty;
                txtDesc.Text = string.Empty;
            }
        }
    }
}