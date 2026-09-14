using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using HT.Api.Service.Contracts.BuildersOfT;
using ResourceMapper.Common.Server.SavedViews.Interfaces;
using ResourceMapper.Common.Shared.SavedViews;
using ResourceMapper.Common.Shared.SavedViews.Contracts;

namespace ResourceMapper.Common.Server.SavedViews
{
    /// <summary>
    /// Validates, then delegates. The owner arrives as a parameter rather than being resolved here,
    /// so the caller is the single place that touches ICurrentIdentity.
    /// </summary>
    public class SavedViewService : ISavedViewService
    {
        private const int MaxNameLength = 200;

        private readonly ISavedViewRepository _repo;

        public SavedViewService(ISavedViewRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiServiceResponse<List<SavedViewModel>>> ListAsync(
            string ownerId, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<List<SavedViewModel>>();
            try
            {
                if (string.IsNullOrWhiteSpace(ownerId))
                {
                    builder.Validation.AddValidation("ownerId", "Is required");
                    return builder.BuildResponse();
                }

                builder.Data.Set(await _repo.ListAsync(ownerId, cancellationToken));
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, ex.Message);
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<SavedViewModel>> SaveAsync(
            string ownerId, SaveViewRequest request, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<SavedViewModel>();
            try
            {
                if (string.IsNullOrWhiteSpace(ownerId))
                    builder.Validation.AddValidation("ownerId", "Is required");

                if (request is null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                var name = (request.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                    builder.Validation.AddValidation("name", "Is required");
                else if (name.Length > MaxNameLength)
                    builder.Validation.AddValidation("name", $"Must be {MaxNameLength} characters or fewer");

                // QueryString is deliberately NOT length-capped: four filters over the catalog's
                // highest-cardinality tags already exceed 2000 characters, and the column is
                // NVARCHAR(MAX) for exactly that reason.
                if (string.IsNullOrWhiteSpace(request.QueryString))
                    builder.Validation.AddValidation("queryString", "Is required");

                if (!builder.IsOk) return builder.BuildResponse();

                var uid = string.IsNullOrWhiteSpace(request.SavedViewUid)
                    ? "sv-" + Guid.NewGuid().ToString("N")
                    : request.SavedViewUid;

                var (result, savedUid) = await _repo.UpsertAsync(
                    ownerId, uid, name, request.QueryString, cancellationToken);

                if (string.Equals(result, "denied", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "That saved view belongs to someone else.");
                    return builder.BuildResponse();
                }

                builder.Data.Set(new SavedViewModel
                {
                    SavedViewUid = savedUid,
                    Name = name,
                    QueryString = request.QueryString
                });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, ex.Message);
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<object>> DeleteAsync(
            string ownerId, string savedViewUid, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(ownerId))
                    builder.Validation.AddValidation("ownerId", "Is required");
                if (string.IsNullOrWhiteSpace(savedViewUid))
                    builder.Validation.AddValidation("savedViewUid", "Is required");
                if (!builder.IsOk) return builder.BuildResponse();

                var deleted = await _repo.DeleteAsync(ownerId, savedViewUid, cancellationToken);
                if (deleted == 0)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "That saved view no longer exists.");
                    return builder.BuildResponse();
                }

                builder.Data.Set(new object());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, ex.Message);
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<object>> SetDefaultAsync(
            string ownerId, string? savedViewUid, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(ownerId))
                {
                    builder.Validation.AddValidation("ownerId", "Is required");
                    return builder.BuildResponse();
                }

                // A null uid is legitimate: it clears the default, leaving the grid to fall back to
                // the resume setting.
                await _repo.SetDefaultAsync(ownerId, savedViewUid, cancellationToken);
                builder.Data.Set(new object());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, ex.Message);
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<object>> ReorderAsync(
            string ownerId, IReadOnlyList<string> uidsInOrder, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(ownerId))
                {
                    builder.Validation.AddValidation("ownerId", "Is required");
                    return builder.BuildResponse();
                }

                // Reordering nothing is a no-op, not a failure — and sending an empty TVP would
                // update no rows anyway.
                if (uidsInOrder is { Count: > 0 })
                    await _repo.ReorderAsync(ownerId, uidsInOrder, cancellationToken);

                builder.Data.Set(new object());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, ex.Message);
                return builder.BuildResponse();
            }
        }
    }
}
