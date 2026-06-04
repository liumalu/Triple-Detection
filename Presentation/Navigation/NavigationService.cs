using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace TripleDetection.Presentation.Navigation
{

public class NavigationService : INavigationService
{
    private readonly Dictionary<string, Type> _routes = new Dictionary<string, Type>();
    private readonly IServiceProvider _serviceProvider;
    private ContentControl _region;

    public string CurrentViewKey { get; private set; } = "";
    public event Action<string> Navigated;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetRegion(ContentControl region)
    {
        _region = region;
    }

    public void RegisterRoute(string key, Type viewType)
    {
        _routes[key] = viewType;
    }

    public void NavigateTo<TView>() where TView : class
    {
        foreach (var kvp in _routes)
        {
            if (kvp.Value == typeof(TView))
            {
                NavigateTo<TView>(kvp.Key);
                return;
            }
        }
        throw new InvalidOperationException($"View {typeof(TView).Name} not registered");
    }

    public void NavigateTo<TView>(string key) where TView : class
    {
        if (_region == null)
            throw new InvalidOperationException("Region not set. Call SetRegion first.");
        if (!_routes.TryGetValue(key, out var viewType) || viewType != typeof(TView))
            throw new InvalidOperationException($"Route key '{key}' does not match view type {typeof(TView).Name}");

        var view = (TView)_serviceProvider.GetService(typeof(TView));
        _region.Content = view;
        CurrentViewKey = key;
        var handler = Navigated;
        if (handler != null) handler(key);
    }
}
}