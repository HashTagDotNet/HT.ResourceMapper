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
        /// <summary>A reorder covers one owner's whole list; far more is a malformed request.</summary>
        private const int MaxReorderCount = 500;

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
                SavedViewValidation.ValidateOwnerId(builder, ownerId);
                if (!builder.IsOk) return builder.BuildResponse();

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
                if (request is null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                SavedViewValidation.ValidateOwnerId(builder, ownerId);
                var name = SavedViewValidation.ValidateName(builder, request.Name);
                SavedViewValidation.ValidateQueryString(builder, request.QueryString);
                SavedViewValidation.ValidateUid(builder, request.SavedViewUid, required: false);

                if (!builder.IsOk) return builder.BuildResponse();

                var uid = string.IsNullOrWhiteSpace(request.SavedViewUid)
                    ? "sv-" + Guid.NewGuid().ToString("N")
                    : request.SavedViewUid;

                var (result, savedUid) = await _repo.UpsertAsync(
                    ownerId, uid, name, request.QueryString, cancellationToken);

                if (string.Equals(result, "duplicate", StringComparison.OrdinalIgnoreCase))
                {
                    // Detected in the procedure, so two concurrent saves cannot both slip past a
                    // check-then-write and have the loser fail on the unique constraint as a 500.
                    builder.Validation.AddValidation("name", "A saved view with that name already exists");
                    return builder.BuildResponse();
                }

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
                SavedViewValidation.ValidateOwnerId(builder, ownerId);
                SavedViewValidation.ValidateUid(builder, savedViewUid, required: true);
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
                SavedViewValidation.ValidateOwnerId(builder, ownerId);
                // A null/empty uid is legitimate here: it clears the default.
                SavedViewValidation.ValidateUid(builder, savedViewUid, required: false);
                if (!builder.IsOk) return builder.BuildResponse();

                // A null uid clears the default, leaving the grid to fall back to the resume
                // setting.
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
                SavedViewValidation.ValidateOwnerId(builder, ownerId);

                // Every uid is checked: the list arrives straight from an HTTP body, so it is the
                // least trustworthy input this service takes.
                if (uidsInOrder is { Count: > 0 })
                {
                    if (uidsInOrder.Count > MaxReorderCount)
                        builder.Validation.AddValidation("uidsInOrder",
                            $"Must contain {MaxReorderCount} entries or fewer");

                    foreach (var uid in uidsInOrder)
                        SavedViewValidation.ValidateUid(builder, uid, required: true);

                    if (uidsInOrder.Distinct(StringComparer.OrdinalIgnoreCase).Count() != uidsInOrder.Count)
                        builder.Validation.AddValidation("uidsInOrder", "Must not repeat a saved view");
                }

                if (!builder.IsOk) return builder.BuildResponse();

                // Reordering nothing is a no-op, not a failure - and an empty TVP updates no rows.
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
