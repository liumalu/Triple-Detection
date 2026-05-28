using System;
using System.Windows.Input;

namespace TripleDetection.ViewModels
{
    public class TabItemViewModel
    {
        public string Tag { get; set; }
        public string DisplayName { get; set; }
        public bool IsActive { get; set; }
        public bool IsClosable { get; set; } = true;
        public ICommand SelectCommand { get; set; }
        public ICommand CloseCommand { get; set; }
    }
}