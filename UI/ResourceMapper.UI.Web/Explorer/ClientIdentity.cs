using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace ResourceMapper.UI.Web.Explorer
{
    /// <summary>
    /// Anonymous per-client identity for the explorer: a durable GUID kept in the browser's
    /// localStorage (read/written via JS interop — the same mechanism Home.razor uses). There is no
    /// login; this handle owns a client's saved diagrams. Call only after the circuit is interactive
    /// (localStorage is unavailable during prerender).
    /// </summary>
    public static class ClientIdentity
    {
        public const string StorageKey = "rm_client_id";

        /// <summary>Returns the existing client id, or creates and persists a new one.</summary>
        public static async Task<string> GetOrCreateAsync(IJSRuntime js)
        {
            string? id = null;
            try { id = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey); }
            catch { /* storage unavailable (prerender / private mode) */ }

            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString("N");
                try { await js.InvokeVoidAsync("localStorage.setItem", StorageKey, id); }
                catch { /* best-effort; a non-persisted id still works for this session */ }
            }
            return id!;
        }

        /// <summary>Overwrites the stored client id (recovery-key restore).</summary>
        public static async Task SetAsync(IJSRuntime js, string id)
        {
            try { await js.InvokeVoidAsync("localStorage.setItem", StorageKey, id); }
            catch { /* best-effort */ }
        }
    }
}
