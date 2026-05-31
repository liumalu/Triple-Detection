using System.Windows.Input;
using CommunityToolkit.Mvvm;

using CommunityToolkit.Mvvm.ComponentModel;
namespace TripleDetection.Presentation.ViewModels
{
    public partial class TabItemViewModel : ObservableObject
    {
        [ObservableProperty] private string? _tag;
        [ObservableProperty] private string? _displayName;
        [ObservableProperty] private bool _isActive;
        [ObservableProperty] private bool _isClosable = true;

        public ICommand SelectCommand
        {
            get => _selectCommand;
            set => SetProperty(ref _selectCommand, value);
        }

        public ICommand CloseCommand
        {
            get => _closeCommand;
            set => SetProperty(ref _closeCommand, value);
        }

        private ICommand? _selectCommand;
        private ICommand? _closeCommand;
    }
}