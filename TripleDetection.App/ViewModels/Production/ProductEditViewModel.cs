using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Prism.Mvvm;
using TripleDetection.Data.Entities;
using TripleDetection.Services;
using TripleDetection.Services.Production;

namespace TripleDetection.ViewModels.Production
{
    public class ProductEditViewModel : BindableBase
    {
        private readonly IProductService _productService;
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
            set => SetProperty(ref _isEditMode, value);
        }

        public string Code
        {
            get => _code;
            set { if (SetProperty(ref _code, value)) ErrorMessage = ""; }
        }

        public string Name
        {
            get => _name;
            set { if (SetProperty(ref _name, value)) ErrorMessage = ""; }
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public ValidType ValidType
        {
            get => _validType;
            set => SetProperty(ref _validType, value);
        }

        public int ValidPeriod
        {
            get => _validPeriod;
            set => SetProperty(ref _validPeriod, value);
        }

        public string SolFilePath
        {
            get => _solFilePath;
            set { if (SetProperty(ref _solFilePath, value)) ErrorMessage = ""; }
        }

        public ProductStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string WindowTitle => IsEditMode ? "编辑产品" : "新增产品";

        public event EventHandler<bool> RequestClose;

        public ProductEditViewModel(Product product = null)
            : this(product, new ProductService())
        {
        }

        public ProductEditViewModel(Product product, IProductService productService)
        {
            _productService = productService;
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
            System.Diagnostics.Debug.WriteLine($"[Save] 节点1-开始校验 | Code={Code}, Name={Name}, SolFilePath={SolFilePath}, ValidPeriod={ValidPeriod}");
            if (!Validate())
            {
                System.Diagnostics.Debug.WriteLine($"[Save] 节点1-校验失败 | ErrorMessage={ErrorMessage}");
                return;
            }
            System.Diagnostics.Debug.WriteLine($"[Save] 节点1-校验通过");

            System.Diagnostics.Debug.WriteLine($"[Save] 节点2-UI数据 | Code={Code}, Name={Name}, Description={Description}, SolFilePath={SolFilePath}, Status={Status}");

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
            System.Diagnostics.Debug.WriteLine($"[Save] 节点3-构建Product对象 | Code={product.Code}, Name={product.Name}, SolFilePath={product.SolFilePath}");

            System.Diagnostics.Debug.WriteLine($"[Save] 节点4-开始数据库存储...");
            Task.Run(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[Save] 后台线程开始...");
                if (IsEditMode)
                {
                    product.Id = _productId;
                    _productService.Update(product, "admin", SessionManager.CurrentUserId);
                }
                else
                {
                    _productService.Create(product, "admin", SessionManager.CurrentUserId);
                }
                System.Diagnostics.Debug.WriteLine($"[Save] 后台线程完成 | ProductId={product.Id}");

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"[Save] UI线程回调完成");
                    RequestClose?.Invoke(this, true);
                });
            });
        }

        public void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}
