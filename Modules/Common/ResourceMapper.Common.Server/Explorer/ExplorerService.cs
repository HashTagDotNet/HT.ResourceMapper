using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Client.Contracts.Models;                 // CallStatusCode
using HT.Api.Service.Contracts;                       // ApiServiceResponse<T>
using HT.Api.Service.Contracts.BuildersOfT;           // ServiceResponseBuilder<T>
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Shared.Explorer;

namespace ResourceMapper.Common.Server.Explorer
{
    public class ExplorerService : IExplorerService
    {
        private readonly IExplorerRepository _repo;

        public ExplorerService(IExplorerRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiServiceResponse<ExplorerNodeModel>> GetNodeAsync(string resourceUid, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ExplorerNodeModel>();
            try
            {
                if (string.IsNullOrWhiteSpace(resourceUid))
                {
                    builder.Validation.AddValidation("resourceUid", "Resource UID is required");
                    return builder.BuildResponse();
                }

                var rows = await _repo.GetForExplorerAsync(resourceUid, cancellationToken);
                var self = rows.FirstOrDefault(r => r.Direction == "Self");
                if (self == null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Resource Not Found",
                        $"Resource with UID '{resourceUid}' was not found.", "resourceUid");
                    return builder.BuildResponse();
                }

                var model = new ExplorerNodeModel
                {
                    ResourceUid = self.ResourceUid,
                    ResourceKey = self.ResourceKey,
                    ResourceName = self.ResourceName,
                    ResourceType = self.ResourceType,
                    ShortCode = self.ShortCode,
                    IconKey = self.IconKey,
                    Domain = self.Domain,
                    PrimaryUrl = self.PrimaryUrl,
                    Neighbors = rows
                        .Where(r => r.Direction != "Self")
                        .Select(r => new ExplorerNeighborModel
                        {
                            Direction = r.Direction,
                            ResourceUid = r.ResourceUid,
                            ResourceKey = r.ResourceKey,
                            ResourceName = r.ResourceName,
                            ResourceType = r.ResourceType,
                            ShortCode = r.ShortCode,
                            IconKey = r.IconKey,
                            Domain = r.Domain,
                            PrimaryUrl = r.PrimaryUrl
                        })
                        .ToList()
                };

                builder.Data.Set(model);
                return builder.BuildResponse();
            }
            catch (System.Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }
    }
}
