using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace TripleDetection.Services
{
    public class TabManager
    {
        private readonly Dictionary<string, UserControl> _openViews = new Dictionary<string, UserControl>();
        private readonly Dictionary<string, string> _viewNames = new Dictionary<string, string>();
        private string _activeTag;

        public event EventHandler<string> ActiveViewChanged;
        public event EventHandler<KeyValuePair<string, UserControl>> ViewClosed;
        public event EventHandler<KeyValuePair<string, UserControl>> ViewOpened;

        public TabManager()
        {
            RegisterView("Dashboard", "📊 仪表盘");
            RegisterView("Detection", "🔍 检测执行");
            RegisterView("Products", "📦 产品管理");
            RegisterView("Tasks", "📋 任务管理");
            RegisterView("Logs", "📝 操作日志");
            RegisterView("Settings", "⚙️ 系统配置");
            RegisterView("UserManagement", "👤 用户权限");
        }

        private void RegisterView(string tag, string displayName)
        {
            _viewNames[tag] = displayName;
        }

        public UserControl OpenView(string tag)
        {
            if (_openViews.ContainsKey(tag))
            {
                _activeTag = tag;
                ActiveViewChanged?.Invoke(this, tag);
                return _openViews[tag];
            }

            var view = CreateView(tag);
            _openViews[tag] = view;
            _activeTag = tag;
            ViewOpened?.Invoke(this, new KeyValuePair<string, UserControl>(tag, view));
            ActiveViewChanged?.Invoke(this, tag);
            return view;
        }

        public void CloseView(string tag)
        {
            if (!_openViews.ContainsKey(tag)) return;
            if (_openViews.Count <= 1) return;

            var view = _openViews[tag];
            _openViews.Remove(tag);
            ViewClosed?.Invoke(this, new KeyValuePair<string, UserControl>(tag, view));

            if (_activeTag == tag)
            {
                _activeTag = _openViews.Keys.FirstOrDefault();
                ActiveViewChanged?.Invoke(this, _activeTag);
            }
        }

        public void CloseOtherViews(string keepTag)
        {
            var tagsToClose = _openViews.Keys.Where(k => k != keepTag).ToList();
            foreach (var tag in tagsToClose)
            {
                CloseView(tag);
            }
        }

        public void CloseAllViews()
        {
            var firstTag = _openViews.Keys.FirstOrDefault();
            _openViews.Clear();
            if (!string.IsNullOrEmpty(firstTag))
            {
                _openViews[firstTag] = CreateView(firstTag);
                _activeTag = firstTag;
            }
            ActiveViewChanged?.Invoke(this, _activeTag);
        }

        public void ActivateView(string tag)
        {
            if (_openViews.ContainsKey(tag))
            {
                _activeTag = tag;
                ActiveViewChanged?.Invoke(this, tag);
            }
        }

        private UserControl CreateView(string tag)
        {
            switch (tag)
            {
                case "Dashboard": return new Views.DashboardView();
                case "Detection": return new Views.DetectionView();
                case "Products": return new Views.ProductListView();
                case "Tasks": return new Views.TaskListView();
                case "Logs": return new Views.LogsView();
                case "Settings": return new Views.SettingsView();
                case "UserManagement": return new Views.UserManagementView();
                default: return new Views.DashboardView();
            }
        }

        public string GetViewName(string tag)
        {
            return _viewNames.ContainsKey(tag) ? _viewNames[tag] : tag;
        }

        public Dictionary<string, UserControl> GetOpenViews()
        {
            return _openViews;
        }

        public string GetActiveTag()
        {
            return _activeTag;
        }

        public bool IsViewOpen(string tag)
        {
            return _openViews.ContainsKey(tag);
        }
    }
}