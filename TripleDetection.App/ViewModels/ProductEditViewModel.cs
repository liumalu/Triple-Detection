using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using TripleDetection.Data.Entities;
using TripleDetection.Services;

namespace TripleDetection.ViewModels
{
    public class ProductEditViewModel : INotifyPropertyChanged
    {
        private readonly ProductService _productService;
        private bool _isEditMode;
        private int _productId;
        private string _code = "";
        private string _name = "";
        private string _description = "";
        private ValidType _validType = ValidType.Year;
        private int _validPeriod = 1;
        private string _solFilePath = "";
        private ProductStatus _status = ProductStatus.Active;
        private string _errorMessage = "";

        public ObservableCollection<ValidType> ValidTypes { get; } = new ObservableCollection<ValidType>
        {
            ValidType.Year,
            ValidType.Month,
            ValidType.Day
        };

        public ObservableCollection<ProductStatus> Statuses { get; } = new ObservableCollection<ProductStatus>
        {
            ProductStatus.Inactive,
            ProductStatus.Active
        };

        public bool IsEditMode
        {
            get => _isEditMode;
            set { _isEditMode = value; OnPropertyChanged(); }
        }

        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); ErrorMessage = ""; }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); ErrorMessage = ""; }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public ValidType ValidType
        {
            get => _validType;
            set { _validType = value; OnPropertyChanged(); }
        }

        public int ValidPeriod
        {
            get => _validPeriod;
            set { _validPeriod = value; OnPropertyChanged(); }
        }

        public string SolFilePath
        {
            get => _solFilePath;
            set { _solFilePath = value; OnPropertyChanged(); ErrorMessage = ""; }
        }

        public ProductStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public string WindowTitle => IsEditMode ? "编辑产品" : "新增产品";

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> RequestClose;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ProductEditViewModel(Product product = null)
        {
            _productService = new ProductService();
            if (product != null)
            {
                IsEditMode = true;
                _productId = product.Id;
                _code = product.Code;
                _name = product.Name;
                _description = product.Description;
                _validType = product.ValidType;
                _validPeriod = product.ValidPeriod;
                _solFilePath = product.SolFilePath;
                _status = product.Status;
            }
        }

        public void BrowseSolFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Solution Files (*.sol)|*.sol",
                Title = "选择方案文件"
            };
            if (dialog.ShowDialog() == true)
            {
                SolFilePath = dialog.FileName;
            }
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                ErrorMessage = "产品编码不能为空";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "产品名称不能为空";
                return false;
            }
            if (string.IsNullOrWhiteSpace(SolFilePath))
            {
                ErrorMessage = "方案文件路径不能为空";
                return false;
            }
            if (ValidPeriod <= 0)
            {
                ErrorMessage = "有效期数量必须大于0";
                return false;
            }

            // Check duplicate code for new product
            if (!IsEditMode)
            {
                var existing = _productService.GetAll().FirstOrDefault(p => p.Code == Code && !p.IsDeleted);
                if (existing != null)
                {
                    ErrorMessage = "产品编码已存在";
                    return false;
                }
            }

            return true;
        }

        public void Save()
        {
            if (!Validate())
                return;

            var product = new Product
            {
                Code = Code,
                Name = Name,
                Description = Description,
                ValidType = ValidType,
                ValidPeriod = ValidPeriod,
                SolFilePath = SolFilePath,
                Status = Status
            };

            if (IsEditMode)
            {
                product.Id = _productId;
                _productService.Update(product, "admin");
            }
            else
            {
                _productService.Create(product, "admin");
            }

            RequestClose?.Invoke(this, true);
        }

        public void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}