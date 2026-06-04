using System.Windows.Input;
using Prism.Mvvm;

namespace TripleDetection.Presentation.ViewModels
{
    public partial class TabItemViewModel : ViewModelBase
    {
        private string _tag = string.Empty;
        public string Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        private string _displayName = string.Empty;
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        private bool _isClosable = true;
        public bool IsClosable
        {
            get => _isClosable;
            set => SetProperty(ref _isClosable, value);
        }

        private ICommand _selectCommand;
        public ICommand SelectCommand
        {
            get => _selectCommand;
            set => SetProperty(ref _selectCommand, value);
        }

        private ICommand _closeCommand;
        public ICommand CloseCommand
        {
            get => _closeCommand;
            set => SetProperty(ref _closeCommand, value);
        }
    }
}