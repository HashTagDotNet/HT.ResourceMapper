using HT.Api.Client.Contracts.Models;
using ResourceMapper.Common.Shared.Contracts;
using System.Net.Http.Json;

namespace ResourceMapper.Feature1.Client
{
    public partial class DateTimeDisplay
    {
        public DateTimeDisplay()
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
