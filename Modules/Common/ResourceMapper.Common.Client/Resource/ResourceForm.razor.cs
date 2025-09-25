using Microsoft.AspNetCore.Components;
using ResourceMapper.Common.Shared.Editor;

namespace ResourceMapper.Common.Client.Resource
{
    public partial class ResourceForm
    {
        private bool _isReadOnly=true;
        private bool _isInitialized = false;

        [Parameter] public string? ResourceUid { get; set; }

        [Parameter,SupplyParameterFromQuery(Name = "mode")] public string? RenderMode { get; set; }

        [Inject] public NavigationManager Navigation { get; set; } = default!;
        private ResourceEditorModel _editorModel = new ResourceEditorModel();
        protected override async Task OnInitializedAsync()
        {
            _isReadOnly = true;

            await base.OnInitializedAsync();
            _isInitialized = true;
        }

        private void OnApplyClicked()
        {
            Navigation.NavigateTo("/");
        }

        private void OnDeleteClicked()
        {
            Navigation.NavigateTo("/");
        }
    }
}
