// Explorer canvas — thin wrapper over Cytoscape (global `cytoscape`, from vendor/cytoscape.min.js).
// One instance per page. Graph state (which nodes/edges exist, which are expanded) lives in the
// browser; C# pushes graph batches down (addGraph) and receives tap events (via dotNet ref).

let cy = null;
let dotNet = null;
let seedId = null;

export function init(hostEl, dotNetRef) {
    dotNet = dotNetRef;
    cy = cytoscape({
        container: hostEl,
        elements: [],
        style: [
            { selector: 'node', style: {
                'background-color': '#2563EB',
                'label': 'data(name)',
                'color': '#0f172a',
                'font-size': '11px',
                'text-valign': 'bottom',
                'text-halign': 'center',
                'text-margin-y': 4,
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

    // Tap a node -> expand/collapse; Shift+tap -> remove (interim trigger; slice 4 adds a menu).
    cy.on('tap', 'node', evt => {
        const uid = evt.target.id();
        const remove = !!(evt.originalEvent && evt.originalEvent.shiftKey);
        dotNet.invokeMethodAsync(remove ? 'OnNodeRemoveRequested' : 'OnNodeTapped', uid);
    });
}

// Idempotently add nodes/edges. nodes: [{uid,key,name,type,domain,primaryUrl}].
// edges: [{source,target}] (uids, dependent -> dependency). seedUid: mark as seed or null.
// expandFromUid: when set (an expansion), place new nodes around that parent WITHOUT relayout,
// preserving existing (possibly dragged) positions. When null (seed load), run the initial layout.
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
        // Seed load — lay the whole thing out once.
        cy.layout({ name: 'cose', animate: false, padding: 30 }).run();
    } else if (added.length) {
        placeAround(expandFromUid, added);
    }
}

// Place newly-added nodes in a ring around their parent, leaving existing nodes where they are.
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
}

// Collapse: remove leaf neighbours that exist only because of `uid`
// (degree 1, not the seed, not themselves expanded). Positions of survivors are preserved.
export function collapse(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return;
    const victims = node.neighborhood('node').filter(n =>
        n.degree(false) === 1 && !n.hasClass('seed') && !n.hasClass('expanded'));
    victims.remove();
    node.removeClass('expanded');
}

export function fit() { if (cy) cy.fit(undefined, 30); }

// Collapse everything back to just the seed.
export function collapseAll() {
    if (!seedId) return [];
    cy.nodes().filter(n => n.id() !== seedId).remove(); // edges are removed with their nodes
    cy.getElementById(seedId).removeClass('expanded');
    fit();
    return cy.nodes().map(n => n.id());
}

// Remove a node, then drop any node no longer reachable (undirected) from the seed.
// Removing the seed clears the canvas. Positions of survivors are preserved (no relayout).
export function removeNode(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return cy.nodes().map(n => n.id());
    if (uid === seedId) { cy.elements().remove(); seedId = null; return []; }
    node.remove();
    const seed = cy.getElementById(seedId);
    if (seed.empty()) return cy.nodes().map(n => n.id());
    const keep = seed.component();          // undirected connected component containing the seed
    cy.nodes().not(keep).remove();
    return cy.nodes().map(n => n.id());
}

export function dispose() {
    if (cy) { cy.destroy(); cy = null; }
    dotNet = null;
}
