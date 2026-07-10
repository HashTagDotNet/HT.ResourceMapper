using ResourceMapper.Common.Shared.Editor;

namespace ResourceMapper.UI.Web.Components.Editor
{
    /// <summary>
    /// Shared General-tab validation so GeneralTab, ReviewTab, and the save gate agree on
    /// "valid". Writes messages into each field's SingleValueEditor.Messages, which the Mud
    /// fields render via Error/ErrorText.
    /// </summary>
    public static class EditorValidation
    {
        public static bool ValidateGeneral(ResourceEditorModel model)
        {
            ClearMessages(model);
            var isValid = true;

            if (model.ResourceTypeId is null or 0)
            {
                AddError(model.ResourceType, "Resource type is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(model.Domain.EditedValue))
            {
                AddError(model.Domain, "Domain is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(model.Name.EditedValue))
            {
                AddError(model.Name, "Name is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(model.Key.EditedValue))
            {
                AddError(model.Key, "Key is required");
                isValid = false;
            }
            else if (model.IdentityPreview.IsUnique == false)
            {
                AddError(model.Key, model.IdentityPreview.UniquenessMessage ?? "Another resource of this type already uses this key");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>True once Domain + Type + Key are all set — the point at which an early
        /// uniqueness check becomes meaningful (a partial identity always reports "unique").</summary>
        public static bool IsIdentityComplete(ResourceEditorModel model) =>
            model.ResourceTypeId is > 0
            && !string.IsNullOrWhiteSpace(model.Domain.EditedValue)
            && !string.IsNullOrWhiteSpace(model.Key.EditedValue);

        /// <summary>Required (Error) tag with no value → error; Link value not http/https →
        /// error. An empty Suggested/Optional row is not an error (RD3).</summary>
        public static bool ValidateTags(ResourceEditorModel model)
        {
            var isValid = true;

            foreach (var row in model.Tags.Where(t => !t.IsRemoved))
            {
                row.RowMessages.Clear();
                var hasValue = row.Values.Any(v => !string.IsNullOrWhiteSpace(v.EditedValue));
                var label = row.Definition.DisplayName ?? row.Definition.TagDefinitionKey;

                if (row.IsRequired && !hasValue)
                {
                    row.RowMessages.Add(new EditorMessage { Message = $"{label} is required", Level = EditorFieldLevel.Error });
                    isValid = false;
                }

                if (row.IsLink)
                {
                    foreach (var v in row.Values.Where(v => !string.IsNullOrWhiteSpace(v.EditedValue)))
                    {
                        if (!IsValidLinkValue(v.EditedValue))
                        {
                            row.RowMessages.Add(new EditorMessage { Message = $"{label} must be a valid http/https URL", Level = EditorFieldLevel.Error });
                            isValid = false;
                        }
                    }
                }
            }

            return isValid;
        }

        private static bool IsValidLinkValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static void ClearMessages(ResourceEditorModel model)
        {
            model.ResourceType.Messages.Clear();
            model.Domain.Messages.Clear();
            model.Name.Messages.Clear();
            model.Key.Messages.Clear();
        }

        private static void AddError(SingleValueEditor editor, string message) =>
            editor.Messages.Add(new EditorMessage { Message = message, Level = EditorFieldLevel.Error });
    }
}
