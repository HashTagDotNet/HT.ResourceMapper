# Slice #6 — Client identity (localStorage)

## Context

Sixth slice of the [Resource Explorer build](./00-implementation-plan-list.md). See the
[design spec](./resource-explorer-design-v1.md) (§7.1) and the master list. Depends on slice 2
(the explorer page). Small slice — a reusable identity helper the persist/share UI (slice 7) and the
settings migration (slice 9) will use.

This slice gives the app an **anonymous durable `clientId`**: a GUID kept in the browser's
`localStorage`, read/written via JS interop (the same `localStorage.getItem/setItem` mechanism
`Home.razor` already uses — **no cookie, no middleware**, because `HttpContext` is `null` in the
interactive circuit, design §7.1). The `clientId` is obtained at the UI layer and will be passed as a
parameter into `IDiagramService` (slice 5) by slice 7.

**Key decisions (this slice):**

- **No new JS module** — the helper calls the built-in `localStorage.getItem/setItem` via `IJSRuntime`
  (deferred past prerender, in `OnAfterRenderAsync`).
- **GUID generated in C#** (`Guid.NewGuid().ToString("N")`) then persisted — keeps id generation in one
  place and consistent with the diagram ids.
- **Reusable static helper** `ClientIdentity` in the Web project (UI-layer; no test project exists for
  UI, and the logic is a thin interop wrapper — verified by driving).
- **Visible hook (interim):** the explorer page shows a truncated `Client: xxxxxxxx…` caption with a
  **copy** button (the "recovery key" — copy your id to carry it to another browser). Slice 7 relocates
  this into the toolbar/menu.
- **No consumer yet** — nothing is saved until slice 7; this slice just makes the id available + visible.

---

## 1. The identity helper (`UI\ResourceMapper.UI.Web\Explorer\ClientIdentity.cs`)

New `Explorer\` folder in the Web project; namespace `ResourceMapper.UI.Web.Explorer`.

```csharp
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
```

- [ ] **Step 1:** Create `ClientIdentity.cs`.

---

## 2. Wire it into the explorer page (`Components\Pages\ResourceExplorer.razor`)

### 2a. Obtain the id after init

Add the `@using` (with the other usings at the top):

```razor
@using ResourceMapper.UI.Web.Explorer
```

Add the field (beside `_preset`):

```csharp
    private string _clientId = string.Empty;
```

In `OnAfterRenderAsync`, after `await SafeInvokeAsync("setPreset", _preset);` (before/after the seed
load is fine), obtain the id and re-render so the caption shows:

```csharp
        _clientId = await ClientIdentity.GetOrCreateAsync(JS);
        StateHasChanged();
```

### 2b. Caption + copy in the control bar

At the end of the `<div class="rm-explorer-bar"> … </div>` (after the existing hint `MudText`), add a
right-aligned client caption + copy button:

```razor
        <MudSpacer />
        @if (!string.IsNullOrEmpty(_clientId))
        {
            <MudText Typo="Typo.caption" Class="mud-text-secondary mr-1">
                Client: @(_clientId.Length >= 8 ? _clientId[..8] : _clientId)…
            </MudText>
            <MudTooltip Text="Copy your client id (carry it to another browser)">
                <MudIconButton Icon="@Icons.Material.Outlined.ContentCopy" Size="Size.Small"
                               aria-label="Copy client id" OnClick="CopyClientIdAsync" />
            </MudTooltip>
        }
```

### 2c. Copy handler

Add beside `OnPresetChanged`:

```csharp
    private async Task CopyClientIdAsync()
    {
        if (string.IsNullOrEmpty(_clientId)) return;
        try { await JS.InvokeVoidAsync("navigator.clipboard.writeText", _clientId); }
        catch { /* clipboard unavailable */ }
    }
```

- [ ] **Step 2:** Apply 2a–2c.

---

## UI verification hook (visible slice)

- [ ] **Step 3: Build + run.** `dotnet build` clean; `dotnet run --project UI/ResourceMapper.UI.Web`.
- [ ] **Step 4: Drive it.** Explore any resource, then confirm:
  1. The control bar shows `Client: xxxxxxxx…` (first 8 chars of a 32-char GUID).
  2. **Reload** the page → the **same** id (persisted in `localStorage`).
  3. The **copy** button copies the full id to the clipboard (paste to check).
  4. In the browser devtools, `localStorage.getItem('rm_client_id')` returns the full id; clearing it
     and reloading yields a **new** id (accepted trade-off — design §7.1).

---

## Verification

1. `dotnet build` clean (sqlproj `MSB4278` aside); `dotnet test` unchanged (no new unit tests — UI-layer helper; no UI test project).
2. Manual browser drive per Steps 3–4 — the gate.

---

## Out of scope (later slices)

- **Passing `clientId` into `IDiagramService`** (Save/List/Delete/SaveCopy) → **slice 7**. This slice
  only obtains and displays it.
- **Recovery-key restore UI** (an input to paste an id and call `ClientIdentity.SetAsync`) → slice 7 or
  later; `SetAsync` is provided now for it.
- **Relocating the caption/copy into the toolbar or a menu** → slice 7 (this bar placement is interim).
- **Settings migration reuse** of this helper → slice 9.

---

## Execution notes

_(Written after execution — record interop behavior, persistence across reload actually observed, and
commit hash(es). Then mark slice #6 **Done** ✓ / slice #7 **Next** in the master list, and commit.)_
