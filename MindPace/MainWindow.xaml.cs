using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MindPace.Models;
using MindPace.Services;

namespace MindPace
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<Project> _projects = new();
        private readonly DispatcherTimer _timer = new();

        private int _remainingSeconds = 25 * 60;
        private bool _isRunning = false;
        private bool _isWorkSession = true;
        private int _workMinutes = 25;
        private int _breakMinutes = 5;
        private int _completedSessions = 0;
        private int _totalFocusedMinutes = 0;

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

            var activeProjects = ScheduleCalculator.GetActiveProjects(_projects);
            lblProjectCount.Text = $"Всього: {_projects.Count} | Активних: {activeProjects.Count}";

            var preservedId = (cbFocusProject.SelectedItem as Project)?.Id;
            var focusList = new List<Project>
            {
                new Project { Id = Guid.Empty, Title = "(Без прив'язки до проекту)" }
            };
            focusList.AddRange(activeProjects);
            cbFocusProject.ItemsSource = focusList;

            if (preservedId.HasValue && focusList.Any(p => p.Id == preservedId.Value))
            {
                cbFocusProject.SelectedItem = focusList.First(p => p.Id == preservedId.Value);
            }
            else
            {
                cbFocusProject.SelectedIndex = 0;
            }

            RefreshScheduleTab(activeProjects);
        }

        private void RefreshScheduleTab(List<Project>? activeProjects = null)
        {
            activeProjects ??= ScheduleCalculator.GetActiveProjects(_projects);

            double totalTodayHours = ScheduleCalculator.CalculateTotalDailyHours(activeProjects);
            lblTotalTodayHours.Text = Project.FormatHours(totalTodayHours);

            icProjectDailyRates.ItemsSource = null;
            icProjectDailyRates.ItemsSource = activeProjects;

            var daysSchedule = ScheduleCalculator.GenerateWeekSchedule(activeProjects, DateTime.Today);
            icDaysSchedule.ItemsSource = null;
            icDaysSchedule.ItemsSource = daysSchedule;
        }

        private void BtnAddProject_Click(object sender, RoutedEventArgs e)
        {
            lblProjectError.Visibility = Visibility.Collapsed;

            DateTime deadline = dpDeadline.SelectedDate ?? DateTime.Today.AddDays(7);
            if (!ScheduleCalculator.TryValidateProjectInput(
                txtTitle.Text,
                txtTotalHours.Text,
                deadline,
                out double totalHours,
                out string errorMessage))
            {
                ShowProjectError(errorMessage);
                return;
            }

            var newProject = new Project
            {
                Title = txtTitle.Text.Trim(),
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

        private void BtnRefreshSchedule_Click(object sender, RoutedEventArgs e)
        {
            RefreshAllViews();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateTimerDisplay();
            }
            else
            {
                _timer.Stop();
                _isRunning = false;
                btnTimerStartPause.Content = "▶ Почати";

                try
                {
                    SystemSounds.Exclamation.Play();
                }
                catch { }

                if (_isWorkSession)
                {
                    _completedSessions++;
                    _totalFocusedMinutes += _workMinutes;

                    if (chkAutoLogProgress.IsChecked == true &&
                        cbFocusProject.SelectedItem is Project proj &&
                        proj.Id != Guid.Empty)
                    {
                        var target = _projects.FirstOrDefault(p => p.Id == proj.Id);
                        if (target != null)
                        {
                            target.HoursWorked = Math.Round(target.HoursWorked + (_workMinutes / 60.0), 2);
                            ProjectStorage.Save(_projects);
                            RefreshAllViews();
                        }
                    }

                    _isWorkSession = false;
                    _remainingSeconds = _breakMinutes * 60;
                    SetTimerModeUI(isWork: false);

                    MessageBox.Show(
                        "Сесію фокусу завершено! Відпочиньте ☕",
                        "MindPace — Фокус уваги",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    _isWorkSession = true;
                    _remainingSeconds = _workMinutes * 60;
                    SetTimerModeUI(isWork: true);

                    MessageBox.Show(
                        "Перерву завершено! Готові до наступного блоку роботи? 🔥",
                        "MindPace — Фокус уваги",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                UpdateTimerDisplay();
                UpdateStatsDisplay();
            }
        }

        private void BtnTimerStartPause_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                _timer.Start();
                _isRunning = true;
                btnTimerStartPause.Content = "⏸ Пауза";
                btnTimerStartPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
            }
            else
            {
                _timer.Stop();
                _isRunning = false;
                btnTimerStartPause.Content = "▶ Продовжити";
                btnTimerStartPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
            }
        }

        private void BtnTimerReset_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            btnTimerStartPause.Content = "▶ Почати";
            btnTimerStartPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));

            _remainingSeconds = (_isWorkSession ? _workMinutes : _breakMinutes) * 60;
            UpdateTimerDisplay();
        }

        private void BtnTimerSkip_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            btnTimerStartPause.Content = "▶ Почати";
            btnTimerStartPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));

            _isWorkSession = !_isWorkSession;
            _remainingSeconds = (_isWorkSession ? _workMinutes : _breakMinutes) * 60;

            SetTimerModeUI(_isWorkSession);
            UpdateTimerDisplay();
        }

        private void BtnApplyTimerSettings_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtWorkMin.Text.Trim(), out int w) && w > 0 &&
                int.TryParse(txtBreakMin.Text.Trim(), out int b) && b > 0)
            {
                _workMinutes = w;
                _breakMinutes = b;

                if (!_isRunning)
                {
                    _remainingSeconds = (_isWorkSession ? _workMinutes : _breakMinutes) * 60;
                    UpdateTimerDisplay();
                }

                MessageBox.Show(
                    $"Налаштування збережено:\nРобота: {_workMinutes} хв\nПерерва: {_breakMinutes} хв",
                    "Налаштування таймера",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            else
            {
                MessageBox.Show(
                    "Будь ласка, введіть коректні додатні числа хвилин.",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void SetTimerModeUI(bool isWork)
        {
            if (isWork)
            {
                lblTimerState.Text = "🔥 ЧАС ФОКУСУВАТИСЬ";
                lblTimerState.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                badgeStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
            }
            else
            {
                lblTimerState.Text = "☕ ЧАС ДЛЯ ВІДПОЧИНКУ";
                lblTimerState.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                badgeStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
            }
        }

        private void UpdateTimerDisplay()
        {
            int min = _remainingSeconds / 60;
            int sec = _remainingSeconds % 60;
            lblTimerDisplay.Text = $"{min:D2}:{sec:D2}";
        }

        private void UpdateStatsDisplay()
        {
            lblFocusStats.Text = $"Завершено сесій: {_completedSessions} | Час у фокусі: {_totalFocusedMinutes} хв";
        }
    }
}
