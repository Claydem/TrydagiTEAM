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

            // Встановлення поточної дати в шапці
            var ukCulture = CultureInfo.GetCultureInfo("uk-UA");
            var rawDate = DateTime.Today.ToString("dddd, d MMMM yyyy", ukCulture);
            lblTodayDate.Text = char.ToUpper(rawDate[0]) + rawDate[1..];

            // Налаштування дати за замовчуванням (+7 днів)
            dpDeadline.SelectedDate = DateTime.Today.AddDays(7);

            // Налаштування таймера концентрації
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            // Завантаження проектів
            LoadProjects();

            // Оновлення розрахунків та списків
            RefreshAllViews();
        }

        #region Завантаження та оновлення даних

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
            // Оновлення списку проектів на вкладці 1
            icProjects.ItemsSource = null;
            icProjects.ItemsSource = _projects;

            var activeProjects = _projects.Where(p => !p.IsCompleted && p.RemainingHours > 0).ToList();
            lblProjectCount.Text = $"Всього: {_projects.Count} | Активних: {activeProjects.Count}";

            // Оновлення вибору проекту для таймера (вкладка 3)
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

            // Оновлення вкладки 2 (Розподіл по днях)
            RefreshScheduleTab(activeProjects);
        }

        private void RefreshScheduleTab(List<Project>? activeProjects = null)
        {
            activeProjects ??= _projects.Where(p => !p.IsCompleted && p.RemainingHours > 0).ToList();

            // 1. Зведений час на сьогодні
            double totalTodayHours = activeProjects.Sum(p => p.RequiredDailyHours);
            lblTotalTodayHours.Text = Project.FormatHours(totalTodayHours);

            // 2. Список щоденного навантаження за проектами
            icProjectDailyRates.ItemsSource = null;
            icProjectDailyRates.ItemsSource = activeProjects;

            // 3. Календарний розклад на найближчі 7 днів
            var ukCulture = CultureInfo.GetCultureInfo("uk-UA");
            var daysSchedule = new List<DaySchedule>();

            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(i);
                string dayTitle;
                if (i == 0) dayTitle = $"Сьогодні ({date:dd.MM})";
                else if (i == 1) dayTitle = $"Завтра ({date:dd.MM})";
                else
                {
                    var dayName = date.ToString("dddd", ukCulture);
                    dayTitle = $"{char.ToUpper(dayName[0]) + dayName[1..]} ({date:dd.MM})";
                }

                // Проекти, які актуальні для цього дня (дедлайн не раніше ніж ця дата)
                var tasksForDay = activeProjects
                    .Where(p => p.Deadline.Date >= date.Date)
                    .Select(p => new DayTask
                    {
                        ProjectTitle = p.Title,
                        DurationText = Project.FormatHours(p.RequiredDailyHours),
                        DeadlineText = $"до {p.Deadline:dd.MM}"
                    })
                    .ToList();

                double dayTotalHours = activeProjects
                    .Where(p => p.Deadline.Date >= date.Date)
                    .Sum(p => p.RequiredDailyHours);

                string totalText = tasksForDay.Count > 0 
                    ? $"Разом: {Project.FormatHours(dayTotalHours)}" 
                    : "Немає завдань";

                daysSchedule.Add(new DaySchedule
                {
                    Date = date,
                    DayTitle = dayTitle,
                    TotalTimeFormatted = totalText,
                    Tasks = tasksForDay
                });
            }

            icDaysSchedule.ItemsSource = null;
            icDaysSchedule.ItemsSource = daysSchedule;
        }

        #endregion

        #region Вкладка 1: Проекти

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

            // Очищення форми
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

        #endregion

        #region Вкладка 2: Розподіл по днях

        private void BtnRefreshSchedule_Click(object sender, RoutedEventArgs e)
        {
            RefreshAllViews();
        }

        #endregion

        #region Вкладка 3: Фокус уваги (Таймер Помодоро)

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateTimerDisplay();
            }
            else
            {
                // Таймер дійшов до нуля
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

                    // Автозарахування часу до вибраного проекту
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

                    // Перемикаємо на перерву
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
                    // Закінчилась перерва, перемикаємо на роботу
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

        #endregion
    }
}