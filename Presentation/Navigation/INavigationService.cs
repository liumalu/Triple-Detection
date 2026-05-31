namespace TripleDetection.Presentation.Navigation;

public interface INavigationService
{
    void NavigateTo<TView>() where TView : class;
    void NavigateTo<TView>(string key) where TView : class;
    string CurrentViewKey { get; }
    event Action<string>? Navigated;
    void SetRegion(System.Windows.Controls.ContentControl region);
    void RegisterRoute(string key, System.Type viewType);
}