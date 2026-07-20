// Explorer canvas — thin wrapper over Cytoscape (global `cytoscape`, from vendor/cytoscape.min.js).
// One instance per page. Graph state (which nodes/edges exist, which are expanded) lives in the
// browser; C# pushes graph batches down (addGraph) and receives tap events (via dotNet ref).

let cy = null;
let dotNet = null;

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

    // Tap a node -> ask C# to expand or collapse it.
    cy.on('tap', 'node', evt => {
        const uid = evt.target.id();
        dotNet.invokeMethodAsync('OnNodeTapped', uid);
    });
}

// Idempotently add nodes/edges. `nodes`: [{uid,key,name,type,domain,primaryUrl}].
// `edges`: [{source,target}] (uids, dependent -> dependency). `seedUid`: mark as the seed, or null.
export function addGraph(nodes, edges, seedUid) {
    const toAdd = [];
    for (const n of nodes) {
        if (cy.getElementById(n.uid).empty()) {
            toAdd.push({ group: 'nodes', data: {
                id: n.uid, uid: n.uid, name: n.name, key: n.key,
                type: n.type, domain: n.domain, url: n.primaryUrl
            }});
        }
    }
    for (const e of edges) {
        const id = e.source + '__' + e.target;
        if (cy.getElementById(id).empty()) {
            toAdd.push({ group: 'edges', data: { id: id, source: e.source, target: e.target } });
        }
    }
    if (toAdd.length) cy.add(toAdd);
    if (seedUid) cy.getElementById(seedUid).addClass('seed');
    relayout();
}

export function markExpanded(uid, expanded) {
    const n = cy.getElementById(uid);
    if (n.empty()) return;
    if (expanded) n.addClass('expanded'); else n.removeClass('expanded');
}

// Collapse: remove leaf neighbours that exist only because of `uid`
// (degree 1, not the seed, not themselves expanded).
export function collapse(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return;
    const victims = node.neighborhood('node').filter(n =>
        n.degree(false) === 1 && !n.hasClass('seed') && !n.hasClass('expanded'));
    victims.remove();
    node.removeClass('expanded');
    relayout();
}

export function fit() { if (cy) cy.fit(undefined, 30); }

function relayout() {
    // Slice 3 replaces this with position-preserving layout.
    cy.layout({ name: 'cose', animate: false, padding: 30 }).run();
}

export function dispose() {
    if (cy) { cy.destroy(); cy = null; }
    dotNet = null;
}
