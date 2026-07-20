# Slice #8 — Export (image / SVG / print) + rich-link copy

## Context

Eighth slice of the [Resource Explorer build](./00-implementation-plan-list.md). See the
[design spec](./resource-explorer-design-v1.md) (§9) and the master list. Depends on slice 2 (canvas);
the rich-link enhancement touches the slice-7b `ShareAsync`.

This slice adds **export**: an **Export** toolbar menu with **Copy image (PNG → clipboard)**,
**Save PNG**, **Save SVG**, and **Print → PDF**; plus the user-requested **rich-hyperlink Share copy**
(paste into Teams/Outlook shows the diagram *name* as a clickable link, not the raw URL).

**Key decisions (this slice):**

- **PNG** via Cytoscape core `cy.png(...)` (clipboard blob + file). **SVG** via the **`cytoscape-svg`**
  extension (MIT — already vendored at `wwwroot/js/explorer/vendor/cytoscape-svg.js`; self-registers
  after cytoscape). **Print/PDF** = open the SVG in a new window and `print()` (vector, clean — the
  spec's "browser Print → Save-as-PDF").
- **Rich-link copy** uses the async Clipboard API with **both** `text/html` (`<a href>` with the
  escaped diagram name) **and** `text/plain` (the raw URL) — so rich editors show "My Diagram" and
  plain-text targets still get the URL. Replaces 7b's plain `writeText`.
- **Fallible clipboard actions return a bool** from JS (no exceptions across the circuit); the page
  shows a success/failure snackbar.
- **Commit the vendored `cytoscape-svg.js`** (already in the working tree). Do NOT re-download.
- No unit tests (UI/JS); verified by driving.

---

## 1. Load the SVG extension (`UI\ResourceMapper.UI.Web\Components\App.razor`)

Add the `<script>` **after** `cytoscape-cxtmenu.js` and **before** `blazor.web.js`:

```razor
    <script src="js/explorer/vendor/cytoscape.min.js"></script>
    <script src="js/explorer/vendor/cytoscape-cxtmenu.js"></script>
    <script src="js/explorer/vendor/cytoscape-svg.js"></script>
    <script src="_framework/blazor.web.js"></script>
```

- [ ] **Step 1:** Add the one `<script>` line.

---

## 2. JS: export helpers (`wwwroot\js\explorer\explorer-canvas.js`)

Add near the end (before `dispose`). `copyPng`/`copyRichLink` return `true`/`false`; the others are
fire-and-forget downloads.

```javascript
// ---- export -------------------------------------------------------------

export async function copyPng() {
    try {
        const blob = cy.png({ output: 'blob', full: true, bg: '#ffffff', scale: 2 });
        await navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
        return true;
    } catch (e) { return false; }
}

export function savePng(filename) {
    const uri = cy.png({ output: 'base64uri', full: true, bg: '#ffffff', scale: 2 });
    downloadUri(uri, (filename || 'diagram') + '.png');
}

export function saveSvg(filename) {
    const svg = cy.svg({ full: true, bg: '#ffffff' });   // cytoscape-svg extension
    const url = URL.createObjectURL(new Blob([svg], { type: 'image/svg+xml;charset=utf-8' }));
    downloadUri(url, (filename || 'diagram') + '.svg');
    setTimeout(() => URL.revokeObjectURL(url), 5000);
}

export function printDiagram() {
    const svg = cy.svg({ full: true, bg: '#ffffff' });
    const w = window.open('', '_blank');
    if (!w) return;
    w.document.write('<!doctype html><title>Diagram</title>' + svg);
    w.document.close();
    w.focus();
    w.print();
}

// Copy a link as BOTH a rich text/html anchor (name as label) and text/plain (raw url).
export async function copyRichLink(url, text) {
    const html = '<a href="' + escapeHtml(url) + '">' + escapeHtml(text) + '</a>';
    try {
        await navigator.clipboard.write([new ClipboardItem({
            'text/html': new Blob([html], { type: 'text/html' }),
            'text/plain': new Blob([url], { type: 'text/plain' })
        })]);
        return true;
    } catch (e) {
        try { await navigator.clipboard.writeText(url); return true; }
        catch (e2) { return false; }
    }
}

function downloadUri(uri, filename) {
    const a = document.createElement('a');
    a.href = uri;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}

function escapeHtml(s) {
    return String(s)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;')
        .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
```

- [ ] **Step 2:** Add the export helpers.

---

## 3. Page: Export menu + rich Share (`Components\Pages\ResourceExplorer.razor`)

### 3a. Export menu in the toolbar

Add after the **Share** button (from 7b). **Match the existing `MudMenu` usage in the codebase**
(e.g. `Components/Layout/MainLayout.razor`) for the exact MudBlazor 9.5 menu API (activator button
props, `MudMenuItem`):

```razor
        <MudMenu Label="Export" StartIcon="@Icons.Material.Outlined.Download" Size="Size.Small"
                 Variant="Variant.Outlined" Class="ml-1">
            <MudMenuItem Icon="@Icons.Material.Outlined.ContentCopy" OnClick="CopyImageAsync">Copy image</MudMenuItem>
            <MudMenuItem Icon="@Icons.Material.Outlined.Image" OnClick="SavePngAsync">Save PNG</MudMenuItem>
            <MudMenuItem Icon="@Icons.Material.Outlined.Polyline" OnClick="SaveSvgAsync">Save SVG</MudMenuItem>
            <MudMenuItem Icon="@Icons.Material.Outlined.Print" OnClick="PrintAsync">Print / PDF</MudMenuItem>
        </MudMenu>
```

### 3b. Handlers (add beside `ShareAsync`)

```csharp
    private async Task CopyImageAsync()
    {
        var ok = await SafeInvokeResultAsync<bool>("copyPng");
        Snackbar.Add(ok ? "Image copied to clipboard." : "Copy image failed.",
            ok ? Severity.Success : Severity.Error);
    }

    private async Task SavePngAsync() => await SafeInvokeAsync("savePng", DownloadName());
    private async Task SaveSvgAsync() => await SafeInvokeAsync("saveSvg", DownloadName());
    private async Task PrintAsync() => await SafeInvokeAsync("printDiagram");

    private string DownloadName()
    {
        var name = string.IsNullOrWhiteSpace(_diagramName) ? "diagram" : _diagramName;
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(clean) ? "diagram" : clean;
    }
```
(Ensure `@using System.Linq` is available — it is via the app's `_Imports.razor`; if the compiler
disagrees, add it.)

### 3c. Upgrade `ShareAsync` to the rich copy

In the `ShareAsync` method (from 7b), replace the clipboard line:

```csharp
        // was: await JS.InvokeVoidAsync("navigator.clipboard.writeText", url); + snackbar
        var label = string.IsNullOrWhiteSpace(_diagramName) ? url : _diagramName;
        var ok = await SafeInvokeResultAsync<bool>("copyRichLink", url, label);
        Snackbar.Add(ok ? "Share link copied." : url, ok ? Severity.Success : Severity.Info);
```

- [ ] **Step 3:** Apply 3a–3c.

---

## UI verification hook (visible slice)

- [ ] **Step 4: Build + run.** `dotnet build` clean; `dotnet run --project UI/ResourceMapper.UI.Web`.
- [ ] **Step 5: Drive it** at `/explore/DEMOEXP-checkout` (expand a few nodes first):
  1. **Export → Copy image** → paste into an image-accepting target (e.g. an image editor, or Teams
     message) → the diagram appears as a PNG; a "copied" snackbar shows.
  2. **Export → Save PNG** → a `<diagramname>.png` (or `diagram.png`) downloads and opens as the graph.
  3. **Export → Save SVG** → a `.svg` downloads; opening it in a browser shows a crisp **vector** graph.
  4. **Export → Print / PDF** → a new window opens with the SVG and the print dialog; "Save as PDF"
     yields a vector PDF.
  5. **Save-As** a diagram named e.g. "My Diagram", then **Share** → paste into a **rich** editor
     (Teams/Outlook/Word) → it shows a clickable **"My Diagram"** link; paste into a **plain-text**
     field → the raw URL. (Filenames from Save PNG/SVG also reflect the diagram name.)
- [ ] **Step 6 (optional): Playwright** — assert `savePng`/`saveSvg` trigger a download event, and that
  `copyRichLink` puts `text/html` on the clipboard (read back via `navigator.clipboard.read()`).

---

## Verification

1. `dotnet build` clean (sqlproj `MSB4278` aside); `dotnet test` unchanged (no new unit tests).
2. Manual browser drive per Steps 4–5 — the gate. (Clipboard image/HTML writes require a secure
   context — dev runs over https/localhost, which qualifies.)

---

## Out of scope (later slices)

- **Visual polish** — dashed/thinner edges, 1.25rem labels, zoom/pan controls, floating-overlay
  toolbar, menu restyle → slice 9.
- **Client settings migration** → slice 10.

---

## Execution notes

_(Written after execution — record clipboard/image support observed, the SVG extension registration,
the rich-link paste result (Teams/rich editor), and commit hash(es). Then mark slice #8 done /
slice #9 next in the master list, and commit.)_
