using Microsoft.AspNetCore.Components;
using MudBlazor;
using ResourceMapper.Common.Shared.Editor;
using Microsoft.JSInterop;

namespace ResourceMapper.Common.Client.Resource
{
    public partial class ResourceForm
    {
        
        [Inject] private ISnackbar Toast { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        private bool _isReadOnly = true;
        private bool _isInitialized = false;

        [Parameter] public string? ResourceUid { get; set; }

        [Parameter, SupplyParameterFromQuery(Name = "mode")] public string? RenderMode { get; set; }

        [Inject] public NavigationManager Navigation { get; set; } = default!;
        private ResourceEditorModel _editorModel = new ResourceEditorModel();
        private string[] _errors = [];
        private bool _success;
        private MudForm? editorForm;
        private bool _isDisabled;
        private string? _serverValidationError;

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

        
        private async Task PasteLinkAsync()
        {
            try
            {
                // Try to get both text and HTML from clipboard
                var clipboardData = await JSRuntime.InvokeAsync<ClipboardData>("getClipboardData");
                
                string linkText = "";
                string linkUrl = "";

                // First try to parse HTML content for rich links
                if (!string.IsNullOrWhiteSpace(clipboardData.Html))
                {
                    var (text, url) = ExtractLinkFromHtml(clipboardData.Html);
                    if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(url))
                    {
                        linkText = text;
                        linkUrl = url;
                    }
                }
                
                // Fallback to plain text
                if (string.IsNullOrEmpty(linkText) && !string.IsNullOrWhiteSpace(clipboardData.Text))
                {
                    if (Uri.TryCreate(clipboardData.Text.Trim(), UriKind.Absolute, out var uri))
                    {
                        linkUrl = clipboardData.Text.Trim();
                        linkText = GenerateSmartTitle(uri);
                    }
                    else
                    {
                        // If it's not a URL, just put the text in the name field
                        linkText = clipboardData.Text.Trim();
                        linkUrl = "";
                    }
                }
                
                if (!string.IsNullOrEmpty(linkText))
                {
                    _editorModel.NewTagName = linkText;
                    _editorModel.NewTagValue = linkUrl;
                    
                    StateHasChanged();
                    
                    if (!string.IsNullOrEmpty(linkUrl))
                    {
                        Toast.Add($"Pasted link: {linkText}", Severity.Success);
                    }
                    else
                    {
                        Toast.Add("Pasted text to tag name", Severity.Info);
                    }
                }
                else
                {
                    Toast.Add("Clipboard is empty", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                Toast.Add($"Failed to read clipboard: {ex.Message}", Severity.Error);
            }
        }

        private (string text, string url) ExtractLinkFromHtml(string html)
        {
            try
            {
                // Simple regex to extract href and text from HTML links
                var linkRegex = new System.Text.RegularExpressions.Regex(
                    @"<a[^>]*href\s*=\s*[""']([^""']*)[""'][^>]*>([^<]*)</a>", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                var match = linkRegex.Match(html);
                if (match.Success)
                {
                    var url = match.Groups[1].Value;
                    var text = match.Groups[2].Value;
                    
                    // Clean up the text (remove extra whitespace, decode HTML entities)
                    text = System.Net.WebUtility.HtmlDecode(text).Trim();
                    
                    return (text, url);
                }
            }
            catch
            {
                // Ignore regex errors
            }
            
            return ("", "");
        }

        private string GenerateSmartTitle(Uri uri)
        {
            // Enhanced logic to generate better titles from URLs
            var host = uri.Host.Replace("www.", "");
            
            // Common service patterns
            var titleMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "portal.azure.com", "Azure Portal" },
                { "github.com", "GitHub" },
                { "stackoverflow.com", "Stack Overflow" },
                { "docs.microsoft.com", "Microsoft Docs" },
                { "google.com", "Google" },
                { "teams.microsoft.com", "Microsoft Teams" },
                { "outlook.office.com", "Outlook" },
                { "sharepoint.com", "SharePoint" }
            };

            // Check for known services
            foreach (var mapping in titleMappings)
            {
                if (host.Contains(mapping.Key))
                {
                    return mapping.Value;
                }
            }

            // Try to extract meaningful parts from the path
            var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments.Length > 0)
            {
                var lastSegment = pathSegments.Last();
                // Convert kebab-case or snake_case to title case
                if (lastSegment.Contains('-') || lastSegment.Contains('_'))
                {
                    var words = lastSegment.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                    return string.Join(" ", words.Select(w => 
                        char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant()));
                }
            }

            // Fallback to domain name
            return char.ToUpperInvariant(host[0]) + host.Substring(1);
        }

        private IEnumerable<string> ValidateCode(string value)
        {
            // Client-side validation
            if (string.IsNullOrWhiteSpace(value))
            {
                yield return "Code is required";
            }
            else if (value.Length < 2)
            {
                yield return "Code must be at least 2 characters long";
            }
            else if (value.Length > 50)
            {
                yield return "Code cannot exceed 50 characters";
            }

          
        }

        private async Task SubmitAsync()
        {

            // Clear any previous server-side validation errors
            _serverValidationError = null;
            
            await editorForm.Validate();
            
            if (editorForm.IsValid)
            {
                _isDisabled = true;
                StateHasChanged();
                
                // Simulate server call
                await Task.Delay(3000);
                
                // Simulate server-side validation failure
                //var simulatedServerSideValidationError = "Your code is not formatted correctly";
                
                //// Add server-side validation error
                //_serverValidationError = simulatedServerSideValidationError;
                
                //// Force revalidation to show the server error
                //await editorForm.Validate();
                
                _isDisabled = false;
                StateHasChanged();
                
                // Set success based on whether there are server errors
                //_success = string.IsNullOrEmpty(_serverValidationError);
                Toast.Add("saved", Severity.Info, config =>
                {
                    config.CloseAfterNavigation = true;
                    config.VisibleStateDuration = 1000;
                });
            }
            else
            {
                _success = false;
                _errors = editorForm.Errors.ToArray();
            }
        }

        public class ClipboardData
        {
            public string Text { get; set; } = "";
            public string Html { get; set; } = "";
        }
    }
}
