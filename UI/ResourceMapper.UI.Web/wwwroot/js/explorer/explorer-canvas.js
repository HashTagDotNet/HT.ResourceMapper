// Explorer canvas — thin wrapper over Cytoscape (global `cytoscape`) + the cxtmenu extension.
// One instance per page. Graph state lives in the browser; C# pushes graph batches (addGraph) and
// receives node events (tap / menu) via the DotNetObjectReference.

let cy = null;
let dotNet = null;
let seedId = null;
let currentPreset = 'nameType';   // 'name' | 'nameType' | 'detailed'
let tip = null;
let menu = null;

export function init(hostEl, dotNetRef) {
    dotNet = dotNetRef;
    cy = cytoscape({
        container: hostEl,
        elements: [],
        style: [
            { selector: 'node', style: {
                'background-color': '#2563EB',
                'label': 'data(label)',
                'color': '#0f172a',
                'font-size': '11px',
                'text-valign': 'bottom',
                'text-halign': 'center',
                'text-margin-y': 4,
                'text-wrap': 'wrap',
                'text-max-width': '140px',
                'width': 30, 'height': 30,
                'border-width': 2, 'border-color': '#1e3a8a'
            }},
            { selector: 'node.seed',     style: { 'background-color': '#f59e0b', 'border-color': '#b45309' }},
            { selector: 'node.expanded', style: { 'border-color': '#16a34a', 'border-width': 3 }},
            { selector: 'edge', style: {
                'width': 2,
                'line-color': '#94a3b8',
                'target-arrow-color': '#94a3b8',
                'target-arrow-shape': 'triangle',
                'curve-style': 'bezier'
            }}
        ],
        layout: { name: 'grid' },
        minZoom: 0.2, maxZoom: 3, wheelSensitivity: 0.2
    });

    // Tap toggles expand/collapse (remove/open actions live in the right-click menu).
    cy.on('tap', 'node', evt => {
        dotNet.invokeMethodAsync('OnNodeTapped', evt.target.id());
    });

    initTooltip(hostEl);
    initMenu();
}

// ---- labels / presets ---------------------------------------------------

function labelFor(ele) {
    const d = ele.data();
    const glyph = ele.hasClass('expanded') ? '▾ ' : '▸ ';
    let body;
    switch (currentPreset) {
        case 'name':
            body = d.name || d.uid; break;
        case 'detailed':
            body = [d.key, d.name, d.type, d.domain].filter(Boolean).join('\n'); break;
        case 'nameType':
        default:
            body = (d.name || d.uid) + (d.type ? '\n' + d.type : ''); break;
    }
    return glyph + body;
}

function applyLabels() {
    cy.nodes().forEach(n => n.data('label', labelFor(n)));
}

export function setPreset(preset) {
    currentPreset = preset;
    applyLabels();
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
                  enabled: !!url,
                  select: () => { if (url) window.open(url, '_blank', 'noopener'); } },
                { content: 'Open in Mapper',
                  select: () => window.open('/resources/' + encodeURIComponent(uid), '_blank', 'noopener') }
            ];
        }
    });
}

// ---- graph mutation ------------------------------------------------------

export function addGraph(nodes, edges, seedUid, expandFromUid) {
    const added = [];
    for (const n of nodes) {
        if (cy.getElementById(n.uid).empty()) {
            added.push(n.uid);
            cy.add({ group: 'nodes', data: {
                id: n.uid, uid: n.uid, name: n.name, key: n.key,
                type: n.type, domain: n.domain, url: n.primaryUrl
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
        cy.layout({ name: 'cose', animate: false, padding: 30 }).run();
    } else if (added.length) {
        placeAround(expandFromUid, added);
    }
    applyLabels();
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
    applyLabels();
}

export function collapse(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return;
    const victims = node.neighborhood('node').filter(n =>
        n.degree(false) === 1 && !n.hasClass('seed') && !n.hasClass('expanded'));
    victims.remove();
    node.removeClass('expanded');
    applyLabels();
}

export function collapseAll() {
    if (!seedId) return [];
    cy.nodes().filter(n => n.id() !== seedId).remove();
    cy.getElementById(seedId).removeClass('expanded');
    applyLabels();
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
    applyLabels();
    return cy.nodes().map(n => n.id());
}

export function fit() { if (cy) cy.fit(undefined, 30); }

export function dispose() {
    if (menu) { try { menu.destroy(); } catch (e) { /* extension teardown */ } menu = null; }
    if (tip && tip.parentNode) tip.parentNode.removeChild(tip);
    tip = null;
    if (cy) { cy.destroy(); cy = null; }
    dotNet = null;
    seedId = null;
}
