using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using CommunityToolkit.Mvvm;
using TripleDetection.Domain.Entities;
using TripleDetection.Application.Services;
using TripleDetection.Domain.Enums;

namespace TripleDetection.Presentation.ViewModels.Production
{
    public class ProductEditViewModel : ObservableObject
    {
        private readonly IProductService _productService;

        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private int _productId;
        [ObservableProperty] private string _code = "";
        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string _description = "";
        [ObservableProperty] private ValidType _validType = ValidType.Year;
        [ObservableProperty] private int _validPeriod = 1;
        [ObservableProperty] private string _solFilePath = "";
        [ObservableProperty] private ProductStatus _status = ProductStatus.Active;
        [ObservableProperty] private string _errorMessage = "";

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

        public ProductEditViewModel(Product product, IProductService productService)
        {
            _productService = productService;
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

        partial void OnCodeChanged(string value) => ErrorMessage = "";
        partial void OnNameChanged(string value) => ErrorMessage = "";
        partial void OnSolFilePathChanged(string value) => ErrorMessage = "";

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
                    product.Id = ProductId;
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