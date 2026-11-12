using System;
using System.Collections.ObjectModel;
using System.Globalization;
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

            var ukCulture = CultureInfo.GetCultureInfo("uk-UA");
            var rawDate = DateTime.Today.ToString("dddd, d MMMM yyyy", ukCulture);
            lblTodayDate.Text = char.ToUpper(rawDate[0]) + rawDate[1..];

            dpDeadline.SelectedDate = DateTime.Today.AddDays(7);

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            LoadProjects();
            RefreshAllViews();
        }

        private void LoadProjects()
        {
            _projects.Clear();
            var loaded = ProjectStorage.Load();
            foreach (var item in loaded)
            {
                _projects.Add(item);
            }
        }

        private void RefreshAllViews()
        {
            icProjects.ItemsSource = null;
            icProjects.ItemsSource = _projects;

            var activeProjects = _projects.Where(p => !p.IsCompleted && p.RemainingHours > 0).ToList();
            lblProjectCount.Text = $"Всього: {_projects.Count} | Активних: {activeProjects.Count}";

            cbFocusProjects.ItemsSource = null;
            cbFocusProjects.ItemsSource = activeProjects;
            if (activeProjects.Count > 0) cbFocusProjects.SelectedIndex = 0;

            double totalTodayHours = activeProjects.Sum(p => p.RequiredDailyHours);
            lblTotalSchedule.Text = $"Щоб встигнути всі проекти, сьогодні треба працювати: {Project.FormatHours(totalTodayHours)}";

            lstSchedule.ItemsSource = activeProjects.Select(p => new
            {
                DisplayInfo = $"{p.Title} — Дедлайн: {p.Deadline:dd.MM.yyyy} | Темп: {p.DailyTimeFormatted}"
            }).ToList();
        }

        private void BtnAddProject_Click(object sender, RoutedEventArgs e)
        {
            lblProjectError.Visibility = Visibility.Collapsed;

            string title = txtTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                ShowProjectError("Будь ласка, введіть назву проекту.");
                return;
            }

            string hoursRaw = txtTotalHours.Text.Trim().Replace(',', '.');
            if (!double.TryParse(hoursRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out double totalHours) || totalHours <= 0)
            {
                ShowProjectError("Введіть коректну оцінку часу в годинах (більше 0).");
                return;
            }

            DateTime deadline = dpDeadline.SelectedDate ?? DateTime.Today.AddDays(7);
            if (deadline.Date < DateTime.Today)
            {
                ShowProjectError("Дедлайн не може бути у минулому.");
                return;
            }

            var newProject = new Project
            {
                Title = title,
                Description = txtDescription.Text.Trim(),
                TotalEstimatedHours = totalHours,
                HoursWorked = 0,
                Deadline = deadline,
                CreatedAt = DateTime.Now
            };

            _projects.Add(newProject);
            ProjectStorage.Save(_projects);

            txtTitle.Text = string.Empty;
            txtDescription.Text = string.Empty;
            txtTotalHours.Text = "10";
            dpDeadline.SelectedDate = DateTime.Today.AddDays(7);

            RefreshAllViews();
        }

        private void ShowProjectError(string message)
        {
            lblProjectError.Text = message;
            lblProjectError.Visibility = Visibility.Visible;
        }

        private void BtnAddProgress_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Project project)
            {
                project.HoursWorked = Math.Round(project.HoursWorked + 0.5, 2);
                ProjectStorage.Save(_projects);
                RefreshAllViews();
            }
        }

        private void BtnToggleComplete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Project project)
            {
                project.IsCompleted = !project.IsCompleted;
                ProjectStorage.Save(_projects);
                RefreshAllViews();
            }
        }

        private void BtnDeleteProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Project project)
            {
                var result = MessageBox.Show(
                    $"Ви дійсно бажаєте видалити проект «{project.Title}»?",
                    "Підтвердження видалення",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    _projects.Remove(project);
                    ProjectStorage.Save(_projects);
                    RefreshAllViews();
                }
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
                btnTimerStart.Content = "Старт";

                try { SystemSounds.Exclamation.Play(); } catch { }

                if (cbFocusProjects.SelectedItem is Project proj)
                {
                    proj.HoursWorked = Math.Round(proj.HoursWorked + 0.42, 2);
                    ProjectStorage.Save(_projects);
                    RefreshAllViews();
                }

                MessageBox.Show("Сесію фокусу завершено!", "MindPace");
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
                btnTimerStart.Content = "Пауза";
            }
            else
            {
                _timer.Stop();
                _isRunning = false;
                btnTimerStart.Content = "Продовжити";
            }
        }

        private void BtnTimerReset_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            btnTimerStart.Content = "Старт";
            _seconds = 25 * 60;
            lblTimer.Text = "25:00";
        }
    }
}
