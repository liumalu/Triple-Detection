# 用户管理模块重构实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 重构用户管理模块，参考 ProductListView 架构模式，实现 JSON 文件持久化 + 仓储模式 + 分页

**Architecture:** 分层架构（View → ViewModel → UserService → UserRepository → SimpleJsonHelper → JSON文件），复用现有 SimpleJsonHelper 做 JSON 序列化/反序列化

**Tech Stack:** WPF (.NET 4.8), C# 8.0, MVVM, Repository Pattern, JSON persistence via SimpleJsonHelper

---

## 文件结构

```
TripleDetection.Data/
├── Repositories/
│   └── UserRepository.cs          # 新建：IUserRepository 实现，JSON 持久化
├── Entities/
│   └── UserQuery.cs               # 新建：查询条件类，继承 PagedQuery

TripleDetection.App/
├── ViewModels/
│   ├── UserManagementViewModel.cs # 重构：分页/CRUD/查询
│   └── UserEditViewModel.cs       # 新建：编辑窗口 ViewModel
├── Views/
│   ├── UserManagementView.xaml    # 重构：列表视图 + 分页控件
│   ├── UserManagementView.xaml.cs # 重构：code-behind 路由
│   └── UserEditWindow.xaml        # 新建：编辑窗口（替代 UserEditDialog）
├── Services/
│   └── UserService.cs             # 新建：业务服务层（代理 UserRepository）
└── Services/Settings/
    └── UserService.cs             # 删除：旧实现

待删除文件（确认无引用后再删）:
TripleDetection.App\Views\UserEditDialog.xaml
TripleDetection.App\Views\UserEditDialog.xaml.cs
```

---

## Task 1: 创建 UserQuery 查询条件类

**Files:**
- Create: `TripleDetection.Data/Entities/UserQuery.cs`

- [ ] **Step 1: 创建 UserQuery.cs**

```csharp
using System;
using TripleDetection.Data.Repositories;

namespace TripleDetection.Data.Entities
{
    /// <summary>
    /// 用户查询条件
    /// </summary>
    public class UserQuery : PagedQuery
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public string StatusText { get; set; }
    }
}
```

- [ ] **Step 2: 验证编译**

```bash
"/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" "d:\xcm\Triple-Detection\TripleDetection.Data\TripleDetection.Data.csproj" -t:Build -p:Configuration=Debug
```
Expected: BUILD SUCCEEDED

---

## Task 2: 创建 IUserRepository 接口

**Files:**
- Create: `TripleDetection.Data/Repositories/IUserRepository.cs`

- [ ] **Step 1: 创建 IUserRepository.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories
{
    /// <summary>
    /// 用户仓储接口（Username 作为主键，非 int Id）
    /// </summary>
    public interface IUserRepository
    {
        User GetByUsername(string username);
        IEnumerable<User> GetAll();
        IEnumerable<User> Find(Expression<Func<User, bool>> predicate);
        void Add(User entity);
        void Update(User entity);
        void Delete(string username);
        int Count();
        int Count(Expression<Func<User, bool>> predicate);
        PagedResult<User> Query(UserQuery query);
    }
}
```

- [ ] **Step 2: 验证编译**

Expected: BUILD SUCCEEDED

---

## Task 3: 创建 UserRepository JSON 实现

**Files:**
- Create: `TripleDetection.Data/Repositories/UserRepository.cs`

- [ ] **Step 1: 创建 UserRepository.cs**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using TripleDetection.Data.Entities;
using TripleDetection.Services;

namespace TripleDetection.Data.Repositories
{
    /// <summary>
    /// 基于 JSON 文件的用户仓储实现
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly string _configPath;
        private List<User> _users;
        private bool _loaded = false;
        private readonly object _lock = new object();

        public UserRepository()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(baseDir, "Config", "users.json");
        }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            lock (_lock)
            {
                if (_loaded) return;
                _users = SimpleJsonHelper.Load<List<User>>(_configPath) ?? new List<User>();
                _loaded = true;
            }
        }

        private void Save()
        {
            SimpleJsonHelper.Save(_users, _configPath);
        }

        public User GetByUsername(string username)
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _users.FirstOrDefault(u => u.Username == username);
            }
        }

        public IEnumerable<User> GetAll()
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _users.ToList();
            }
        }

        public IEnumerable<User> Find(Expression<Func<User, bool>> predicate)
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _users.Where(predicate.Compile()).ToList();
            }
        }

        public void Add(User entity)
        {
            EnsureLoaded();
            lock (_lock)
            {
                entity.CreateAt = DateTime.Now;
                _users.Add(entity);
                Save();
            }
        }

        public void Update(User entity)
        {
            EnsureLoaded();
            lock (_lock)
            {
                var existing = _users.FirstOrDefault(u => u.Username == entity.Username);
                if (existing != null)
                {
                    var index = _users.IndexOf(existing);
                    _users[index] = entity;
                    Save();
                }
            }
        }

        public void Delete(string username)
        {
            EnsureLoaded();
            lock (_lock)
            {
                _users.RemoveAll(u => u.Username == username);
                Save();
            }
        }

        public int Count()
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _users.Count;
            }
        }

        public int Count(Expression<Func<User, bool>> predicate)
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _users.Where(predicate.Compile()).Count();
            }
        }

        public PagedResult<User> Query(UserQuery query)
        {
            EnsureLoaded();
            lock (_lock)
            {
                var filtered = _users.AsEnumerable();

                if (!string.IsNullOrEmpty(query.Username))
                    filtered = filtered.Where(u => u.Username.IndexOf(query.Username, StringComparison.OrdinalIgnoreCase) >= 0);

                if (!string.IsNullOrEmpty(query.Role))
                    filtered = filtered.Where(u => u.Role == query.Role);

                if (!string.IsNullOrEmpty(query.StatusText))
                    filtered = filtered.Where(u => u.StatusText == query.StatusText);

                var total = filtered.Count();
                var items = filtered
                    .Skip(query.PageIndex * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                return new PagedResult<User>(items, total, query.PageIndex, query.PageSize);
            }
        }
    }
}
```

- [ ] **Step 2: 验证编译**

Expected: BUILD SUCCEEDED

**注意:** 此处使用了 `List<User>` 作为 SimpleJsonHelper 的反序列化目标，因为 User 是 class 类型，JSON 数组 `[...]` 反序列化后是 `object[]`，需要能正确还原为 `List<User>`。SimpleJsonHelper.Load<List<User>>() 依赖于 Load<T> 的泛型处理，如果 SimpleJsonHelper 内部将 object[] 赋值给了 List<User>，需要确保 SimpleJsonHelper 的反序列化能处理数组类型转换。**如果编译失败，需要额外修改 SimpleJsonHelper 支持 List<T> 反序列化。**

---

## Task 4: 创建 UserService 业务服务层

**Files:**
- Create: `TripleDetection.App/Services/UserService.cs`

- [ ] **Step 1: 创建 UserService.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;

namespace TripleDetection.Services
{
    /// <summary>
    /// 用户业务服务层（代理 UserRepository）
    /// </summary>
    public class UserService
    {
        private readonly IUserRepository _repository;

        public UserService() : this(new UserRepository()) { }

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public List<User> GetAll() => _repository.GetAll().ToList();

        public User GetByUsername(string username) => _repository.GetByUsername(username);

        public void Add(User user)
        {
            var existing = _repository.GetByUsername(user.Username);
            if (existing != null)
                throw new InvalidOperationException($"用户名 {user.Username} 已存在");
            _repository.Add(user);
        }

        public void Update(User user)
        {
            var existing = _repository.GetByUsername(user.Username);
            if (existing == null)
                throw new InvalidOperationException($"用户名 {user.Username} 不存在");
            _repository.Update(user);
        }

        public void Delete(string username) => _repository.Delete(username);

        public void Enable(string username)
        {
            var user = _repository.GetByUsername(username);
            if (user != null)
            {
                user.IsEnabled = true;
                _repository.Update(user);
            }
        }

        public void Disable(string username)
        {
            var user = _repository.GetByUsername(username);
            if (user != null)
            {
                user.IsEnabled = false;
                _repository.Update(user);
            }
        }

        public void Lock(string username)
        {
            var user = _repository.GetByUsername(username);
            if (user != null)
            {
                user.IsLocked = true;
                _repository.Update(user);
            }
        }

        public void Unlock(string username)
        {
            var user = _repository.GetByUsername(username);
            if (user != null)
            {
                user.IsLocked = false;
                _repository.Update(user);
            }
        }

        public bool ValidateUser(string username, string password)
        {
            var user = _repository.GetByUsername(username);
            if (user == null || !user.IsEnabled || user.IsLocked)
                return false;
            return user.Password == password;
        }

        public PagedResult<User> Query(UserQuery query) => _repository.Query(query);
    }
}
```

- [ ] **Step 2: 验证编译**

Expected: BUILD SUCCEEDED

---

## Task 5: 创建 UserManagementViewModel

**Files:**
- Create: `TripleDetection.App/ViewModels/UserManagementViewModel.cs`

- [ ] **Step 1: 创建 UserManagementViewModel.cs（第一部分：属性和构造函数）**

```csharp
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TripleDetection.Data.Entities;
using TripleDetection.Services;

namespace TripleDetection.ViewModels
{
    public class UserManagementViewModel : INotifyPropertyChanged
    {
        private readonly UserService _service;

        public ObservableCollection<User> Users { get; } = new ObservableCollection<User>();

        // 查询条件
        private string _queryUsername = "";
        public string QueryUsername
        {
            get => _queryUsername;
            set { _queryUsername = value; OnPropertyChanged(); }
        }

        private string _queryRole = "";
        public string QueryRole
        {
            get => _queryRole;
            set { _queryRole = value; OnPropertyChanged(); }
        }

        private string _queryStatus = "";
        public string QueryStatus
        {
            get => _queryStatus;
            set { _queryStatus = value; OnPropertyChanged(); }
        }

        // 分页属性
        private int _pageIndex = 0;
        public int PageIndex
        {
            get => _pageIndex;
            set { _pageIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentPageDisplay)); OnPropertyChanged(nameof(TotalPagesDisplay)); }
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set { _pageSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPagesDisplay)); }
        }

        private int _totalCount = 0;
        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPagesDisplay)); }
        }

        private int _totalPages = 0;
        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentPageDisplay)); OnPropertyChanged(nameof(TotalPagesDisplay)); }
        }

        public string CurrentPageDisplay => (PageIndex + 1).ToString();
        public string TotalPagesDisplay => TotalPages.ToString();

        public bool HasPreviousPage => PageIndex > 0;
        public bool HasNextPage => PageIndex < TotalPages - 1;

        // 命令
        public ICommand SearchCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand OpenEditWindowCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }

        public UserManagementViewModel()
        {
            _service = new UserService();

            SearchCommand = new RelayCommand(_ => Search());
            ResetCommand = new RelayCommand(_ => Reset());
            OpenEditWindowCommand = new RelayCommand(OpenEditWindow);
            FirstPageCommand = new RelayCommand(_ => FirstPage());
            PrevPageCommand = new RelayCommand(_ => PreviousPage());
            NextPageCommand = new RelayCommand(_ => NextPage());
            LastPageCommand = new RelayCommand(_ => LastPage());

            Search();
        }
```

- [ ] **Step 2: 创建 UserManagementViewModel.cs（第二部分：Search/Navigate 方法）**

```csharp
        public void Search()
        {
            var query = new UserQuery
            {
                PageIndex = PageIndex,
                PageSize = PageSize,
                Username = QueryUsername,
                Role = string.IsNullOrEmpty(QueryRole) ? null : QueryRole,
                StatusText = string.IsNullOrEmpty(QueryStatus) ? null : QueryStatus
            };

            var result = _service.Query(query);
            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages;

            Users.Clear();
            foreach (var user in result.Items)
            {
                Users.Add(user);
            }

            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }

        public void Reset()
        {
            QueryUsername = "";
            QueryRole = "";
            QueryStatus = "";
            PageIndex = 0;
            Search();
        }

        public void OpenEditWindow(object param)
        {
            User user = param as User;
            var vm = new UserEditViewModel(user);
            var dialog = new Views.UserEditWindow { DataContext = vm, Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                if (vm.IsEdit)
                    _service.Update(vm.User);
                else
                    _service.Add(vm.User);
                Search();
            }
        }

        public void DeleteUser(User user)
        {
            var result = MessageBox.Show($"确定要删除用户 '{user.Username}' 吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _service.Delete(user.Username);
                Search();
            }
        }

        public void EnableUser(string username) { _service.Enable(username); Search(); }
        public void DisableUser(string username) { _service.Disable(username); Search(); }
        public void LockUser(string username) { _service.Lock(username); Search(); }
        public void UnlockUser(string username) { _service.Unlock(username); Search(); }

        // 分页导航
        public void FirstPage() { PageIndex = 0; Search(); }
        public void PreviousPage() { if (HasPreviousPage) { PageIndex--; Search(); } }
        public void NextPage() { if (HasNextPage) { PageIndex++; Search(); } }
        public void LastPage() { PageIndex = TotalPages - 1; Search(); }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
```

- [ ] **Step 3: 验证编译**

Expected: BUILD SUCCEEDED (如果没有 RelayCommand，需要先创建 RelayCommand 或使用 ActionCommand)

**注意:** 如果项目没有 RelayCommand 类，需要使用 `System.Windows.Input.ICommand` 配合简单实现，或参考 ProductListViewModel 的命令实现方式。

---

## Task 6: 创建 UserEditViewModel

**Files:**
- Create: `TripleDetection.App/ViewModels/UserEditViewModel.cs`

- [ ] **Step 1: 创建 UserEditViewModel.cs**

```csharp
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TripleDetection.Data.Entities;

namespace TripleDetection.ViewModels
{
    public class UserEditViewModel : INotifyPropertyChanged
    {
        private readonly User _user;
        private readonly bool _isEdit;

        public User User => _user;
        public bool IsEdit => _isEdit;

        public string Username
        {
            get => _user.Username;
            set { _user.Username = value; OnPropertyChanged(); }
        }

        public string RealName
        {
            get => _user.RealName;
            set { _user.RealName = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _user.Password;
            set { _user.Password = value; OnPropertyChanged(); }
        }

        public string Role
        {
            get => _user.Role;
            set { _user.Role = value; OnPropertyChanged(); }
        }

        public bool IsEnabled
        {
            get => _user.IsEnabled;
            set { _user.IsEnabled = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Roles { get; } = new ObservableCollection<string>
        {
            "Admin", "Supervisor", "Operator", "Viewer"
        };

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public UserEditViewModel(User user = null)
        {
            _isEdit = user != null;
            _user = user ?? new User { Role = "Operator", IsEnabled = true };
            SaveCommand = new RelayCommand(_ => { });
            CancelCommand = new RelayCommand(_ => { });
        }
    }
}
```

- [ ] **Step 2: 验证编译**

Expected: BUILD SUCCEEDED

---

## Task 7: 创建 UserEditWindow

**Files:**
- Create: `TripleDetection.App/Views/UserEditWindow.xaml`
- Create: `TripleDetection.App/Views/UserEditWindow.xaml.cs`

- [ ] **Step 1: 创建 UserEditWindow.xaml**

```xml
<Window x:Class="TripleDetection.Views.UserEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="用户编辑" Height="400" Width="450"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,10">
            <TextBlock Text="用户名:" Width="80" VerticalAlignment="Center"/>
            <TextBox Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" Width="300" VerticalContentAlignment="Center" Padding="4,2"/>
        </StackPanel>

        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,10">
            <TextBlock Text="姓名:" Width="80" VerticalAlignment="Center"/>
            <TextBox Text="{Binding RealName, UpdateSourceTrigger=PropertyChanged}" Width="300" VerticalContentAlignment="Center" Padding="4,2"/>
        </StackPanel>

        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,0,0,10">
            <TextBlock Text="密码:" Width="80" VerticalAlignment="Center"/>
            <PasswordBox x:Name="txtPassword" Width="300" VerticalContentAlignment="Center" Padding="4,2"/>
        </StackPanel>

        <StackPanel Grid.Row="3" Orientation="Horizontal" Margin="0,0,0,10">
            <TextBlock Text="角色:" Width="80" VerticalAlignment="Center"/>
            <ComboBox ItemsSource="{Binding Roles}" SelectedItem="{Binding Role}" Width="300" VerticalContentAlignment="Center"/>
        </StackPanel>

        <StackPanel Grid.Row="4" Orientation="Horizontal" Margin="0,0,0,10">
            <TextBlock Text="启用:" Width="80" VerticalAlignment="Center"/>
            <CheckBox IsChecked="{Binding IsEnabled}" VerticalAlignment="Center"/>
        </StackPanel>

        <StackPanel Grid.Row="6" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="保存" Width="80" Margin="5" Click="BtnSave_Click"
                    Style="{StaticResource PrimaryButtonStyle}"/>
            <Button Content="取消" Width="80" Margin="5" Click="BtnCancel_Click"/>
        </StackPanel>
    </Grid>
</Window>
```

- [ ] **Step 2: 创建 UserEditWindow.xaml.cs**

```csharp
using System.Windows;
using TripleDetection.ViewModels;

namespace TripleDetection.Views
{
    public partial class UserEditWindow : Window
    {
        private UserEditViewModel ViewModel => (UserEditViewModel)DataContext;

        public UserEditWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 将密码框的值同步到 ViewModel
            ViewModel.Password = txtPassword.Password;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
```

- [ ] **Step 3: 验证编译**

Expected: BUILD SUCCEEDED

**注意:** PasswordBox 的 Password 属性不能绑定，需要在 code-behind 中手动同步。

---

## Task 8: 重构 UserManagementView

**Files:**
- Modify: `TripleDetection.App/Views/UserManagementView.xaml`
- Modify: `TripleDetection.App/Views/UserManagementView.xaml.cs`

- [ ] **Step 1: 重构 UserManagementView.xaml（对齐 ProductListView 布局）**

```xml
<UserControl x:Class="TripleDetection.Views.UserManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="White">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 标题 -->
        <TextBlock Grid.Row="0" Text="用户权限管理" FontSize="18" FontWeight="Bold"
                   Foreground="{StaticResource TextPrimaryBrush}" Margin="0,0,0,20"/>

        <!-- 查询条件 -->
        <Border Grid.Row="1" Background="{StaticResource CardBackgroundBrush}" Padding="16" Margin="0,0,0,10">
            <StackPanel Orientation="Horizontal">
                <StackPanel Orientation="Horizontal" Margin="0,0,16,0">
                    <TextBlock Text="用户名:" VerticalAlignment="Center" Margin="0,0,8,0"
                               Foreground="{StaticResource TextPrimaryBrush}"/>
                    <TextBox Text="{Binding QueryUsername, UpdateSourceTrigger=PropertyChanged}"
                             Width="120" VerticalContentAlignment="Center" Padding="4,2"/>
                </StackPanel>

                <StackPanel Orientation="Horizontal" Margin="0,0,16,0">
                    <TextBlock Text="角色:" VerticalAlignment="Center" Margin="0,0,8,0"
                               Foreground="{StaticResource TextPrimaryBrush}"/>
                    <ComboBox SelectedItem="{Binding QueryRole}" Width="120" VerticalContentAlignment="Center">
                        <ComboBoxItem Content=""/>
                        <ComboBoxItem Content="Admin"/>
                        <ComboBoxItem Content="Supervisor"/>
                        <ComboBoxItem Content="Operator"/>
                        <ComboBoxItem Content="Viewer"/>
                    </ComboBox>
                </StackPanel>

                <StackPanel Orientation="Horizontal" Margin="0,0,16,0">
                    <TextBlock Text="状态:" VerticalAlignment="Center" Margin="0,0,8,0"
                               Foreground="{StaticResource TextPrimaryBrush}"/>
                    <ComboBox SelectedItem="{Binding QueryStatus}" Width="120" VerticalContentAlignment="Center">
                        <ComboBoxItem Content=""/>
                        <ComboBoxItem Content="正常"/>
                        <ComboBoxItem Content="已禁用"/>
                        <ComboBoxItem Content="已锁定"/>
                    </ComboBox>
                </StackPanel>

                <Button Content="查询" Width="80" Margin="5" Command="{Binding SearchCommand}"
                        Style="{StaticResource PrimaryButtonStyle}"/>
                <Button Content="重置" Width="80" Margin="5" Command="{Binding ResetCommand}"/>
                <Button Content="新增用户" Width="100" Margin="5" Command="{Binding OpenEditWindowCommand}" CommandParameter="{x:Null}"
                        Style="{StaticResource PrimaryButtonStyle}"/>
            </StackPanel>
        </Border>

        <!-- 用户列表 -->
        <Border Grid.Row="2" Background="{StaticResource CardBackgroundBrush}" Padding="16">
            <DataGrid x:Name="dgUsers" AutoGenerateColumns="False"
                      ItemsSource="{Binding Users}"
                      SelectionMode="Single"
                      CanUserAddRows="False"
                      CanUserDeleteRows="False"
                      IsReadOnly="True"
                      GridLinesVisibility="Horizontal"
                      HeadersVisibility="Column"
                      Background="White"
                      BorderThickness="0">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="用户名" Binding="{Binding Username}" Width="120"/>
                    <DataGridTextColumn Header="姓名" Binding="{Binding RealName}" Width="100"/>
                    <DataGridTextColumn Header="角色" Binding="{Binding Role}" Width="80"/>
                    <DataGridTextColumn Header="状态" Binding="{Binding StatusText}" Width="80"/>
                    <DataGridTextColumn Header="创建时间" Binding="{Binding CreatedAt, StringFormat=yyyy-MM-dd HH:mm}" Width="140"/>
                    <DataGridTemplateColumn Header="操作" Width="*">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal">
                                    <Button Content="编辑" Width="50" Margin="2" Command="{Binding DataContext.OpenEditWindowCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                    <Button Content="删除" Width="50" Margin="2"
                                            Style="{StaticResource DangerButtonStyle}"
                                            Click="BtnDelete_Click"/>
                                    <Button Content="启用" Width="50" Margin="2" Click="BtnEnable_Click"/>
                                    <Button Content="禁用" Width="50" Margin="2" Click="BtnDisable_Click"/>
                                    <Button Content="锁定" Width="50" Margin="2" Click="BtnLock_Click"/>
                                    <Button Content="解锁" Width="50" Margin="2" Click="BtnUnlock_Click"/>
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>

        <!-- 分页控件 -->
        <Border Grid.Row="3" Background="{StaticResource CardBackgroundBrush}" Padding="10" Margin="0,10,0,0">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <TextBlock Text="共" VerticalAlignment="Center" Margin="0,0,5,0"/>
                <TextBlock Text="{Binding TotalCount}" VerticalAlignment="Center" FontWeight="Bold"/>
                <TextBlock Text="条" VerticalAlignment="Center" Margin="0,0,10,0"/>
                <Button Content="首页" Width="60" Margin="2" Command="{Binding FirstPageCommand}"/>
                <Button Content="上一页" Width="60" Margin="2" Command="{Binding PrevPageCommand}"/>
                <TextBlock VerticalAlignment="Center" Margin="5,0">
                    <Run Text="{Binding CurrentPageDisplay, Mode=OneWay}"/>
                    <Run Text="/"/>
                    <Run Text="{Binding TotalPagesDisplay, Mode=OneWay}"/>
                </TextBlock>
                <Button Content="下一页" Width="60" Margin="2" Command="{Binding NextPageCommand}"/>
                <Button Content="末页" Width="60" Margin="2" Command="{Binding LastPageCommand}"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: 重构 UserManagementView.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Controls;
using TripleDetection.Data.Entities;
using TripleDetection.ViewModels;

namespace TripleDetection.Views
{
    public partial class UserManagementView : UserControl
    {
        private UserManagementViewModel ViewModel => (UserManagementViewModel)DataContext;

        public UserManagementView()
        {
            InitializeComponent();
            DataContext = new UserManagementViewModel();
            Loaded += (s, e) => ViewModel.SearchCommand.Execute(null);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var user = btn.DataContext as User;
            if (user == null) return;
            ViewModel.DeleteUser(user);
        }

        private void BtnEnable_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var user = btn.DataContext as User;
            if (user == null) return;
            ViewModel.EnableUser(user.Username);
        }

        private void BtnDisable_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var user = btn.DataContext as User;
            if (user == null) return;
            ViewModel.DisableUser(user.Username);
        }

        private void BtnLock_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var user = btn.DataContext as User;
            if (user == null) return;
            ViewModel.LockUser(user.Username);
        }

        private void BtnUnlock_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var user = btn.DataContext as User;
            if (user == null) return;
            ViewModel.UnlockUser(user.Username);
        }
    }
}
```

- [ ] **Step 3: 验证编译**

Expected: BUILD SUCCEEDED

---

## Task 9: 验证编译（完整项目）

- [ ] **Step 1: 编译整个解决方案**

```bash
"/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" "d:\xcm\Triple-Detection\TripleDetection.App\TripleDetection.App.csproj" -t:Rebuild -p:Configuration=Debug
```
Expected: BUILD SUCCEEDED, 0 Errors

---

## Task 10: 删除旧文件（确认编译通过后）

**Files to delete:**
- `TripleDetection.App\Services\Settings\UserService.cs`
- `TripleDetection.App\Views\UserEditDialog.xaml`
- `TripleDetection.App\Views\UserEditDialog.xaml.cs`

- [ ] **Step 1: 删除旧文件**

```bash
rm "d:\xcm\Triple-Detection\TripleDetection.App\Services\Settings\UserService.cs"
rm "d:\xcm\Triple-Detection\TripleDetection.App\Views\UserEditDialog.xaml"
rm "d:\xcm\Triple-Detection\TripleDetection.App\Views\UserEditDialog.xaml.cs"
```

- [ ] **Step 2: 最终验证编译**

Expected: BUILD SUCCEEDED

---

## 验证清单

- [ ] 编译通过，0 errors
- [ ] 启动应用，用户列表显示 3 条初始用户数据
- [ ] 新增用户 → 保存 → 列表刷新
- [ ] 编辑用户 → 保存 → 列表刷新
- [ ] 删除用户 → 确认 → 用户从列表移除
- [ ] 启用/禁用/锁定/解锁 → 状态更新，JSON 持久化
- [ ] 查询（用户名/角色/状态）→ 正确筛选
- [ ] 分页（首页/上一页/下一页/末页）→ 正常工作
- [ ] 重启应用 → 数据从 JSON 正确加载

---

## 依赖关系（Task 执行顺序）

```
Task 1 (UserQuery) → Task 2 (IUserRepository) → Task 3 (UserRepository) → Task 4 (UserService)
                                                                      ↓
                                            Task 5 (UserManagementViewModel) ← (依赖 Task 4)
                                                                      ↓
                         Task 6 (UserEditViewModel) → Task 7 (UserEditWindow)
                                                                      ↓
                          Task 8 (UserManagementView) ← (依赖 Task 5, 6, 7)
                                                                      ↓
                                        Task 9 (验证编译)
                                                                      ↓
                                      Task 10 (删除旧文件)
```