using ResourceMapper.Feature1.Shared.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMapper.Feature1.Client
{
    public partial class DateTimeDisplay
    {
        public DateTimeDisplay()
        {

        }
        private GetDateTimeResponse? dateTimeResponse;
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
                var t = await HttpClient.GetFromJsonAsync<GetDateTimeResponse>("api/date");
                dateTimeResponse = t;
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
