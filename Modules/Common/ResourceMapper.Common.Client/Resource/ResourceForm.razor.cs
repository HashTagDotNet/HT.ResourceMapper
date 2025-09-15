using Microsoft.AspNetCore.Components;

namespace ResourceMapper.Common.Client.Resource
{
    public partial class ResourceForm
    {
        private bool _isReadOnly=true;
        private bool _isInitialized = false;

        [Parameter] public string? ResourceUid { get; set; }

       [Parameter,SupplyParameterFromQuery(Name = "mode")] public string? RenderMode { get; set; }
       protected override async Task OnInitializedAsync()
       {
           _isReadOnly = true;
           
           await base.OnInitializedAsync();
           _isInitialized = true;
        }
    }
}
