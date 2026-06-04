using System;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using TripleDetection.Application.Models;
using TripleDetection.Application.Services;

namespace TripleDetection.Presentation.ViewModels.Audit
{
    public class StatisticsViewModel : BindableBase
    {
        private readonly IStatisticsService _statisticsService;

        private DateTime _startDate = DateTime.Today.AddDays(-30);
        private DateTime _endDate = DateTime.Today;

        public StatisticsViewModel(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
            // Removed: LoadStatistics(); - let the view call it when ready
        }

        public DateTime StartDate
        {
            get => _startDate;
            set { SetProperty(ref _startDate, value); LoadStatistics(); }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set { SetProperty(ref _endDate, value); LoadStatistics(); }
        }

        public PassRateStatistics PassRateStats { get; private set; }
        public DetectionTimeStatistics TimeStats { get; private set; }
        public ObservableCollection<DailyPassRateTrend> PassRateTrend { get; private set; }
        public ObservableCollection<ProductDetectionStatistics> ProductStats { get; private set; }

        public void LoadStatistics()
        {
            try
            {
                PassRateStats = _statisticsService.GetPassRateStatistics(_startDate, _endDate.AddDays(1));
                TimeStats = _statisticsService.GetDetectionTimeStatistics(_startDate, _endDate.AddDays(1));

                var trend = _statisticsService.GetDailyPassRateTrend(_startDate, _endDate.AddDays(1));
                PassRateTrend = new ObservableCollection<DailyPassRateTrend>(trend);

                var productStats = _statisticsService.GetProductStatistics(_startDate, _endDate.AddDays(1));
                ProductStats = new ObservableCollection<ProductDetectionStatistics>(productStats);

                RaisePropertyChanged(nameof(PassRateStats));
                RaisePropertyChanged(nameof(TimeStats));
                RaisePropertyChanged(nameof(PassRateTrend));
                RaisePropertyChanged(nameof(ProductStats));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadStatistics failed: {ex.Message}");
                // Reset to safe defaults
                PassRateStats = new PassRateStatistics();
                TimeStats = new DetectionTimeStatistics();
                PassRateTrend = new ObservableCollection<DailyPassRateTrend>();
                ProductStats = new ObservableCollection<ProductDetectionStatistics>();
            }
        }
    }
}