using MacroPoint.Azure.ResourceExplorer.Api.Client.Contracts.Models;
using MacroPoint.Azure.ResourceExplorer.Api.Service.Contracts;
using MacroPoint.Azure.ResourceExplorer.Common.Shared.Editor;
using MacroPoint.Azure.ResourceExplorer.Common.Shared.Editor.Contracts;
using MudBlazor;

namespace MacroPoint.Azure.ResourceExplorer.UI.Web.Components.Editor
{
    /// <summary>
    /// Model-to-request mapping and server-error mapping shared by the full editor and the quick-add
    /// form, so both go through the same transactional save with the same request shape.
    /// </summary>
    public static class EditorSave
    {
        public static SaveResourceRequest BuildRequest(ResourceEditorModel model)
        {
            var flattenedTags = FlattenTags(model);
            return new SaveResourceRequest
            {
                Mode = model.IsPersisted ? "Edit" : "Create",
                ResourceUid = model.ResourceUid,
                ResourceTypeId = model.ResourceTypeId ?? 0,
                Domain = model.Domain.EditedValue,
                ResourceName = model.Name.EditedValue,
                ResourceKey = model.Key.EditedValue,
                Description = model.Description.EditedValue,
                PrimaryTagDefinitionId = ResolveEffectivePrimary(model, flattenedTags),
                Tags = flattenedTags,
                DependsOnUids = FlattenDependencyUids(model.DependsOn),
                DependentOnUids = FlattenDependencyUids(model.DependentOn)
            };
        }

        /// <summary>Maps field errors onto their editors; tag/relationship errors (a server-side
        /// backstop) go straight to the snackbar.</summary>
        public static void ApplyServerErrors(ResourceEditorModel model, ApiServiceResponse<SaveResourceResponse> response, ISnackbar snackbar)
        {
            foreach (var error in response.ApiResponse.Errors ?? new List<Message>())
            {
                var message = error.Details ?? error.Title ?? "Invalid";
                var target = error.PropertyName switch
                {
                    "request.ResourceTypeId" => model.ResourceType,
                    "request.Domain" => model.Domain,
                    "request.ResourceName" => model.Name,
                    "request.ResourceKey" => model.Key,
                    _ => null
                };

                if (target is not null)
                {
                    target.Messages.Add(new EditorMessage { Message = message, Level = EditorFieldLevel.Error });
                }
                else if (string.Equals(error.PropertyName, "request.Tags", StringComparison.Ordinal)
                         || string.Equals(error.PropertyName, "request.DependsOnUids", StringComparison.Ordinal)
                         || string.Equals(error.PropertyName, "request.DependentOnUids", StringComparison.Ordinal))
                {
                    snackbar.Add(message, Severity.Error);
                }
            }
        }

        /// <summary>
        /// Re-seeds a Create-mode model's tag rows for a newly chosen type: drops the previous
        /// pre-seeded rows (user-added rows stay), adds the type's entry-point template rows and every
        /// globally required tag (PL-53), and applies the template's default primary.
        /// </summary>
        public static void SeedTagsForType(ResourceEditorModel model, OpenEditorResponse response, int? resourceTypeId)
        {
            model.Tags = model.Tags.Where(t => !t.IsPreSeeded).ToList();
            if (resourceTypeId is null) return;

            var templates = response.EntryPointTemplates.Where(t => t.ResourceTypeId == resourceTypeId).ToList();
            var newDefaultPrimaryId = templates.FirstOrDefault(t => t.IsDefaultPrimary)?.TagDefinitionId;

            foreach (var template in templates)
            {
                if (model.Tags.Any(t => t.TagDefinitionId == template.TagDefinitionId)) continue;

                var definition = response.TagDictionary.FirstOrDefault(d => d.TagDefinitionId == template.TagDefinitionId);
                if (definition is null || definition.IsDomainTag) continue;

                model.Tags.Add(PreSeededRow(definition, template.IsDefaultPrimary));
            }

            foreach (var definition in response.TagDictionary.Where(IsGloballyRequired))
            {
                if (model.Tags.Any(t => t.TagDefinitionId == definition.TagDefinitionId)) continue;
                model.Tags.Add(PreSeededRow(definition, false));
            }

            if (newDefaultPrimaryId is not null)
            {
                foreach (var t in model.Tags)
                    t.IsPrimary = t.TagDefinitionId == newDefaultPrimaryId;
                model.PrimaryTagDefinitionId = newDefaultPrimaryId;
            }
        }

        /// <summary>
        /// Mirrors the server's required-tag rule (ResourceService.ValidateTags): every
        /// RequirementLevel='Error' definition except the domain tag (enforced through request.Domain)
        /// and system tags (plumbing, not user metadata).
        /// </summary>
        public static bool IsGloballyRequired(TagDefinitionModel d) =>
            !d.IsDomainTag && !d.IsSystemTag && string.Equals(d.RequirementLevel, "Error", StringComparison.Ordinal);

        public static TagRowEditor PreSeededRow(TagDefinitionModel definition, bool isPrimary) => new()
        {
            Definition = definition,
            TagDefinitionId = definition.TagDefinitionId,
            Values = new List<SingleValueEditor>(),
            IsPrimary = isPrimary,
            IsPreSeeded = true
        };

        private static List<SaveTagValue> FlattenTags(ResourceEditorModel model)
        {
            var result = new List<SaveTagValue>();
            foreach (var row in model.Tags.Where(t => !t.IsRemoved))
            {
                foreach (var value in row.Values.Where(v => !string.IsNullOrWhiteSpace(v.EditedValue)))
                {
                    result.Add(new SaveTagValue
                    {
                        TagDefinitionId = row.TagDefinitionId,
                        TagDefinitionKey = row.Definition.TagDefinitionKey,
                        Value = value.EditedValue
                    });
                }
            }
            return result;
        }

        private static List<string> FlattenDependencyUids(List<DependencyRowEditor> rows) =>
            rows.Where(r => !r.IsRemoved).Select(r => r.OtherResourceUid).Distinct().ToList();

        // RD2: only send a primary when its row is a Link tag with a non-empty value that
        // survives this save (an emptied/removed primary row must clear the primary).
        private static int? ResolveEffectivePrimary(ResourceEditorModel model, List<SaveTagValue> flattenedTags)
        {
            if (model.PrimaryTagDefinitionId is null) return null;

            var primaryRow = model.Tags.FirstOrDefault(t => !t.IsRemoved && t.TagDefinitionId == model.PrimaryTagDefinitionId);
            if (primaryRow is null || !primaryRow.IsLink) return null;

            var survives = flattenedTags.Any(t => t.TagDefinitionId == model.PrimaryTagDefinitionId);
            return survives ? model.PrimaryTagDefinitionId : null;
        }
    }
}
