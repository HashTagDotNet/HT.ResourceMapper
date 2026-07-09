using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using HT.Api.Service.Contracts.BuildersOfT;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.HomePage.Contracts;
using ResourceMapper.Common.Shared.HomePage;
using ResourceMapper.Common.Shared.Editor.Contracts;

namespace ResourceMapper.Common.Server.Resources
{
    public class ResourceService : IResourceService
    {
        private readonly IResourceRepository _repo;

        public ResourceService(IResourceRepository repository)
        {
            _repo = repository;
        }
        
        public async Task<ApiServiceResponse<ResourceGridResponse>> GetResourceGridItems(ResourceGridRequest? request, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<ResourceGridResponse>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }
                if (request.Skip < 0)
                {
                    builder.Validation.AddValidation("request.Skip", "Must be greater than or equal to 0");
                }
                if (request.Take <= 0)
                {
                    builder.Validation.AddValidation("request.Take", "Must be greater than 0");
                }
                ValidateSearchFor(builder, request.SearchFor);
                ValidateOrderBy(builder, request.OrderBy);
                var filters = ValidateAndSanitizeFilters(builder, request.Filters);
                if (builder.IsOk == false)
                {
                    return builder.BuildResponse();
                }

                var repositoryResult = await _repo.GetResourceGridItemsAsync(
                    request.SearchFor,
                    NormalizeOrderBy(request.OrderBy),
                    request.OrderDirection,
                    request.Skip,
                    ClampTake(request.Take),
                    DefaultTagLimit,
                    filters,
                    cancellationToken);

                var response = new ResourceGridResponse
                {
                    TotalItems = repositoryResult.TotalCount,
                    Items = repositoryResult.GridItems?.Select(MapToResourceGridItemModel).ToList()
                };

                builder.Data.Set(response);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");  
                return builder.BuildResponse();
            }
        }
        public async Task<ApiServiceResponse<ResourceGridFacetResponse>> GetResourceGridFacet(
            ResourceGridFacetRequest? request, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<ResourceGridFacetResponse>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                if (!FacetColumns.Contains(request.Column))
                {
                    builder.Validation.AddValidation("request.Column", "Must be 'ResourceType' or 'Tag'");
                }
                var isTag = string.Equals(request.Column, ColumnTag, StringComparison.Ordinal);
                if (isTag && string.IsNullOrWhiteSpace(request.TagKey))
                {
                    builder.Validation.AddValidation("request.TagKey", "Is required for a Tag facet");
                }
                if (isTag && request.TagKey is { Length: > MaxTagKeyLength })
                {
                    builder.Validation.AddValidation("request.TagKey", $"Must be {MaxTagKeyLength} characters or fewer");
                }
                ValidateSearchFor(builder, request.SearchFor);
                var filters = ValidateAndSanitizeFilters(builder, request.Filters);
                if (builder.IsOk == false)
                {
                    return builder.BuildResponse();
                }

                // Strip the facet's own dimension so it never constrains itself (the sproc also strips).
                var otherFilters = filters
                    .Where(f => !(string.Equals(f.Column, request.Column, StringComparison.Ordinal)
                                  && (!isTag || string.Equals(f.TagKey, request.TagKey, StringComparison.Ordinal))))
                    .ToList();

                var rows = await _repo.GetFilterValuesAsync(
                    request.Column, isTag ? request.TagKey : null, request.SearchFor, otherFilters, cancellationToken);

                var response = new ResourceGridFacetResponse
                {
                    Column = request.Column,
                    TagKey = isTag ? request.TagKey : null,
                    Values = rows.Select(r => new ResourceGridFacetValue
                    {
                        Value = r.Value,
                        IsBlank = r.IsBlank,
                        Count = r.ItemCount,
                        Display = r.IsBlank ? "(blank)" : (r.Value ?? string.Empty)
                    }).ToList()
                };

                builder.Data.Set(response);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<List<string>>> GetTagFilterKeys(CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<List<string>>();
            try
            {
                var keys = await _repo.GetAllTagKeysAsync(cancellationToken);
                builder.Data.Set(keys);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<OpenEditorResponse>> GetResourceEditorModelAsync(OpenEditorRequest request,CancellationToken cancellationToken=default)
        {
            var response = new ApiServiceResponse<OpenEditorResponse>();

            var allResourceTypes = await _repo.GetAllResourceTypesAsync(cancellationToken);

            // new response
            response.ApiResponse.Data ??= new()
            {
                ResourceTypes = allResourceTypes.Select(r => new KeyValuePair<string, string>(r.ResourceTypeUid, r.TypeName)).ToList()
            };
            response.ApiResponse.Data.EditorModel = new Shared.Editor.ResourceEditorModel()
            {
                Key = new(),
                Name = new(),
                Description = new(),
                ResourceType = new(),
                Domain = new(),
                ResourceUid = Guid.NewGuid().ToString(),
                Mode = string.Equals(request.Mode, "Edit", StringComparison.OrdinalIgnoreCase) ? "Edit" : "Create",
            };

            return response;

        }
        /// <summary>
        /// Example method showing how to handle resource not found vs endpoint not found
        /// </summary>
        public async Task<ApiServiceResponse<ResourceGridItemModel>> GetResourceByUid(string resourceUid, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<ResourceGridItemModel>();
            
            try
            {
                if (string.IsNullOrWhiteSpace(resourceUid))
                {
                    builder.Validation.AddValidation("resourceUid", "Resource UID is required");
                    return builder.BuildResponse();
                }

                // This would be your repository call to find a specific resource
                // var resource = await _repo.GetResourceByUidAsync(resourceUid, cancellationToken);
                
                // Simulating resource not found scenario
                ResourceGridItem? resource = null; // Replace with actual repository call
                
                if (resource == null)
                {
                    // This creates a RESOURCE NOT FOUND error (HTTP 422)
                    // which is different from endpoint not found (HTTP 404)
                    builder.Errors.AddError(
                        CallStatusCode.NotFound, 
                        $"Resource with UID '{resourceUid}' was not found in the database.",
                        "resourceUid");
                    
                    // You can also add additional context
                    builder.Meta.AddTag("ResourceType", "Resource");
                    builder.Meta.AddTag("SearchCriteria", resourceUid);
                    
                    return builder.BuildResponse();
                }

                var responseItem = MapToResourceGridItemModel(resource);
                builder.Data.Set(new ResourceGridItemModel { /* ... map your single item ... */ });
                
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message);
                return builder.BuildResponse();
            }
        }

        // ---------------- filter validation / sanitization (§5b) ----------------

        private const string ColumnResourceType = "ResourceType";
        private const string ColumnResourceName = "ResourceName";
        private const string ColumnDescription = "Description";
        private const string ColumnTag = "Tag";
        private const int DefaultTagLimit = 5;
        private const int MaxFilters = 4;
        private const int MaxValuesPerFilter = 500;
        private const int MaxTagKeyLength = 50;
        private const int MaxValueLength = 2000;
        private const int MaxTextLength = 255;
        private const int MaxSearchLength = 255;

        private static readonly HashSet<string> FilterColumns = new(StringComparer.Ordinal)
            { ColumnResourceType, ColumnResourceName, ColumnDescription, ColumnTag };
        private static readonly HashSet<string> FacetColumns = new(StringComparer.Ordinal)
            { ColumnResourceType, ColumnTag };
        private static readonly HashSet<string> OrderByWhitelist = new(StringComparer.Ordinal)
            { "ResourceId", "ResourceUid", "ResourceKey", "ResourceName", "Description", "CreatedOn", "UpdatedOn", "TypeName" };
        private static readonly int[] AllowedTakes = { 50, 100, 500 };

        private static void ValidateSearchFor<T>(ServiceResponseBuilder<T> builder, string? searchFor) where T : class, new()
        {
            if (searchFor is { Length: > MaxSearchLength })
                builder.Validation.AddValidation("request.SearchFor", $"Must be {MaxSearchLength} characters or fewer");
        }

        private static void ValidateOrderBy<T>(ServiceResponseBuilder<T> builder, string? orderBy) where T : class, new()
        {
            if (!string.IsNullOrWhiteSpace(orderBy) && !OrderByWhitelist.Contains(orderBy))
                builder.Validation.AddValidation("request.OrderBy", "Is not a sortable column");
        }

        private static string NormalizeOrderBy(string? orderBy)
            => !string.IsNullOrWhiteSpace(orderBy) && OrderByWhitelist.Contains(orderBy) ? orderBy! : "ResourceName";

        private static int ClampTake(int take)
            => Array.IndexOf(AllowedTakes, take) >= 0 ? take : 100;

        /// <summary>
        /// Validates each filter and returns only the CONSTRAINING ones (empty enumerable selections
        /// and blank text filters are dropped — the "All = no constraint" rule). Adds validation
        /// messages to <paramref name="builder"/> on any hard failure; callers must check IsOk.
        /// </summary>
        private static List<ResourceGridFilterDefinition> ValidateAndSanitizeFilters<T>(
            ServiceResponseBuilder<T> builder, List<ResourceGridFilterDefinition>? filters) where T : class, new()
        {
            var result = new List<ResourceGridFilterDefinition>();
            if (filters == null || filters.Count == 0)
                return result;

            if (filters.Count > MaxFilters)
                builder.Validation.AddValidation("request.Filters", $"A maximum of {MaxFilters} filters is supported");

            foreach (var f in filters)
            {
                var col = f.Column ?? string.Empty;
                if (!FilterColumns.Contains(col))
                {
                    builder.Validation.AddValidation("request.Filters", $"Unknown filter column '{col}'");
                    continue;
                }

                var isTag = string.Equals(col, ColumnTag, StringComparison.Ordinal);
                var isText = string.Equals(col, ColumnResourceName, StringComparison.Ordinal)
                             || string.Equals(col, ColumnDescription, StringComparison.Ordinal);

                if (isTag)
                {
                    if (string.IsNullOrWhiteSpace(f.TagKey))
                    {
                        builder.Validation.AddValidation("request.Filters", "A Tag filter requires a TagKey");
                        continue;
                    }
                    if (f.TagKey!.Length > MaxTagKeyLength)
                    {
                        builder.Validation.AddValidation("request.Filters", $"TagKey must be {MaxTagKeyLength} characters or fewer");
                        continue;
                    }
                }

                if (isText)
                {
                    if (f.Kind != ResourceGridFilterKind.Text || f.Operator != ResourceGridFilterOperator.Contains)
                    {
                        builder.Validation.AddValidation("request.Filters", $"Column '{col}' supports only a 'contains' text filter");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(f.Text))
                        continue; // non-constraining — drop
                    if (f.Text!.Length > MaxTextLength)
                    {
                        builder.Validation.AddValidation("request.Filters", $"Filter text must be {MaxTextLength} characters or fewer");
                        continue;
                    }
                    result.Add(f);
                    continue;
                }

                // enumerable (ResourceType or Tag)
                if (f.Kind != ResourceGridFilterKind.Enumerable
                    || (f.Operator != ResourceGridFilterOperator.Equals && f.Operator != ResourceGridFilterOperator.NotEquals))
                {
                    builder.Validation.AddValidation("request.Filters", $"Column '{col}' supports only Equals/NotEquals value filters");
                    continue;
                }

                var values = f.Values?.Where(v => v != null).ToList() ?? new List<string>();
                if (values.Count == 0 && !f.IncludeBlank)
                    continue; // non-constraining — drop
                if (values.Count > MaxValuesPerFilter)
                {
                    builder.Validation.AddValidation("request.Filters", $"A filter supports at most {MaxValuesPerFilter} values");
                    continue;
                }
                if (values.Any(v => v.Length > MaxValueLength))
                {
                    builder.Validation.AddValidation("request.Filters", $"A filter value must be {MaxValueLength} characters or fewer");
                    continue;
                }

                result.Add(new ResourceGridFilterDefinition
                {
                    Column = col,
                    TagKey = isTag ? f.TagKey : null,
                    Kind = ResourceGridFilterKind.Enumerable,
                    Operator = f.Operator,
                    Values = values,
                    IncludeBlank = f.IncludeBlank
                });
            }

            return result;
        }

        private static ResourceGridItemModel MapToResourceGridItemModel(ResourceGridItem item)
        {
            return new ResourceGridItemModel
            {
                ResourceUid = item.ResourceUid ?? string.Empty,
                ResourceName = item.ResourceName ?? string.Empty,
                ResourceType = item.ResourceType,
                Description = item.Description ?? string.Empty,
                LastUpdatedOn = item.LastUpdatedOn,
                RelativeLastUpdatedOn = GetRelativeTimeString(item.LastUpdatedOn),
                Tags = item.Tags?.Select(MapToResourceGridTagModel).ToList() ?? new List<ResourceGridTagModel>(),
                TotalTags = item.Tags?.Count
            };
        }

        private static ResourceGridTagModel MapToResourceGridTagModel(ResourceGridTag tag)
        {
            return new ResourceGridTagModel
            {
                TagUid = tag.TagUid ?? string.Empty,
                TagKey = tag.TagKey ?? string.Empty,
                ContentType = tag.ContentType ?? string.Empty,
                TagValue = tag.TagValue ?? string.Empty
            };
        }

        private static string GetRelativeTimeString(DateTime dateTime)
        {
            var now = DateTime.UtcNow;
            var timeSpan = now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes > 1 ? "s" : "")} ago";
            
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours > 1 ? "s" : "")} ago";
            
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays > 1 ? "s" : "")} ago";
            
            if (timeSpan.TotalDays < 365)
            {
                var months = (int)(timeSpan.TotalDays / 30);
                return $"{months} month{(months > 1 ? "s" : "")} ago";
            }
            
            var years = (int)(timeSpan.TotalDays / 365);
            return $"{years} year{(years > 1 ? "s" : "")} ago";
        }
    }
}
