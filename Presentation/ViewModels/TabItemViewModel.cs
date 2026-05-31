using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace TripleDetection.Presentation.ViewModels
{
    public class TabItemViewModel : BindableBase
    {
        private string _tag;
        private string _displayName;
        private bool _isActive;
        private bool _isClosable = true;
        private ICommand _selectCommand;
        private ICommand _closeCommand;

        public string Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool IsClosable
        {
            get => _isClosable;
            set => SetProperty(ref _isClosable, value);
        }

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
    }
}