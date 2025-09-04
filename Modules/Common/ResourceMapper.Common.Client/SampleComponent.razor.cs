using System.Net.Http.Json;
using HT.Api.Client.Contracts.Models;
using Microsoft.AspNetCore.Components;
using ResourceMapper.Common.Shared.Contracts;

namespace ResourceMapper.Common.Client
{
    public partial class SampleComponent:ComponentBase
    {
        public SampleComponent()
        {

        }
        private SampleGetDateTimeResponse? dateTimeResponse;
        private bool isLoading = false;
        private string? errorMessage;

        protected override async Task OnInitializedAsync()
        {
            await LoadServerDate();
        }

        private async Task RefreshDate()
        {
            await LoadServerDate();
        }

        public async Task RefreshAsync()
        {
            await LoadServerDate();
        }

        private async Task LoadServerDate()
        {
            isLoading = true;
            errorMessage = null;
            StateHasChanged();

            try
            {
                var t = await HttpClient.GetFromJsonAsync<ApiResponse<SampleGetDateTimeResponse>>("api/date");
                dateTimeResponse = t.Data;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading server date: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }
    }
}
