// Explorer canvas — thin wrapper over Cytoscape (global `cytoscape`) + the cxtmenu extension.
// One instance per page. Graph state lives in the browser; C# pushes graph batches (addGraph) and
// receives node events (tap / menu) via the DotNetObjectReference.
//
// Node rendering is entirely canvas-drawn (background-color + background-image(s) + label) so it
// is captured by cy.png()/cy.svg() exports — HTML-overlay nodes would not be. See
// docs/plans/explorer/09c-node-layout.md for the technique.

let cy = null;
let dotNet = null;
let seedId = null;
let tip = null;
let menu = null;
let overlayEl = null;
let overlayTitle = '';
let overlaySub = '';

// Node geometry. The circle has to hold the type icon AND the short code without them colliding,
// so the icon sits in the upper third (background-position-y percent) and the code label is pushed
// into the lower third (text-margin-y). Widened from 46px to give both room.
const NODE_SIZE = 54;
const ICON_SIZE = 20;
const CODE_MARGIN_Y = 11;

// cy.fit() zoom ceiling. Without it, fitting a one-node graph (e.g. straight after "Collapse all")
// runs to maxZoom and every later expansion is placed inside that hugely magnified frame.
const FIT_MAX_ZOOM = 1.2;

// ---- icon + color maps, data-URI helpers --------------------------------

// White type-icon glyphs keyed by ResourceType.IconKey (from slice 9b). Minimal, schematic.
const ICON_PATHS = {
    web:      '<rect x="3" y="4" width="18" height="13" rx="2"/><rect x="8" y="19" width="8" height="2"/>',
    apps:     '<rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>',
    settings: '<circle cx="12" cy="12" r="6" fill="none" stroke="#fff" stroke-width="2.4"/><circle cx="12" cy="12" r="2"/>',
    insights: '<rect x="3" y="13" width="4" height="8"/><rect x="10" y="8" width="4" height="13"/><rect x="17" y="3" width="4" height="18"/>',
    memory:   '<ellipse cx="12" cy="6" rx="8" ry="3"/><path d="M4,6 v12 a8,3 0 0 0 16,0 v-12" fill="none" stroke="#fff" stroke-width="2.2"/>',
    database: '<ellipse cx="12" cy="6" rx="8" ry="3"/><path d="M4,6 v12 a8,3 0 0 0 16,0 v-12" fill="none" stroke="#fff" stroke-width="2.2"/>',
    bus:      '<rect x="3" y="6" width="18" height="4" rx="1"/><rect x="3" y="14" width="18" height="4" rx="1"/>',
    queue:    '<rect x="3" y="5" width="18" height="3"/><rect x="3" y="10.5" width="18" height="3"/><rect x="3" y="16" width="18" height="3"/>',
    hub:      '<circle cx="12" cy="12" r="3"/><circle cx="4" cy="4" r="2.4"/><circle cx="20" cy="4" r="2.4"/><circle cx="4" cy="20" r="2.4"/><circle cx="20" cy="20" r="2.4"/>',
    folder:   '<path d="M3,6 h6 l2,2 h10 v11 h-18 z"/>',
    dns:      '<circle cx="12" cy="12" r="8" fill="none" stroke="#fff" stroke-width="2.2"/><path d="M4,12 h16 M12,4 a12,8 0 0 0 0,16 a12,8 0 0 0 0,-16" fill="none" stroke="#fff" stroke-width="1.6"/>'
};

const TYPE_COLORS = {
    web:'#2563EB', apps:'#2563EB', insights:'#7c3aed', memory:'#dc2626', database:'#4338ca',
    bus:'#ea580c', queue:'#ea580c', settings:'#0d9488', hub:'#0891b2', folder:'#64748b', dns:'#0d9488'
};
function nodeColor(iconKey) { return TYPE_COLORS[iconKey] || '#2563EB'; }

function svgDataUri(svg) {
    return 'data:image/svg+xml;utf8,' + encodeURIComponent(svg);
}

const TRANSPARENT_PX = svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="1" height="1"></svg>');

// Escapes text destined for SVG/HTML markup. Used by every string-built markup helper below.
function escapeHtml(s) {
    return String(s)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;')
        .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// The white type icon (or a neutral dot if the key is unknown / missing).
function iconUri(iconKey) {
    const body = ICON_PATHS[iconKey] || '<circle cx="12" cy="12" r="4"/>';
    return svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="#fff">' + body + '</svg>');
}

// The name (14 bold) + type (12) caption drawn as an SVG image (export-safe, two font sizes).
// Sized/positioned to sit BELOW the node circle — see the node style's background-position-y /
// bounds-expansion (tuned empirically; cytoscape's background-position-y percent is a "travel
// range" of (nodeHeight - imageHeight), which is too narrow to place a near-node-height image
// below the node, hence the caption uses a fixed px offset instead of a percent).
function captionUri(name, type) {
    const w = 180, h = 36;
    return svgDataUri(
        '<svg xmlns="http://www.w3.org/2000/svg" width="' + w + '" height="' + h + '">' +
        '<text x="' + (w/2) + '" y="15" text-anchor="middle" font-family="system-ui,sans-serif" font-size="14" font-weight="700" fill="#0f172a">' + escapeHtml(name || '') + '</text>' +
        '<text x="' + (w/2) + '" y="30" text-anchor="middle" font-family="system-ui,sans-serif" font-size="12" fill="#64748b">' + escapeHtml(type || '') + '</text>' +
        '</svg>');
}

// Recompute a node's bg images from its data + labeled state.
function nodeBgImages(n) {
    const d = n.data();
    const caption = n.hasClass('labeled') ? captionUri(d.name, d.type) : TRANSPARENT_PX;
    return [d._icon, caption];
}
function refreshNode(n) { n.data('bgImages', nodeBgImages(n)); }

export function init(hostEl, dotNetRef) {
    dotNet = dotNetRef;
    cy = cytoscape({
        container: hostEl,
        elements: [],
        style: [
            { selector: 'node', style: {
                'shape': 'ellipse',
                'background-color': 'data(color)',
                'width': NODE_SIZE, 'height': NODE_SIZE,
                'border-width': 2, 'border-color': '#1e293b',
                // The short code and the type icon must BOTH be legible, so they are separated
                // vertically inside the circle rather than both being centred (which drew the code
                // on top of the icon and made every node an unreadable glyph):
                //   icon  — background image #1, upper third (background-position-y below)
                //   code  — the node label, pushed into the lower third by text-margin-y
                'label': 'data(code)',
                'color': '#fff',
                'font-size': '13px',
                'font-weight': 700,
                'text-valign': 'center',
                'text-halign': 'center',
                'text-margin-y': CODE_MARGIN_Y,
                // two background images: [type icon (always), name/type caption (labeled only)].
                // Position/size tuned against the geometry constants above: the icon's percent is a
                // "travel range" of (nodeHeight - imageHeight), so a small percent parks it near the
                // top; the caption is anchored by a fixed px offset (not percent — see captionUri's
                // comment) so it sits just below the circle with a small gap.
                'background-image': 'data(bgImages)',
                'background-image-crossorigin': 'anonymous',
                'background-width':  [ICON_SIZE + 'px', '180px'],
                'background-height': [ICON_SIZE + 'px', '36px'],
                'background-position-x': ['50%', '50%'],
                'background-position-y': ['10%', (NODE_SIZE + 4) + 'px'],
                'background-clip': ['none', 'none'],
                'background-image-containment': ['inside', 'over']
            }},
            // bounds-expansion [top,right,bottom,left] fits the wide caption's left/right overhang
            // and its below-node extent so renders/exports don't clip it. It applies ONLY to nodes
            // that actually carry a caption: it inflates the node's bounding box, and cy.fit() sums
            // those boxes, so applying it to every node zoomed the whole graph out several times
            // further than the picture needs (which is what made the arrowheads vanish).
            { selector: 'node.labeled',  style: { 'bounds-expansion': [8, 70, 45, 70] }},
            { selector: 'node.seed',     style: { 'border-color': '#f59e0b', 'border-width': 4 }},
            { selector: 'node.expanded', style: { 'border-style': 'double' }},
            { selector: 'edge', style: {
                'width': 2,
                'line-color': '#94a3b8',
                'target-arrow-color': '#94a3b8',
                'target-arrow-shape': 'triangle',
                'arrow-scale': 1.3,
                'curve-style': 'bezier'
            }},
            { selector: 'edge.upstream', style: {
                'line-style': 'dashed'
            }}
        ],
        layout: { name: 'grid' },
        minZoom: 0.2, maxZoom: 3, wheelSensitivity: 0.2
    });

    // Tap toggles expand/collapse (remove/open actions live in the right-click menu).
    cy.on('tap', 'node', evt => {
        dotNet.invokeMethodAsync('OnNodeTapped', evt.target.id());
    });

    // Captions no longer depend on hover/selection (see applyLabeled), so these events must NOT
    // call it any more: applyLabeled() walks every node and regenerates its caption data-URI, so
    // driving it from mouseover meant re-rasterising the whole graph on every pointer move.
    // Hover still drives the tooltip — see initTooltip.

    initTooltip(hostEl);
    initMenu();

    overlayEl = document.createElement('div');
    overlayEl.className = 'rm-explorer-title';
    hostEl.appendChild(overlayEl);
    renderOverlay();
}

// ---- title/date overlay (also composited into PNG/SVG/print exports) ----

export function setTitle(title, subtitle) {
    overlayTitle = title || '';
    overlaySub = subtitle || '';
    renderOverlay();
}

function subWithCount() {
    const n = cy ? cy.nodes().length : 0;
    const base = overlaySub || '';
    return base + (base ? ' · ' : '') + n + ' resource' + (n === 1 ? '' : 's');
}

function renderOverlay() {
    if (!overlayEl) return;
    overlayEl.textContent = '';
    if (overlayTitle) {
        const h = document.createElement('div');
        h.className = 'rm-title-h';
        h.textContent = overlayTitle;
        overlayEl.appendChild(h);
    }
    const s = document.createElement('div');
    s.className = 'rm-title-sub';
    s.textContent = subWithCount();
    overlayEl.appendChild(s);
    if (hasEdges()) {
        const l = document.createElement('img');
        l.className = 'rm-title-legend';
        l.setAttribute('alt', 'Solid arrow: the focus depends on. Dashed arrow: depends on the focus.');
        l.setAttribute('src', legendUri());
        l.setAttribute('width', LEGEND_W);
        l.setAttribute('height', LEGEND_H);
        overlayEl.appendChild(l);
    }
    overlayEl.style.display = (overlayTitle || overlaySub) ? 'block' : 'none';
}

// ---- edge-style legend (screen + composited into every export) -----------
// Edge dashing is seed-relative and carries real meaning (see applyEdgeStyles): dashed edges lie
// on a path INTO the seed — things that depend on it; solid edges lead away — things it depends on.
// One SVG fragment is the single source of truth so the on-screen legend, the PNG header band, the
// SVG header band and the print view can't disagree. cy.png()/cy.svg() capture the graph only, so
// the legend has to be composited in alongside the title (same technique as slice 9d).

const LEGEND_W = 168, LEGEND_H = 38;

function hasEdges() { return !!cy && cy.edges().length > 0; }

// x/y position the fragment within whatever surface it is dropped into.
function legendSvgInner(x, y) {
    const row = (cy1, dash, text) =>
        '<line x1="4" y1="' + cy1 + '" x2="30" y2="' + cy1 + '" stroke="#94a3b8" stroke-width="2"' +
        (dash ? ' stroke-dasharray="5,4"' : '') + ' marker-end="url(#rmLegendArrow)"/>' +
        '<text x="42" y="' + (cy1 + 4) + '">' + text + '</text>';
    return '<g transform="translate(' + x + ',' + y + ')" font-family="system-ui,sans-serif" font-size="11" fill="#475569">' +
        '<defs><marker id="rmLegendArrow" viewBox="0 0 8 8" refX="7" refY="4" markerWidth="5" markerHeight="5" orient="auto">' +
        '<path d="M0,0 L8,4 L0,8 z" fill="#94a3b8"/></marker></defs>' +
        row(10, false, 'Focus depends on') +
        row(28, true,  'Depends on focus') +
        '</g>';
}

function legendUri() {
    return svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="' + LEGEND_W + '" height="' + LEGEND_H + '">' +
        legendSvgInner(0, 0) + '</svg>');
}

// ---- labeled state (caption visibility) ---------------------------------

// EVERY node carries its name/type caption (user decision, 2026-08-03 — supersedes pass 2's
// seed+hover+selected-only rule, which existed to avoid the label overlap of finding #1).
// Viable now because two things changed since: the icon/code no longer collide inside the node
// (PL-37), and the layered layout spreads nodes instead of stacking them in one row (PL-39).
// Consequence: 'node.labeled' bounds-expansion now applies to every node — that is correct here,
// since every node really does have caption overhang, but it means cy.fit() frames the captions
// too and so zooms slightly further out than a caption-less graph would.
function applyLabeled() {
    cy.nodes().forEach(n => {
        n.addClass('labeled');
        refreshNode(n);
    });
}

// Dash "upstream" edges (things that depend on the seed). Recompute after any graph change.
function applyEdgeStyles() {
    cy.edges().removeClass('upstream');
    if (!seedId) return;
    const seed = cy.getElementById(seedId);
    if (seed.empty()) return;
    seed.predecessors('edge').addClass('upstream');   // edges leading INTO the seed = upstream
}

// ---- tooltip (interactive card: metadata + action links; XSS-safe) -------
// Node hover -> metadata + clickable "Open in Azure" / "Open in Resource Mapper" (real anchors:
// discoverable, launch reliably without popup-block, open new tabs so the explorer stays put).
// Edge hover -> 3-line "source / depends on / target". The card is interactive: it lingers while
// the cursor is on it (delayed hide) so links are clickable.

let tipHideTimer = null;

function initTooltip(container) {
    tip = document.createElement('div');
    tip.className = 'rm-explorer-tip';
    tip.style.display = 'none';
    container.appendChild(tip);
    cy.on('mouseover', 'node', evt => showNodeTip(evt.target));
    cy.on('mouseout', 'node', scheduleHideTip);
    cy.on('mouseover', 'edge', evt => showEdgeTip(evt.target, evt.renderedPosition));
    cy.on('mouseout', 'edge', scheduleHideTip);
    cy.on('pan zoom drag', hideTip);
    tip.addEventListener('mouseenter', cancelHideTip);
    tip.addEventListener('mouseleave', hideTip);
}

function tipLine(text, cls) {
    const d = document.createElement('div');
    if (cls) d.className = cls;
    d.textContent = text;
    return d;
}

function tipLink(label, href) {
    const a = document.createElement('a');
    a.textContent = label;
    a.setAttribute('href', href);
    a.setAttribute('target', '_blank');
    a.setAttribute('rel', 'noopener noreferrer');
    a.className = 'rm-tip-link';
    return a;
}

function showNodeTip(node) {
    if (!tip) return;
    cancelHideTip();
    const d = node.data();
    tip.textContent = '';
    tip.appendChild(tipLine(d.name || d.uid, 'rm-tip-name'));
    for (const [k, v] of [['Type', d.type], ['Key', d.key], ['Domain', d.domain]]) {
        if (!v) continue;
        const row = document.createElement('div');
        row.className = 'rm-tip-meta';
        const b = document.createElement('strong'); b.textContent = k + ': ';
        row.appendChild(b); row.appendChild(document.createTextNode(v));
        tip.appendChild(row);
    }
    const actions = document.createElement('div');
    actions.className = 'rm-tip-actions';
    if (isHttpUrl(d.url)) actions.appendChild(tipLink('Open in Azure ↗', d.url));
    actions.appendChild(tipLink('Open in Resource Mapper ↗', '/resources/' + encodeURIComponent(d.uid)));
    tip.appendChild(actions);
    positionTip(node.renderedPosition());
}

function showEdgeTip(edge, pos) {
    if (!tip) return;
    cancelHideTip();
    const s = cy.getElementById(edge.data('source'));
    const t = cy.getElementById(edge.data('target'));
    tip.textContent = '';
    tip.appendChild(tipLine(!s.empty() ? (s.data('name') || edge.data('source')) : edge.data('source'), 'rm-tip-name'));
    tip.appendChild(tipLine('depends on', 'rm-tip-rel'));
    tip.appendChild(tipLine(!t.empty() ? (t.data('name') || edge.data('target')) : edge.data('target'), 'rm-tip-name'));
    positionTip(pos || edge.renderedMidpoint());
}

function positionTip(p) {
    tip.style.left = (p.x + 14) + 'px';
    tip.style.top = (p.y + 14) + 'px';
    tip.style.display = 'block';
}

function scheduleHideTip() { cancelHideTip(); tipHideTimer = setTimeout(hideTip, 240); }
function cancelHideTip() { if (tipHideTimer) { clearTimeout(tipHideTimer); tipHideTimer = null; } }
function hideTip() { cancelHideTip(); if (tip) tip.style.display = 'none'; }

// ---- right-click menu ----------------------------------------------------

function initMenu() {
    menu = cy.cxtmenu({
        selector: 'node',
        menuRadius: 90,
        commands: node => {
            const uid = node.id();
            const url = node.data('url');
            return [
                { content: node.hasClass('expanded') ? 'Collapse' : 'Expand',
                  select: () => dotNet.invokeMethodAsync('OnNodeTapped', uid) },
                { content: 'Remove',
                  select: () => dotNet.invokeMethodAsync('OnNodeRemoveRequested', uid) },
                { content: 'Open link',
                  enabled: isHttpUrl(url),
                  select: () => { if (isHttpUrl(url)) window.open(url, '_blank', 'noopener'); } },
                { content: 'Open in Mapper',
                  select: () => window.open('/resources/' + encodeURIComponent(uid), '_blank', 'noopener') }
            ];
        }
    });
}

// ---- layout: layered tidy + Re-tidy + auto-fit ---------------------------

// Layout roots for the layered tidy.
//
// An edge means "source depends on target", so a directed BFS *from the seed* only ever reaches
// what the seed depends on. Everything that depends on the seed is unreachable, and cytoscape's
// breadthfirst dumps every unvisited node into a single extra row above the tree — which is why
// re-tidy produced one long row plus a lonely seed instead of a layout.
//
// Rooting on the graph's sources (nodes nothing depends on) makes the BFS run *along* the
// dependency direction instead: dependents at the top, dependencies at the bottom, one layer per
// hop. Cyclic sub-graphs can have no source at all, so fall back to the seed, then to everything.
function tidyRoots() {
    const sources = cy.nodes().filter(n => n.indegree(false) === 0);
    if (sources.length > 0) return sources;
    if (seedId) {
        const seed = cy.getElementById(seedId);
        if (!seed.empty()) return seed;
    }
    return cy.nodes();
}

function runTidy() {
    if (!cy || cy.nodes().length === 0) return;
    cy.layout({
        name: 'breadthfirst', directed: true, roots: tidyRoots(),
        spacingFactor: 1.3, padding: 30, animate: false, avoidOverlap: true
    }).run();
    fitAll();
}

export function reTidy() { runTidy(); }

// The single framing primitive. Everything that re-frames the view goes through this so the
// fresh-load path and the collapse/expand path can't drift apart, and so a tiny graph doesn't
// zoom to the moon.
function fitAll() {
    if (!cy || cy.elements().length === 0) return;
    cy.fit(undefined, 30);
    if (cy.zoom() > FIT_MAX_ZOOM) {
        cy.zoom({ level: FIT_MAX_ZOOM, renderedPosition: { x: cy.width() / 2, y: cy.height() / 2 } });
    }
}

// ---- graph mutation ------------------------------------------------------

export function addGraph(nodes, edges, seedUid, expandFromUid) {
    const added = [];
    for (const n of nodes) {
        if (cy.getElementById(n.uid).empty()) {
            added.push(n.uid);
            cy.add({ group: 'nodes', data: {
                id: n.uid, uid: n.uid, name: n.name, key: n.key, type: n.type,
                domain: n.domain, url: n.primaryUrl,
                shortCode: n.shortCode, iconKey: n.iconKey,
                code: n.shortCode || (n.type ? n.type.substr(0,3).toUpperCase() : '?'),
                color: nodeColor(n.iconKey),
                _icon: iconUri(n.iconKey),
                bgImages: [iconUri(n.iconKey), TRANSPARENT_PX]
            }});
        }
    }
    // The server returns every edge inside the on-canvas set, not just the ones touching the node
    // being expanded, so this loop also backfills relationships between nodes that were already
    // here (both endpoints exist -> the line gets drawn). Skipping ids we already hold keeps it
    // idempotent, so re-reading a node can only ever add missing lines.
    for (const e of edges) {
        if (!e || !e.source || !e.target) continue;
        if (cy.getElementById(e.source).empty() || cy.getElementById(e.target).empty()) continue;
        const id = e.source + '__' + e.target;
        if (cy.getElementById(id).empty()) {
            cy.add({ group: 'edges', data: { id: id, source: e.source, target: e.target } });
        }
    }
    if (seedUid) { seedId = seedUid; cy.getElementById(seedUid).addClass('seed'); }

    if (!expandFromUid) {
        runTidy();
    } else if (added.length) {
        placeAround(expandFromUid, added);
        fitAll();
    }
    applyLabeled();
    applyEdgeStyles();
    renderOverlay();
}

// Fan newly-added neighbours out AWAY from the graph (outward from the seed through the parent),
// spaced so they don't stack on one line; radius grows with count.
function placeAround(parentUid, newUids) {
    const parent = cy.getElementById(parentUid);
    if (parent.empty()) return;
    const count = newUids.length;
    if (!count) return;
    const p = parent.position();

    // Outward direction: from the seed toward the parent (so new nodes push away from the graph).
    let base = -Math.PI / 2;
    const seed = seedId ? cy.getElementById(seedId) : null;
    if (seed && !seed.empty() && seed.id() !== parentUid) {
        const sp = seed.position();
        if (sp.x !== p.x || sp.y !== p.y) base = Math.atan2(p.y - sp.y, p.x - sp.x);
    }

    const radius = 150 + count * 26;                         // more neighbours -> push further out
    const span = Math.min(Math.PI * 1.5, (count - 1) * (Math.PI / 7)); // fan up to ~270°, ~26° apart
    const start = base - span / 2;
    newUids.forEach((uid, i) => {
        const node = cy.getElementById(uid);
        if (node.empty()) return;
        const angle = count === 1 ? base : start + (span * i) / (count - 1);
        node.position({ x: p.x + radius * Math.cos(angle), y: p.y + radius * Math.sin(angle) });
    });
}

export function markExpanded(uid, expanded) {
    const n = cy.getElementById(uid);
    if (n.empty()) return;
    if (expanded) n.addClass('expanded'); else n.removeClass('expanded');
    applyLabeled();
}

export function collapse(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return;
    const victims = node.neighborhood('node').filter(n =>
        n.degree(false) === 1 && !n.hasClass('seed') && !n.hasClass('expanded'));
    victims.remove();
    node.removeClass('expanded');
    applyLabeled();
    applyEdgeStyles();
    fitAll();                       // same re-framing as collapseAll / expand — one behaviour
    renderOverlay();
}

export function collapseAll() {
    if (!seedId) return [];
    cy.nodes().filter(n => n.id() !== seedId).remove();
    cy.getElementById(seedId).removeClass('expanded');
    applyLabeled();
    applyEdgeStyles();
    // Re-tidy rather than bare-fit: this leaves the canvas in exactly the state a fresh load of a
    // seed-only graph produces, so expanding from here lands in the same frame as expanding after
    // a fresh load (it used to fit() a single node, magnifying to maxZoom, and every subsequent
    // expansion was then placed inside that blown-up frame).
    runTidy();
    renderOverlay();
    return cy.nodes().map(n => n.id());
}

// The uids currently drawn. Sent back to the server on expand so it can return the edges between
// the newly-read nodes and the ones already here (see Resource_GetForExplorer @KnownResourceUids).
export function nodeIds() {
    return cy ? cy.nodes().map(n => n.id()) : [];
}

export function removeNode(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return cy.nodes().map(n => n.id());
    if (uid === seedId) { cy.elements().remove(); seedId = null; renderOverlay(); return []; }
    node.remove();
    const seed = cy.getElementById(seedId);
    if (seed.empty()) { renderOverlay(); return cy.nodes().map(n => n.id()); }
    const keep = seed.component();
    cy.nodes().not(keep).remove();
    applyLabeled();
    applyEdgeStyles();
    renderOverlay();
    return cy.nodes().map(n => n.id());
}

export function fit() { fitAll(); }

export function zoomBy(factor) {
    if (!cy) return;
    cy.zoom({ level: cy.zoom() * factor, renderedPosition: { x: cy.width() / 2, y: cy.height() / 2 } });
}

export function panByDir(dx, dy) {
    if (!cy) return;
    cy.panBy({ x: dx, y: dy });
}

// ---- persistence (serialize / load) -------------------------------------

function serialize() {
    return {
        seedUid: seedId,
        nodes: cy.nodes().map(n => {
            const p = n.position();
            const d = n.data();
            return {
                uid: n.id(), name: d.name, key: d.key, type: d.type,
                domain: d.domain, url: d.url,
                shortCode: d.shortCode, iconKey: d.iconKey,
                x: p.x, y: p.y,
                expanded: n.hasClass('expanded')
            };
        }),
        edges: cy.edges().map(e => ({ source: e.data('source'), target: e.data('target') }))
    };
}

export function serializeJson() {
    return JSON.stringify(serialize());
}

// Rebuild the canvas from a serialized diagram (positions preserved, no layout, no server calls).
// Returns the list of expanded node uids so C# can restore its expanded set.
export function loadJson(json) {
    let graph;
    try { graph = JSON.parse(json); }
    catch (e) { return null; }
    if (!graph) return null;

    cy.elements().remove();
    seedId = graph.seedUid || null;

    const els = [];
    for (const n of graph.nodes || []) {
        els.push({ group: 'nodes',
            data: {
                id: n.uid, uid: n.uid, name: n.name, key: n.key, type: n.type, domain: n.domain, url: n.url,
                shortCode: n.shortCode, iconKey: n.iconKey,
                code: n.shortCode || (n.type ? n.type.substr(0,3).toUpperCase() : '?'),
                color: nodeColor(n.iconKey),
                _icon: iconUri(n.iconKey),
                bgImages: [iconUri(n.iconKey), TRANSPARENT_PX]
            },
            position: { x: n.x, y: n.y } });
    }
    for (const e of graph.edges || []) {
        els.push({ group: 'edges', data: { id: e.source + '__' + e.target, source: e.source, target: e.target } });
    }
    cy.add(els);

    if (seedId) cy.getElementById(seedId).addClass('seed');
    for (const n of graph.nodes || []) {
        if (n.expanded) cy.getElementById(n.uid).addClass('expanded');
    }
    applyLabeled();
    applyEdgeStyles();
    fitAll();
    renderOverlay();

    return (graph.nodes || []).filter(n => n.expanded).map(n => n.uid);
}

// ---- export ---------------------------------------------------------------
// Cytoscape's cy.png()/cy.svg() capture the graph only; the exports below
// composite the title/date overlay AND the edge-style legend into a header band
// so the exported picture is self-documenting (see docs/plans/explorer/09d-title-chrome.md).
//
// Every export returns a boolean rather than swallowing its failure: a click on
// "Save PNG" that quietly does nothing is indistinguishable from a hung app, so
// the caller (ResourceExplorer.razor) snackbars anything that comes back false.

function loadImage(uri) {
    return new Promise((res, rej) => { const i = new Image(); i.onload = () => res(i); i.onerror = rej; i.src = uri; });
}

// Render the graph PNG with a title/subtitle/legend header band drawn above it. Returns a Blob.
async function pngWithHeader(scale) {
    const uri = cy.png({ output: 'base64uri', full: true, bg: '#ffffff', scale: scale });
    const title = overlayTitle, sub = subWithCount();
    const img = await loadImage(uri);
    const padX = 16 * scale, padTop = 14 * scale, gap = 6 * scale, padBottom = 12 * scale;
    const titleF = 20 * scale, subF = 12 * scale;
    const textH = (title ? titleF : 0) + (title && sub ? gap : 0) + (sub ? subF : 0);
    // A legend that won't rasterise must not take the whole export down with it.
    let legend = null;
    if (hasEdges()) {
        try { legend = await loadImage(legendUri()); }
        catch (e) { console.warn('Explorer export: legend could not be rendered, continuing without it.', e); }
    }
    const legendH = legend ? LEGEND_H * scale : 0;
    const headerH = (title || sub || legend)
        ? padTop + Math.max(textH, legendH) + padBottom
        : 0;
    const canvas = document.createElement('canvas');
    canvas.width = img.width;
    canvas.height = img.height + headerH;
    const ctx = canvas.getContext('2d');
    if (!ctx) throw new Error('2d canvas context unavailable');
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(img, 0, headerH);
    ctx.textBaseline = 'top';
    ctx.textAlign = 'left';
    let y = padTop;
    if (title) { ctx.fillStyle = '#0f172a'; ctx.font = '700 ' + titleF + 'px system-ui, sans-serif'; ctx.fillText(title, padX, y); y += titleF + gap; }
    if (sub)   { ctx.fillStyle = '#64748b'; ctx.font = '400 ' + subF + 'px system-ui, sans-serif'; ctx.fillText(sub, padX, y); }
    // Right-aligned beside the title, but only when the picture is wide enough to hold both.
    if (legend) {
        const lw = LEGEND_W * scale;
        const lx = canvas.width - padX - lw;
        if (lx >= 260 * scale) ctx.drawImage(legend, lx, padTop, lw, legendH);
    }
    const blob = await new Promise(res => canvas.toBlob(res, 'image/png'));
    if (!blob) throw new Error('canvas.toBlob produced no image');
    return blob;
}

// Prepend a title/subtitle/legend header into an SVG string (bumps height/viewBox, shifts content down).
function svgWithHeader(svg) {
    const title = overlayTitle, sub = subWithCount();
    const legend = hasEdges();
    if (!title && !sub && !legend) return svg;
    const headerH = 52;
    const openMatch = svg.match(/^([\s\S]*?<svg[^>]*>)/);
    if (!openMatch) return svg;
    let open = openMatch[1];
    const inner = svg.slice(open.length, svg.lastIndexOf('</svg>'));
    let width = 0;
    const wM = open.match(/width="([\d.]+)"/);
    if (wM) width = parseFloat(wM[1]);
    const hM = open.match(/height="([\d.]+)"/);
    if (hM) open = open.replace(/height="[\d.]+"/, 'height="' + (parseFloat(hM[1]) + headerH) + '"');
    open = open.replace(/viewBox="([-\d.\s]+)"/, (m, vb) => {
        const p = vb.trim().split(/\s+/).map(Number);
        if (p.length === 4) { p[3] = p[3] + headerH; if (!width) width = p[2]; }
        return 'viewBox="' + p.join(' ') + '"';
    });
    // Only right-align the legend if the picture is actually wide enough to hold it beside the
    // title; on a narrow export drop it rather than stack it on top of the text.
    const legendX = width >= LEGEND_W + 260 ? width - 16 - LEGEND_W : -1;
    const header =
        '<rect x="0" y="0" width="100%" height="' + headerH + '" fill="#ffffff"/>' +
        '<text x="16" y="26" font-family="system-ui,sans-serif" font-size="20" font-weight="700" fill="#0f172a">' + escapeHtml(title || '') + '</text>' +
        '<text x="16" y="44" font-family="system-ui,sans-serif" font-size="12" fill="#64748b">' + escapeHtml(sub || '') + '</text>' +
        (legend && legendX >= 0 ? legendSvgInner(legendX, 7) : '');
    return open + header + '<g transform="translate(0,' + headerH + ')">' + inner + '</g></svg>';
}

export async function copyPng() {
    try {
        const blob = await pngWithHeader(2);
        await navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
        return true;
    } catch (e) { return exportFailed('copy image', e); }
}

export async function savePng(filename) {
    try {
        const blob = await pngWithHeader(2);
        const url = URL.createObjectURL(blob);
        downloadUri(url, (filename || 'diagram') + '.png');
        setTimeout(() => URL.revokeObjectURL(url), 5000);
        return true;
    } catch (e) { return exportFailed('save PNG', e); }
}

export function saveSvg(filename) {
    try {
        const svg = svgWithHeader(cy.svg({ full: true, bg: '#ffffff' }));   // cytoscape-svg extension
        const url = URL.createObjectURL(new Blob([svg], { type: 'image/svg+xml;charset=utf-8' }));
        downloadUri(url, (filename || 'diagram') + '.svg');
        setTimeout(() => URL.revokeObjectURL(url), 5000);
        return true;
    } catch (e) { return exportFailed('save SVG', e); }
}

export function printDiagram() {
    let w = null;
    try {
        const svg = svgWithHeader(cy.svg({ full: true, bg: '#ffffff' }));
        w = window.open('', '_blank');
        if (!w) return exportFailed('print', new Error('the browser blocked the print window'));
        // overlayTitle is the user-supplied diagram name and diagrams are shareable, so an
        // unescaped name would run script in the RECIPIENT's browser when they print. Escape it.
        w.document.write('<!doctype html><meta charset="utf-8"><title>' +
            escapeHtml(overlayTitle || 'Diagram') + '</title>' + svg);
        w.document.close();
        w.focus();
        w.print();
        return true;
    } catch (e) {
        if (w) { try { w.close(); } catch (e2) { /* already gone */ } }
        return exportFailed('print', e);
    }
}

function exportFailed(what, err) {
    console.error('Explorer export failed (' + what + '):', err);
    return false;
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

function isHttpUrl(u) {
    if (!u) return false;
    try {
        const p = new URL(u);           // absolute only
        return p.protocol === 'http:' || p.protocol === 'https:';
    } catch (e) { return false; }
}

export function dispose() {
    if (menu) { try { menu.destroy(); } catch (e) { /* extension teardown */ } menu = null; }
    if (tip && tip.parentNode) tip.parentNode.removeChild(tip);
    tip = null;
    if (overlayEl && overlayEl.parentNode) overlayEl.parentNode.removeChild(overlayEl);
    overlayEl = null;
    if (cy) { cy.destroy(); cy = null; }
    dotNet = null;
    seedId = null;
}
