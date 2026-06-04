using System;
using System.Windows;
using System.Windows.Controls;
using TripleDetection.Application.Services;
using TripleDetection.Infrastructure.Persistence;
using TripleDetection.Presentation.ViewModels.Audit;

namespace TripleDetection.Presentation.Views.Audit
{
    public partial class StatisticsView : UserControl
    {
        private readonly StatisticsViewModel _viewModel;

        public StatisticsView()
        {
            InitializeComponent();
            var connectionString = "Data Source=" + System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db");
            var connectionFactory = new SqliteConnectionFactory(connectionString);
            var statisticsService = new StatisticsService(connectionFactory);
            _viewModel = new StatisticsViewModel(statisticsService);

            dpStartDate.SelectedDate = DateTime.Today.AddDays(-30);
            dpEndDate.SelectedDate = DateTime.Today;

            DataContext = _viewModel;
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            var stats = _viewModel.PassRateStats;
            txtTotalCount.Text = stats.TotalCount.ToString();
            txtOkCount.Text = stats.OkCount.ToString();
            txtNgCount.Text = stats.NgCount.ToString();
            txtPassRate.Text = stats.PassRate.ToString("F1") + "%";

            var timeStats = _viewModel.TimeStats;
            txtAvgTime.Text = timeStats.AverageElapsedMs.ToString("F0") + " ms";
            txtMinTime.Text = timeStats.MinElapsedMs.ToString("F0") + " ms";
            txtMaxTime.Text = timeStats.MaxElapsedMs.ToString("F0") + " ms";

            dgPassRateTrend.ItemsSource = _viewModel.PassRateTrend;
            dgProductStats.ItemsSource = _viewModel.ProductStats;
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (dpStartDate.SelectedDate.HasValue)
                _viewModel.StartDate = dpStartDate.SelectedDate.Value;
            if (dpEndDate.SelectedDate.HasValue)
                _viewModel.EndDate = dpEndDate.SelectedDate.Value;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadStatistics();
            LoadStatistics();
        }
    }
}