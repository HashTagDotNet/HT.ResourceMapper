using ResourceMapper.Common.Shared.Editor.Contracts;

namespace ResourceMapper.Common.Shared.Editor
{
    /// <summary>
    /// Per-circuit stack of in-progress ancestor editor states, backing the nested "Create new…"
    /// dependency-target flow (slice #9). A single ResourceEditor component instance is reused
    /// across nested descents/ascents (Blazor interactive routing re-parents the same instance),
    /// so this stack — not a stack of components — holds what each ancestor level needs to resume.
    /// Registered scoped (per-circuit): survives route changes within a connection, cleared on
    /// refresh/reconnect, matching the design's "refresh mid-stack loses the in-progress stack."
    /// </summary>
    public sealed class EditorNavStack
    {
        private readonly Stack<EditorFrame> _frames = new();

        public bool HasFrames => _frames.Count > 0;
        public int Depth => _frames.Count;

        public void Push(EditorFrame frame) => _frames.Push(frame);
        public EditorFrame Peek() => _frames.Peek();
        public EditorFrame Pop() => _frames.Pop();
        public void Clear() => _frames.Clear();
    }

    /// <summary>One ancestor level: the parent's live edit state plus enough to resume it.</summary>
    public sealed class EditorFrame
    {
        /// <summary>The parent's full editor response (model + reference data), unsaved edits intact.</summary>
        public required OpenEditorResponse Response { get; init; }

        /// <summary>Base-relative path+query (no leading slash) the parent was at before descending.</summary>
        public required string ParentRoute { get; init; }

        /// <summary>
        /// "DependsOn" | "DependentOn" — which tab's picker initiated the nested create — or
        /// <see cref="NoLink"/> for a descent that creates an unrelated resource and links nothing.
        /// </summary>
        public required string Direction { get; init; }

        /// <summary>
        /// Direction for a descent that must NOT link its child into the parent: the General tab's
        /// "Add Resource", which catalogues the next resource rather than a dependency of this one.
        /// It still gets a frame, because the return journey is the same either way — pop back to the
        /// caller's exact route, mode and tab. Only the linking differs.
        /// </summary>
        public const string NoLink = "None";

        /// <summary>"Create" | "Edit" — the parent's mode, restored on ascent.</summary>
        public required string ParentMode { get; init; }

        /// <summary>
        /// Index of the tab the parent was on when it descended, so the ascent returns to the picker
        /// that started the whole thing. Without it the parent reloads on General: ResourceEditor
        /// resets the active tab whenever the nested level changes, and coming back up is exactly
        /// such a change — so the one tab you could not be returned to was the Dependencies or
        /// Dependent On tab you left from.
        /// </summary>
        public required int ParentTab { get; init; }

        /// <summary>Set by the child on a successful save; consumed (and cleared) when popped.</summary>
        public ReturnedChild? Returned { get; set; }
    }

    /// <summary>What a nested-created child reports back to its parent for linking.</summary>
    public sealed class ReturnedChild
    {
        public required string ResourceUid { get; init; }
        public required string Name { get; init; }
        public required string TypeName { get; init; }
        public string? Domain { get; init; }
    }
}
