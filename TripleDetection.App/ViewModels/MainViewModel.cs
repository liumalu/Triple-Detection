using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;
using TripleDetection.Services;

namespace TripleDetection.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _resultText = "--";
        private string _resultBackground = "#808080";
        private string _details = "Detection details will appear here";
        private bool _isImageViewActive = true;
        private string _selectedProcedure = "";
        private object _currentView;

        public ObservableCollection<string> LogMessages { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ResultHistory { get; } = new ObservableCollection<string>();

        public string ResultText
        {
            get => _resultText;
            set { _resultText = value; OnPropertyChanged(); }
        }

        public string ResultBackground
        {
            get => _resultBackground;
            set { _resultBackground = value; OnPropertyChanged(); }
        }

        public string Details
        {
            get => _details;
            set { _details = value; OnPropertyChanged(); }
        }

        public bool IsImageViewActive
        {
            get => _isImageViewActive;
            set { _isImageViewActive = value; OnPropertyChanged(); }
        }

        public string SelectedProcedure
        {
            get => _selectedProcedure;
            set { _selectedProcedure = value; OnPropertyChanged(); }
        }

        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public ICommand NavigateToProductCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public MainViewModel()
        {
            NavigateToProductCommand = new RelayCommand(_ => NavigateToProduct());
        }

        private void NavigateToProduct()
        {
            CurrentView = new Views.ProductListView();
        }

        public void AddLog(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            if (LogMessages.Count > 1000)
                LogMessages.RemoveAt(0);
            LogMessages.Add(entry);
        }

        public void AddResult(string result)
        {
            if (ResultHistory.Count > 500)
                ResultHistory.RemoveAt(0);
            ResultHistory.Add(result);
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
    }
}