using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using Prism.Mvvm;
using TripleDetection.Domain;
using TripleDetection.Domain.Entities;
using TripleDetection.Application.Services;
using TripleDetection.Domain.Enums;

namespace TripleDetection.Presentation.ViewModels.Production
{
    public partial class ProductEditViewModel : ViewModelBase
    {
        private readonly IProductService _productService;
        private readonly IAuditLogService _auditLogService;

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private int _productId;
        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        private string _code = "";
        public string Code
        {
            get => _code;
            set
            {
                if (SetProperty(ref _code, value))
                    ErrorMessage = "";
            }
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                    ErrorMessage = "";
            }
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private ValidType _validType = ValidType.Year;
        public ValidType ValidType
        {
            get => _validType;
            set => SetProperty(ref _validType, value);
        }

        private int _validPeriod = 1;
        public int ValidPeriod
        {
            get => _validPeriod;
            set => SetProperty(ref _validPeriod, value);
        }

        private string _solFilePath = "";
        public string SolFilePath
        {
            get => _solFilePath;
            set
            {
                if (SetProperty(ref _solFilePath, value))
                    ErrorMessage = "";
            }
        }

        private ProductStatus _status = ProductStatus.Active;
        public ProductStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

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

        public string WindowTitle => IsEditMode ? "编辑产品" : "新增产品";

        public event EventHandler<bool> RequestClose;

        public ProductEditViewModel(Product product, IProductService productService, IAuditLogService auditLogService)
        {
            _productService = productService;
            _auditLogService = auditLogService;
            if (product != null)
            {
                IsEditMode = true;
                ProductId = product.Id;
                Code = product.Code;
                Name = product.Name;
                Description = product.Description;
                ValidType = product.ValidType;
                ValidPeriod = product.ValidPeriod;
                SolFilePath = product.SolFilePath;
                Status = product.Status;
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

            int currentUserId = SessionManager.CurrentUserId;

            Task.Run(() =>
            {
                try
                {
                    if (IsEditMode)
                    {
                        product.Id = ProductId;
                        _productService.Update(product, SessionManager.CurrentUserName ?? "Unknown", SessionManager.CurrentUserId);
                        _auditLogService.Log(currentUserId, "PRODUCT_UPDATE", "Product", product.Id,
                            JsonConvert.SerializeObject(new { productId = product.Id, productCode = product.Code }));
                    }
                    else
                    {
                        _productService.Create(product, SessionManager.CurrentUserName ?? "Unknown", SessionManager.CurrentUserId);
                        _auditLogService.Log(currentUserId, "PRODUCT_CREATE", "Product", 0,
                            JsonConvert.SerializeObject(new { productCode = product.Code }));
                    }

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        RequestClose?.Invoke(this, true);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Save] 保存失败: {ex.Message}");
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ErrorMessage = "保存失败: " + ex.Message;
                    });
                }
            });
        }

        public void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}
