using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MindPace.Models;
using MindPace.Services;

namespace MindPace
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<Project> _projects = new();
        private readonly DispatcherTimer _timer = new();
        private int _seconds = 25 * 60;
        private bool _isRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            dpDeadline.SelectedDate = DateTime.Today.AddDays(7);

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            LoadData();
        }

        private void LoadData()
        {
            _projects.Clear();
            var list = StorageService.Load();
            foreach (var item in list) _projects.Add(item);

            RefreshUI();
        }

        private void RefreshUI()
        {
            icProjects.ItemsSource = null;
            icProjects.ItemsSource = _projects;

            var active = _projects.Where(p => !p.IsCompleted && p.RemainingHours > 0).ToList();
            cbFocusProjects.ItemsSource = null;
            cbFocusProjects.ItemsSource = active;
            if (active.Count > 0) cbFocusProjects.SelectedIndex = 0;

            double totalHoursToday = active.Sum(p => p.RequiredDailyHours);
            lblTotalSchedule.Text = $"Щоб встигнути всі проекти, сьогодні треба працювати: {Project.FormatTime(totalHoursToday)}";

            lstSchedule.ItemsSource = active.Select(p => new
            {
                DisplayInfo = $"• {p.Title} — Дедлайн: {p.Deadline:dd.MM} | Потрібно працювати: {p.DailyTimeFormatted}"
            }).ToList();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Введіть назву проекту!", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(txtHours.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double hours) || hours <= 0)
            {
                hours = 10;
            }

            var project = new Project
            {
                Title = txtTitle.Text.Trim(),
                Description = txtDesc.Text.Trim(),
                TotalEstimatedHours = hours,
                HoursWorked = 0,
                Deadline = dpDeadline.SelectedDate ?? DateTime.Today.AddDays(7)
            };

            _projects.Add(project);
            StorageService.Save(_projects);

            txtTitle.Text = string.Empty;
            txtDesc.Text = string.Empty;
            RefreshUI();
        }

        private void BtnAddProgress_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Project project)
            {
                project.HoursWorked = Math.Round(project.HoursWorked + 0.5, 2);
                StorageService.Save(_projects);
                RefreshUI();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Project project)
            {
                _projects.Remove(project);
                StorageService.Save(_projects);
                RefreshUI();
            }
        }

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
                btnTimerStart.Content = "▶ Старт";

                try { SystemSounds.Exclamation.Play(); } catch { }

                if (cbFocusProjects.SelectedItem is Project proj)
                {
                    proj.HoursWorked = Math.Round(proj.HoursWorked + 0.42, 2);
                    StorageService.Save(_projects);
                    RefreshUI();
                }

                MessageBox.Show("Сесію фокусу 25 хвилин завершено! Час відпочити ☕", "MindPace");
                _seconds = 25 * 60;
                lblTimer.Text = "25:00";
            }
        }

        private void BtnTimerStart_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                _timer.Start();
                _isRunning = true;
                btnTimerStart.Content = "⏸ Пауза";
            }
            else
            {
                _timer.Stop();
                _isRunning = false;
                btnTimerStart.Content = "▶ Продовжити";
            }
        }

        private void BtnTimerReset_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            btnTimerStart.Content = "▶ Старт";
            _seconds = 25 * 60;
            lblTimer.Text = "25:00";
        }
    }
}
