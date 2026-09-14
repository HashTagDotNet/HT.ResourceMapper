// Minimal client-side file download for Blazor Interactive Server.
//
// Loaded as an ES module via IJSRuntime.InvokeAsync<IJSObjectReference>("import", ...) rather than
// a <script> tag in App.razor, so it costs nothing on pages that never download anything.
//
// The text arrives over the SignalR circuit and is turned into a Blob object URL here; that avoids
// a data: URI (which browsers cap in length) and avoids needing a server endpoint just to serve
// bytes the server already handed us.
export function downloadText(fileName, text, mimeType) {
    const blob = new Blob([text], { type: mimeType || 'application/json' });
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.style.display = 'none';

    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();

    // Revoke on the next tick: revoking synchronously can cancel the download in some browsers.
    setTimeout(() => URL.revokeObjectURL(url), 5000);
}
