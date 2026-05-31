using Prism.Events;

namespace TripleDetection.Presentation.Events
{
    /// <summary>
    /// Published when a view is navigated to (opened in the main region).
    /// </summary>
    public class ViewOpenedEvent : PubSubEvent<ViewNavigationPayload>
    {
    }

    /// <summary>
    /// Published when a view tab is closed.
    /// </summary>
    public class ViewClosedEvent : PubSubEvent<string>
    {
    }

    /// <summary>
    /// Published when the active view changes. Payload is the view tag (e.g. "Dashboard").
    /// </summary>
    public class ActiveViewChangedEvent : PubSubEvent<string>
    {
    }

    /// <summary>
    /// Payload for view navigation events.
    /// </summary>
    public class ViewNavigationPayload
    {
        public string Tag { get; set; }
        public string DisplayName { get; set; }
    }
}