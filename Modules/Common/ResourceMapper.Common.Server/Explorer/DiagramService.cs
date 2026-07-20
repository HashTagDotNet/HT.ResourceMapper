using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Client.Contracts.Models;                 // CallStatusCode
using HT.Api.Service.Contracts;                       // ApiServiceResponse<T>
using HT.Api.Service.Contracts.BuildersOfT;           // ServiceResponseBuilder<T>
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Shared.Explorer.Diagrams;
using ResourceMapper.Common.Shared.Explorer.Diagrams.Contracts;

namespace ResourceMapper.Common.Server.Explorer
{
    public class DiagramService : IDiagramService
    {
        private readonly IDiagramRepository _repo;

        public DiagramService(IDiagramRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiServiceResponse<SaveDiagramResponse>> SaveAsync(string clientId, SaveDiagramRequest request, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<SaveDiagramResponse>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }
                if (request is null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                var invalid = false;
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    builder.Validation.AddValidation("request.Name", "Name is required");
                    invalid = true;
                }
                if (string.IsNullOrWhiteSpace(request.SeedResourceUid))
                {
                    builder.Validation.AddValidation("request.SeedResourceUid", "Seed resource is required");
                    invalid = true;
                }
                if (invalid) return builder.BuildResponse();

                var isNew = string.IsNullOrWhiteSpace(request.DiagramUid);
                var diagramUid = isNew ? NewId() : request.DiagramUid!;
                var shareCandidate = NewId();   // used only if inserting; Upsert returns the real one

                var result = await _repo.UpsertAsync(diagramUid, shareCandidate, clientId,
                    request.Name, request.SeedResourceUid,
                    string.IsNullOrWhiteSpace(request.DisplayPreset) ? "nameType" : request.DisplayPreset,
                    request.DiagramJson ?? string.Empty, cancellationToken);

                if (result.Result == "denied")
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Diagram Not Found",
                        $"No diagram '{request.DiagramUid}' owned by this client.", "request.DiagramUid");
                    return builder.BuildResponse();
                }

                builder.Data.Set(new SaveDiagramResponse
                {
                    DiagramUid = result.DiagramUid,
                    ShareId = result.ShareId,
                    Result = result.Result
                });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<List<DiagramListItem>>> ListForClientAsync(string clientId, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<List<DiagramListItem>>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }

                var rows = await _repo.ListForClientAsync(clientId, cancellationToken);
                builder.Data.Set(rows.Select(r => new DiagramListItem
                {
                    DiagramUid = r.DiagramUid,
                    ShareId = r.ShareId,
                    Name = r.Name,
                    SeedResourceUid = r.SeedResourceUid,
                    UpdatedOnUtc = r.UpdatedOnUtc
                }).ToList());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<DiagramModel>> GetByShareIdAsync(string shareId, string? callerClientId, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<DiagramModel>();
            try
            {
                if (string.IsNullOrWhiteSpace(shareId))
                {
                    builder.Validation.AddValidation("shareId", "Share id is required");
                    return builder.BuildResponse();
                }

                var row = await _repo.GetByShareIdAsync(shareId, cancellationToken);
                if (row is null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Diagram Not Found",
                        $"No diagram for share id '{shareId}'.", "shareId");
                    return builder.BuildResponse();
                }

                var model = ToModel(row);
                model.IsOwner = !string.IsNullOrEmpty(callerClientId) && row.ClientId == callerClientId;
                if (!model.IsOwner)
                {
                    model.DiagramUid = string.Empty;   // don't leak the owner handle to a read-only recipient
                }

                builder.Data.Set(model);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<SaveDiagramResponse>> SaveCopyAsync(string clientId, string shareId, string? newName, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<SaveDiagramResponse>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }
                if (string.IsNullOrWhiteSpace(shareId))
                {
                    builder.Validation.AddValidation("shareId", "Share id is required");
                    return builder.BuildResponse();
                }

                var src = await _repo.GetByShareIdAsync(shareId, cancellationToken);
                if (src is null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Diagram Not Found",
                        $"No diagram for share id '{shareId}'.", "shareId");
                    return builder.BuildResponse();
                }

                var name = string.IsNullOrWhiteSpace(newName) ? src.Name + " (copy)" : newName!;
                var result = await _repo.UpsertAsync(NewId(), NewId(), clientId,
                    name, src.SeedResourceUid, src.DisplayPreset, src.DiagramJson, cancellationToken);

                builder.Data.Set(new SaveDiagramResponse
                {
                    DiagramUid = result.DiagramUid,
                    ShareId = result.ShareId,
                    Result = result.Result
                });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<object>> DeleteAsync(string clientId, string diagramUid, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }
                if (string.IsNullOrWhiteSpace(diagramUid))
                {
                    builder.Validation.AddValidation("diagramUid", "Diagram id is required");
                    return builder.BuildResponse();
                }

                var deleted = await _repo.DeleteAsync(clientId, diagramUid, cancellationToken);
                if (!deleted)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Diagram Not Found",
                        $"No diagram '{diagramUid}' owned by this client.", "diagramUid");
                    return builder.BuildResponse();
                }

                builder.Data.Set(new object());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        private static string NewId() => Guid.NewGuid().ToString("N");

        private static DiagramModel ToModel(Models.DiagramRow row) => new()
        {
            DiagramUid = row.DiagramUid,
            ShareId = row.ShareId,
            Name = row.Name,
            SeedResourceUid = row.SeedResourceUid,
            DisplayPreset = row.DisplayPreset,
            DiagramJson = row.DiagramJson,
            UpdatedOnUtc = row.UpdatedOnUtc
        };
    }
}
