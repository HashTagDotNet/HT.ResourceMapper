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
                if (builder.IsOk == false)
                {
                    return builder.BuildResponse();
                }

                var repositoryResult = await _repo.GetResourceGridItemsAsync(
                    request.SearchFor, 
                    request.OrderBy, 
                    request.OrderDirection, 
                    request.Skip, 
                    request.Take, 
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
                Code = new(),
                Name = new(),
                Notes = new(),
                ResourceType = new(),
                ResourceUid = Guid.NewGuid().ToString(),
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
