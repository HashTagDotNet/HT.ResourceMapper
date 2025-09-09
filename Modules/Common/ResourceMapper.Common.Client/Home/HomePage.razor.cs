using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ResourceMapper.Common.Shared.HomePage;
using ResourceMapper.Common.Shared.HomePage.Contracts;
using HT.Api.Client.Contracts.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Specialized;
using System.Web;
using Microsoft.AspNetCore.Components.Web;

namespace ResourceMapper.Common.Client.Home
{
    public partial class HomePage
    {
        private readonly HttpClient _httpClient;
        private readonly ISnackbar _snackbar;
        private readonly NavigationManager _navigationManager;

        // Reference to the data grid for triggering refreshes
        private MudDataGrid<ResourceGridItemModel>? dataGrid;

        public HomePage(HttpClient httpClient, ISnackbar snackbar, NavigationManager navigationManager)
        {
            _httpClient = httpClient;
            _snackbar = snackbar;
            _navigationManager = navigationManager;
        }

        public string SearchTerm { get; set; } = string.Empty;
        private bool _isInitialLoad = true;

        protected override async Task OnInitializedAsync()
        {
            // Parse URL parameters on component initialization
         //   ParseUrlParameters();
            await base.OnInitializedAsync();
        }

        private void ParseUrlParameters()
        {
            var uri = new Uri(_navigationManager.Uri);
            var queryParams = QueryHelpers.ParseQuery(uri.Query);

            // Update SearchTerm if it exists in URL
            if (queryParams.TryGetValue("searchFor", out var searchValue))
            {
                SearchTerm = searchValue.FirstOrDefault() ?? string.Empty;
            }
        }

        private async Task HandleKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await PerformSearch();
            }
        }

        public async Task PerformSearch()
        {
            // This method is called when the search button is clicked or Enter is pressed
            // It will trigger a grid refresh and update the URL
            _isInitialLoad = false; // Ensure URL gets updated
            
            if (dataGrid != null)
            {
                await dataGrid.ReloadServerData();
            }
        }

        private async Task<GridData<ResourceGridItemModel>> LoadServerData(GridStateVirtualize<ResourceGridItemModel> state, CancellationToken token)
        {
            try
            {
                var skip = state.StartIndex;
                var take = state.Count;
                if (skip==0 && take==0)
                {
                    // Initial load, set default page size
                    return new GridData<ResourceGridItemModel>()
                    {
                        Items = new List<ResourceGridItemModel>(),
                        TotalItems = 0
                    };
                }
                var request = BuildRequestFromState(state);
                
                // Debug information - you can check browser console for these logs
                Console.WriteLine($"Calling LoadServerData StartIndex: {state.StartIndex}, Count: {state.Count}, Skip: {request.Skip}, Take: {request.Take}");
                
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<ResourceGridResponse>>(
                    BuildApiUrl(request),cancellationToken:token);

                if (response?.Data?.Items != null)
                {
                    Console.WriteLine($"API returned {response.Data.Items.Count} items, TotalItems: {response.Data.TotalItems}");
                    
                    // Always update URL after successful API call to reflect current state
                    UpdateUrlWithRequest(request);
                    
                    return new GridData<ResourceGridItemModel>
                    {
                        Items = response.Data.Items ?? [],
                        TotalItems = response.Data.TotalItems
                    };
                }
                else
                {
                    // Check for errors in the API response
                    if (response?.Errors?.Count > 0)
                    {
                        var errorMessage = string.Join(", ", response.Errors.Select(e => e.Details ?? e.Title ?? "Unknown error"));
                        _snackbar.Add($"Error loading data: {errorMessage}", Severity.Error);
                    }
                    else
                    {
                        _snackbar.Add("No data received from server", Severity.Warning);
                    }

                    return new GridData<ResourceGridItemModel>
                    {
                        Items = new List<ResourceGridItemModel>(),
                        TotalItems = 0
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                _snackbar.Add($"Network error: {ex.Message}", Severity.Error);
                return new GridData<ResourceGridItemModel>
                {
                    Items = new List<ResourceGridItemModel>(),
                    TotalItems = 0
                };
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Unexpected error: {ex.Message}", Severity.Error);
                return new GridData<ResourceGridItemModel>
                {
                    Items = new List<ResourceGridItemModel>(),
                    TotalItems = 0
                };
            }
            finally
            {
                // Mark that initial load is complete
                _isInitialLoad = false;
            }
        }

        private ResourceGridRequest BuildRequestFromState(GridStateVirtualize<ResourceGridItemModel> state)
        {
            // On initial load, check if URL has parameters to override defaults
            if (_isInitialLoad)
            {
                return BuildRequestFromUrl(state);
            }

            // Normal grid state-based request
            return new ResourceGridRequest
            {
                Skip = state.StartIndex,
                Take = state.Count, // Use the actual page size from the grid state
                SearchFor = !string.IsNullOrWhiteSpace(SearchTerm) ? SearchTerm : null,
                OrderBy = null,   // No sorting for now
                OrderDirection = "Asc"
            };
        }

        private ResourceGridRequest BuildRequestFromUrl(GridStateVirtualize<ResourceGridItemModel> state)
        {
            var uri = new Uri(_navigationManager.Uri);
            var queryParams = QueryHelpers.ParseQuery(uri.Query);

            var request = new ResourceGridRequest();

            // Parse skip parameter
            if (queryParams.TryGetValue("skip", out var skipValue) && int.TryParse(skipValue.FirstOrDefault(), out var skip))
            {
                request.Skip = skip;
            }
            else
            {
                request.Skip = state.StartIndex;
            }

            // Parse take parameter
            if (queryParams.TryGetValue("take", out var takeValue) && int.TryParse(takeValue.FirstOrDefault(), out var take))
            {
                request.Take = take;
            }
            else
            {
                request.Take = state.Count; // Use the grid's page size
            }

            // Parse search parameter
            if (queryParams.TryGetValue("searchFor", out var searchValue))
            {
                request.SearchFor = string.IsNullOrWhiteSpace(searchValue.FirstOrDefault()) ? null : searchValue.FirstOrDefault();
            }
            else if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                request.SearchFor = SearchTerm;
            }

            // Parse order parameters
            if (queryParams.TryGetValue("orderBy", out var orderByValue))
            {
                request.OrderBy = string.IsNullOrWhiteSpace(orderByValue.FirstOrDefault()) ? null : orderByValue.FirstOrDefault();
            }

            if (queryParams.TryGetValue("orderDirection", out var orderDirectionValue))
            {
                request.OrderDirection = string.IsNullOrWhiteSpace(orderDirectionValue.FirstOrDefault()) ? "Asc" : orderDirectionValue.FirstOrDefault();
            }
            else
            {
                request.OrderDirection = "Asc";
            }

            return request;
        }

        private void UpdateUrlWithRequest(ResourceGridRequest request)
        {
            var queryParams = new Dictionary<string, string?>();

            // Add non-default parameters to URL
            if (request.Skip > 0)
                queryParams["skip"] = request.Skip.ToString();

            if (request.Take != 15) // Only add if different from default
                queryParams["take"] = request.Take.ToString();

            if (!string.IsNullOrWhiteSpace(request.SearchFor))
                queryParams["searchFor"] = request.SearchFor;

            if (!string.IsNullOrWhiteSpace(request.OrderBy))
                queryParams["orderBy"] = request.OrderBy;

            if (!string.IsNullOrWhiteSpace(request.OrderDirection) && request.OrderDirection != "Asc")
                queryParams["orderDirection"] = request.OrderDirection;

            // Build new URL
            var baseUri = _navigationManager.ToAbsoluteUri(_navigationManager.Uri).GetLeftPart(UriPartial.Path);
            var newUri = QueryHelpers.AddQueryString(baseUri, queryParams);

            // Navigate without adding to history (replace current URL)
            _navigationManager.NavigateTo(newUri, replace: true);
        }

        private string BuildApiUrl(ResourceGridRequest request)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["skip"] = request.Skip.ToString(),
                ["take"] = request.Take.ToString()
            };

            if (!string.IsNullOrWhiteSpace(request.SearchFor))
                queryParams["searchFor"] = request.SearchFor;

            if (!string.IsNullOrWhiteSpace(request.OrderBy))
                queryParams["orderBy"] = request.OrderBy;

            if (!string.IsNullOrWhiteSpace(request.OrderDirection))
                queryParams["orderDirection"] = request.OrderDirection;

            return QueryHelpers.AddQueryString("api/ResourceGrid", queryParams);
        }

        private string FormatTagsDisplay(List<ResourceGridTagModel> tags)
        {
            if (tags == null || !tags.Any())
                return "No tags";

            var displayTags = tags.Take(3).ToList();
            var lines = new List<string>();

            foreach (var tag in displayTags)
            {
                lines.Add($"{tag.TagKey}: {tag.TagValue}");
            }

            if (tags.Count > 3)
            {
                lines.Add($"({tags.Count - 3} more...)");
            }

            return string.Join("\n", lines);
        }
    }
}
