using ResourceMapper.Common.Shared.SavedViews;

namespace ResourceMapper.Common.Server.SavedViews
{
    /// <summary>
    /// Shared state between the grid and the Saved Views menu.
    /// <para>
    /// The menu lives in the layout's hamburger while the query it saves lives in the grid page, so
    /// the two cannot pass parameters to each other. This scoped object is the seam: the grid
    /// publishes its current query and which saved view is open, and the menu reads it and raises
    /// <see cref="Changed"/> so it can re-render.
    /// </para>
    /// <para>Scoped, so it is per Blazor circuit — never shared between users.</para>
    /// </summary>
    public class GridViewContext
    {
        /// <summary>The grid's current query string, or empty when the grid is not on screen.</summary>
        public string CurrentQuery { get; private set; } = string.Empty;

        /// <summary>The saved view currently open, if any.</summary>
        public string OpenViewUid { get; private set; } = string.Empty;

        /// <summary>Its name, so Save can re-save without prompting.</summary>
        public string OpenViewName { get; private set; } = string.Empty;

        /// <summary>Its query as last saved — Save is pointless when this equals CurrentQuery.</summary>
        public string OpenViewQuery { get; private set; } = string.Empty;

        /// <summary>True when a view is open AND the grid has moved away from what was saved.</summary>
        public bool CanSave =>
            !string.IsNullOrEmpty(OpenViewUid) &&
            !string.Equals(CurrentQuery, OpenViewQuery, StringComparison.Ordinal);

        /// <summary>True while the grid is the active page; the menu hides its actions otherwise.</summary>
        public bool GridIsActive { get; private set; }

        /// <summary>Raised whenever any of the above changes, so the menu can re-render.</summary>
        public event Action? Changed;

        /// <summary>
        /// Asks the grid to load a saved view. Carries the whole view, not just its query, because
        /// the grid is what knows the NORMALISED query once parsed — and OpenViewQuery has to match
        /// that, or Save would look changed the instant a view is opened.
        /// </summary>
        public event Func<SavedViewModel, Task>? OpenRequested;

        public void SetCurrentQuery(string query)
        {
            CurrentQuery = query ?? string.Empty;
            GridIsActive = true;
            Changed?.Invoke();
        }

        public void SetOpenView(string uid, string name, string query)
        {
            OpenViewUid = uid ?? string.Empty;
            OpenViewName = name ?? string.Empty;
            OpenViewQuery = query ?? string.Empty;
            Changed?.Invoke();
        }

        public void ClearOpenView() => SetOpenView(string.Empty, string.Empty, string.Empty);

        /// <summary>The grid has left the screen; the menu's Save actions no longer apply.</summary>
        public void GridDetached()
        {
            GridIsActive = false;
            Changed?.Invoke();
        }

        public Task RequestOpenAsync(SavedViewModel view) =>
            OpenRequested?.Invoke(view) ?? Task.CompletedTask;
    }
}
