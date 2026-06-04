using System;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using Prism.Commands;
using TripleDetection.Presentation.Navigation;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Presentation.ViewModels.Detection
{
    public class MainViewModel : ViewModelBase
    {
        private string _resultText = "--";
        private string _resultBackground = "#808080";
        private string _details = "Detection details will appear here";
        private bool _isImageViewActive = true;
        private string _selectedProcedure = "";
        private object _currentView = null;
        private readonly INavigationService _navigationService;

        public ObservableCollection<string> LogMessages { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ResultHistory { get; } = new ObservableCollection<string>();

        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
        }

        public string ResultBackground
        {
            get => _resultBackground;
            set => SetProperty(ref _resultBackground, value);
        }

        public string Details
        {
            get => _details;
            set => SetProperty(ref _details, value);
        }

        public bool IsImageViewActive
        {
            get => _isImageViewActive;
            set => SetProperty(ref _isImageViewActive, value);
        }

        public string SelectedProcedure
        {
            get => _selectedProcedure;
            set => SetProperty(ref _selectedProcedure, value);
        }

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public DelegateCommand NavigateToProductCommand { get; }

        public MainViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToProductCommand = new DelegateCommand(NavigateToProduct);
        }

        private void NavigateToProduct()
        {
            _navigationService.NavigateTo<Views.Production.ProductListView>("Products");
        }

        public void AddLog(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            if (LogMessages.Count > 1000)
                LogMessages.RemoveAt(0);
            LogMessages.Add(logEntry);
        }

        public void AddResult(string result)
        {
            if (ResultHistory.Count > 500)
                ResultHistory.RemoveAt(0);
            ResultHistory.Add(result);
        }

        public void OnDetectionResult(DetectionResult result)
        {
            // Handle detection result
        }
    }
}