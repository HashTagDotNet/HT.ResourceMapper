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
    const esc = s => String(s || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
    return svgDataUri(
        '<svg xmlns="http://www.w3.org/2000/svg" width="' + w + '" height="' + h + '">' +
        '<text x="' + (w/2) + '" y="15" text-anchor="middle" font-family="system-ui,sans-serif" font-size="14" font-weight="700" fill="#0f172a">' + esc(name) + '</text>' +
        '<text x="' + (w/2) + '" y="30" text-anchor="middle" font-family="system-ui,sans-serif" font-size="12" fill="#64748b">' + esc(type) + '</text>' +
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
                'width': 46, 'height': 46,
                'border-width': 2, 'border-color': '#1e293b',
                'label': 'data(code)',
                'color': '#fff',
                'font-size': '11px',
                'font-weight': 700,
                'text-valign': 'center',
                'text-halign': 'center',
                // two background images: [type icon (always), name/type caption (labeled only)].
                // Position/size tuned in-browser (see docs/plans/explorer/09c-node-layout.md execution
                // notes): icon centered-upper inside the circle; caption anchored by a fixed px offset
                // (not percent — see captionUri's comment) so it sits just below the circle with a
                // small gap. bounds-expansion is asymmetric [top,right,bottom,left] to fit the wide
                // caption's left/right overhang and its below-node extent without clipping renders/exports.
                'background-image': 'data(bgImages)',
                'background-image-crossorigin': 'anonymous',
                'background-width':  ['20px', '180px'],
                'background-height': ['20px', '36px'],
                'background-position-x': ['50%', '50%'],
                'background-position-y': ['28%', '50px'],
                'background-clip': ['none', 'none'],
                'background-image-containment': ['inside', 'over'],
                'bounds-expansion': [10, 75, 50, 75]
            }},
            { selector: 'node.seed',     style: { 'border-color': '#f59e0b', 'border-width': 4 }},
            { selector: 'node.expanded', style: { 'border-style': 'double' }},
            { selector: 'edge', style: {
                'width': 1,
                'line-color': '#94a3b8',
                'target-arrow-color': '#94a3b8',
                'target-arrow-shape': 'triangle',
                'arrow-scale': 0.5,
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

    cy.on('mouseover', 'node', e => { cy.scratch('_hover', e.target.id()); applyLabeled(); });
    cy.on('mouseout',  'node', () => { cy.scratch('_hover', null); applyLabeled(); });
    cy.on('select unselect', 'node', () => applyLabeled());

    initTooltip(hostEl);
    initMenu();
}

// ---- labeled state (caption visibility) ---------------------------------

// Seed + hovered + selected nodes get the caption; everyone else just the code+icon.
function applyLabeled() {
    const hoverId = cy.scratch('_hover');
    cy.nodes().forEach(n => {
        const on = n.hasClass('seed') || n.selected() || n.id() === hoverId;
        if (on) n.addClass('labeled'); else n.removeClass('labeled');
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

// ---- tooltip (plain positioned div, XSS-safe via textContent) ------------

function initTooltip(container) {
    tip = document.createElement('div');
    tip.className = 'rm-explorer-tip';
    tip.style.display = 'none';
    container.appendChild(tip);
    cy.on('mouseover', 'node', evt => showTip(evt.target));
    cy.on('mouseout', 'node', hideTip);
    cy.on('pan zoom drag', hideTip);
}

function showTip(node) {
    if (!tip) return;
    const d = node.data();
    tip.textContent = '';
    const rows = [['Name', d.name], ['Key', d.key], ['Type', d.type], ['Domain', d.domain], ['Link', d.url]];
    for (const [k, v] of rows) {
        if (!v) continue;
        const row = document.createElement('div');
        const b = document.createElement('strong');
        b.textContent = k + ': ';
        row.appendChild(b);
        row.appendChild(document.createTextNode(v));
        tip.appendChild(row);
    }
    const p = node.renderedPosition();
    tip.style.left = (p.x + 16) + 'px';
    tip.style.top = (p.y + 16) + 'px';
    tip.style.display = 'block';
}

function hideTip() { if (tip) tip.style.display = 'none'; }

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

function cssId(id) { return id.replace(/[^a-zA-Z0-9_-]/g, m => '\\' + m); }

function runTidy() {
    cy.layout({
        name: 'breadthfirst', directed: true, roots: seedId ? '#' + cssId(seedId) : undefined,
        spacingFactor: 1.3, padding: 30, animate: false
    }).run();
    fit();
}

export function reTidy() { runTidy(); }

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
    for (const e of edges) {
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
        fit();
    }
    applyLabeled();
    applyEdgeStyles();
}

function placeAround(parentUid, newUids) {
    const parent = cy.getElementById(parentUid);
    if (parent.empty()) return;
    const p = parent.position();
    const radius = 150;
    const count = Math.max(newUids.length, 1);
    newUids.forEach((uid, i) => {
        const node = cy.getElementById(uid);
        if (node.empty()) return;
        const angle = (2 * Math.PI * i) / count - Math.PI / 2;
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
}

export function collapseAll() {
    if (!seedId) return [];
    cy.nodes().filter(n => n.id() !== seedId).remove();
    cy.getElementById(seedId).removeClass('expanded');
    applyLabeled();
    applyEdgeStyles();
    fit();
    return cy.nodes().map(n => n.id());
}

export function removeNode(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return cy.nodes().map(n => n.id());
    if (uid === seedId) { cy.elements().remove(); seedId = null; return []; }
    node.remove();
    const seed = cy.getElementById(seedId);
    if (seed.empty()) return cy.nodes().map(n => n.id());
    const keep = seed.component();
    cy.nodes().not(keep).remove();
    applyLabeled();
    applyEdgeStyles();
    return cy.nodes().map(n => n.id());
}

export function fit() { if (cy) cy.fit(undefined, 30); }

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
    fit();

    return (graph.nodes || []).filter(n => n.expanded).map(n => n.uid);
}

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
    if (cy) { cy.destroy(); cy = null; }
    dotNet = null;
    seedId = null;
}
