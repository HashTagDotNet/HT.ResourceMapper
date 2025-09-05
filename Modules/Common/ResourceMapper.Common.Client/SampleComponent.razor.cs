using System.Net.Http.Json;
using HT.Api.Client.Contracts.Models;
using Microsoft.AspNetCore.Components;
using ResourceMapper.Common.Shared.Contracts;
using System.Text.Json;

namespace ResourceMapper.Common.Client
{
    public partial class SampleComponent : ComponentBase
    {
        private SampleGetDateTimeResponse? dateTimeResponse;
        private bool isLoading = false;
        private string? errorMessage;
        private int daysOffset = 0;

        protected override async Task OnInitializedAsync()
        {
            await LoadServerDateTime();
        }

        public async Task RefreshAsync()
        {
            await LoadServerDateTime();
        }

        private async Task RefreshDateTime()
        {
            daysOffset = 0;
            await LoadServerDateTime();
        }

        private async Task GetDateWithOffset()
        {
            await LoadServerDateTime(daysOffset);
        }

        private async Task LoadServerDateTime(int? dateOffset = null)
        {
            isLoading = true;
            errorMessage = null;
            StateHasChanged();

            try
            {
                var offset = dateOffset ?? 0;
                // Use the property name from SampleGetDateTimeRequest
                var queryParams = $"?DateOffsetToGet={offset}";
                
                var httpResponse = await HttpClient.GetAsync($"api/sample{queryParams}");
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<ApiResponse<SampleGetDateTimeResponse>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (response?.Data != null)
                    {
                        dateTimeResponse = response.Data;
                    }
                    else
                    {
                        errorMessage = "No data received from server";
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    errorMessage = $"API error ({httpResponse.StatusCode}): {httpResponse.ReasonPhrase}";
                    
                    // Try to extract error details from response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse>(errorContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        if (errorResponse?.Errors?.Any() == true)
                        {
                            var firstError = errorResponse.Errors.First();
                            errorMessage += $" - {firstError.Detail}";
                        }
                    }
                    catch
                    {
                        // If we can't parse the error response, use the raw content
                        if (!string.IsNullOrEmpty(errorContent))
                        {
                            errorMessage += $" - {errorContent}";
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                errorMessage = $"Network error: {ex.Message}";
            }
            catch (JsonException ex)
            {
                errorMessage = $"JSON parsing error: {ex.Message}";
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