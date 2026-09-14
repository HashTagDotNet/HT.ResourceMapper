namespace MacroPoint.Azure.ResourceExplorer.Common.Shared.Editor
{
    public class EditorMessage
    {
        public string? Message { get; set; }
        public EditorFieldLevel Level { get; set; } = EditorFieldLevel.Error;
    }
}
