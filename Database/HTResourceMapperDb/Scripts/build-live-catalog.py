"""Builds LiveCatalog.json from the ADO wiki page 'vNext - Azure Resource Links' (page 11783),
plus a register of everything the import format cannot carry (which the SQL script must add)."""
import io, os, re, json, html

# Everything lives beside this script: the wiki snapshot in, the catalog out.
B = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.join(B, 'wiki-vnext-azure-resource-links.md')
lines = io.open(SOURCE, encoding='utf-8').read().split('\n')

# ---- agreed decisions -----------------------------------------------------------
SUBS = {'1d6dcbc4-27e3-4f74-a34d-89a09505ccd3': ('DSG-DevTestMSDN', 'non-prod'),
        '6904df25-40a0-4531-9fb0-f6fde48f6785': ('DSG-ProdPreprod', 'prod')}
PROVIDER_TYPE = {
    'microsoft.appconfiguration/configurationstores': 'App Configuration',
    'microsoft.insights/components':                  'App Insights',
    'microsoft.web/sites':                            'App Service',
    'microsoft.cache/redis':                          'Redis',
    'microsoft.cache/redisenterprise':                'Managed Redis',
    'microsoft.servicebus/namespaces':                'Service Bus',
    'microsoft.servicefabric/managedclusters':        'Service Fabric',
    'microsoft.storage/storageaccounts':              'Storage Account',
}
# Operational shortcut links that sit alongside a site's portal link. 'Restart App Services'
# is an Azure DevOps build pipeline, not an Azure link.
EXTRA_LINK_TAG = {'kudu console': 'KuduUrl',
                  'restart app services': 'RestartPipelineUrl',
                  'service console error logs': 'ServiceConsoleUrl'}
# Wiki column heading -> environment value. 'pentest' is new and has no resources in the wiki
# yet, so it appears in the vocabulary (the SQL script) but on no resource here.
ENVIRONMENTS = ['localhost', 'development', 'pentest', 'preprod', 'preview', 'production']
ENV_MAP = {'localhost': 'localhost', 'develop': 'development', 'integration test': 'integration test',
           'int.test': 'integration test', 'pre prod': 'preprod', 'preview': 'preview',
           'prod': 'production', 'production': 'production', 'development': 'development'}
RETIRED_ENVIRONMENTS = {'integration test'}          # no longer used
SYSTEMS = {'Visibility': 'The vNext visibility platform - web app, API and proxy.',
           'Alerting':   'The MacroPoint alerting subsystem - ingestion, rules and triggers.',
           'LITE':       'MacroPoint LITE.'}
APPS = {
    'Alert Aggregator':   ('Alerting',   ['MacroPoint:Alerting:AlertAggregator']),
    'Compliance Manager': ('Alerting',   ['MacroPoint:Alerting:ComplianceManager']),
    # Renamed from 'Db Ingestor'; the wiki still says Db Ingestor (and 'Db Aggregator' in the
    # Service Bus table). The rename is mid-flight: both prefixes are live in the App Config
    # exports of usspi-apc-dt-mplt-07 and usspi-apc-pd-mp-01, so it carries both.
    'Heavy Ingestor':     ('Alerting',   ['MacroPoint:Alerting:HeavyIngestor']),
    'Rule Manager':       ('Alerting',   ['MacroPoint:Alerting:RuleManager']),
    'Rule Processor':     ('Alerting',   ['MacroPoint:Alerting:RuleProcessor']),
    'Signal Processor':   ('Alerting',   ['MacroPoint:Alerting:SignalProcessor']),
    'Trigger Aggregator': ('Alerting',   ['MacroPoint:Alerting:TriggerAggregator']),
    'Trigger Ingestor':   ('Alerting',   ['MacroPoint:Alerting:TriggerIngestor']),
    'vNext':              ('Visibility', ['MacroPoint:Api', 'MacroPoint:Client']),
    'vNext Proxy':        ('Visibility', []),
    'Notification (FTP)': ('Visibility', []),
    'LITE Ftp Processor': ('LITE',       []),
}

# Wiki row labels that become resource descriptions, corrected. 'Storage Account (Blob))' has a
# stray bracket in the wiki; 'App Service - Proxy' is the index table's name for the thing the
# App Services table (and the application itself) calls vNext Proxy - left alone, the same site
# family ends up with two different descriptions depending on which table introduced it.
WIKI_LABEL_FIXUPS = {
    'Storage Account (Blob))': 'Storage Account (Blob)',
    'App Service - Proxy':     'App Service - vNext Proxy',
}
# A row whose label names one thing but whose cell holds another. The Redis row is labelled
# 'Azure Cache for Redis' yet carries a second link to the Enterprise SKU (Microsoft.Cache/
# redisEnterprise), which is a different resource type and needs its own description.
RESOURCE_DESCRIPTIONS = {
    'usspi-red-dt-mplt-04': 'Azure Managed Redis',
}

# Wiki row labels that name an application we already have under a different name. Applied when
# the matrix tables attach their per-environment values, so one application collects them all.
ALIASES = {'Db Ingestor': 'Heavy Ingestor', 'Db Aggregator': 'Heavy Ingestor',
           'Proxy': 'vNext Proxy', 'Api': 'vNext', 'Client': 'vNext', 'Web UI': 'vNext'}
# Wiki row labels that are not applications: infrastructure rows already catalogued from the
# index table, aggregate telemetry filters, and config-only scopes.
NOT_APPLICATIONS = {'Service Bus', 'Service Fabric', 'All Alerts',
                    'Alerting (shared)', 'MacroPoint Shared', 'System Shared'}

def slug(s):  return re.sub(r'[^a-z0-9]+', '-', s.lower()).strip('-')
def clean(c): return html.unescape(re.sub(r'<[^>]+>', ' ', re.sub(r'\[([^\]]*)\]\([^)]*\)', r'\1', c))).strip()
def norm_env(h):
    raw = re.sub(r'\s*\(restricted\)\s*', '', h).strip().lower()
    return ENV_MAP.get(raw, raw)

resources, xdomain_edges, unrepresentable = {}, [], []

def add(name, rtype, url, desc, env=None, link_tag='PortalUrl', fallback_domain=None):
    """Upsert one Azure resource, merging Environment values when a site serves several.

    Tier and subscription come from the portal URL. A Service Fabric Explorer URL carries
    neither, so the column the link sat in (non-prod / prod) is used instead."""
    sub = re.search(r'/subscriptions/([0-9a-f-]{36})', url, re.I)
    rg  = re.search(r'/resource[Gg]roups/([^/]+)', url)
    sub_name, domain = SUBS.get(sub.group(1).lower(), (None, None)) if sub else (None, None)
    domain = domain or fallback_domain or '?'
    if not sub_name:        # no subscription in the URL: the tier implies which one it is
        sub_name = next((n for n, d in SUBS.values() if d == domain), None)
    tags = {'Domain': domain, link_tag: url}
    if rg:       tags['ResourceGroup'] = rg.group(1).lower()
    if sub_name: tags['Subscription'] = sub_name
    r = resources.setdefault(slug(name), {'key': slug(name), 'name': name, 'type': rtype,
                                          'description': desc, 'tags': tags})
    if env:                                            # accumulate environments per site
        cur = r['tags'].setdefault('Environment', [])
        if env not in cur: cur.append(env)
    return r

# ---- 1. index table: one resource per link ---------------------------------------
for n in range(4, 16):
    cells = lines[n].strip('|').split('|')
    desc  = clean(cells[0])
    desc  = WIKI_LABEL_FIXUPS.get(desc, desc)
    for col_domain, cell in zip(('non-prod', 'prod'), cells[1:3]):
        in_cell, search_note = [], None
        for part in re.split(r'<br\s*/?>', cell):
            links = re.findall(r'\[([^\]]*)\]\(([^)]*)\)', part)
            if not links and clean(part).startswith('Search:'):
                search_note = clean(part).split(':', 1)[1].strip()
            for text, url in links:
                m  = re.search(r'/providers/([^/]+)/([^/]+)/([^/?#]+)', url)
                sf = re.match(r'https://([a-z0-9-]+)\.[a-z0-9.-]+:19080/', url, re.I)
                if m:
                    rtype = PROVIDER_TYPE[f'{m.group(1)}/{m.group(2)}'.lower()]
                    # The index table's non-prod column holds the development deployments; this is
                    # the only place the usaus- (second region) sites appear.
                    in_cell.append(add(m.group(3), rtype, url, desc,
                                       env='development' if rtype == 'App Service' else None,
                                       fallback_domain=col_domain))
                elif sf:
                    # The link text names a Service Fabric APPLICATION TYPE hosted on the cluster,
                    # not the cluster itself, so it belongs in a tag. Keeping it out of the
                    # description lets both clusters share one description and pair up as a single
                    # row in any view grouped by description (as the wiki's own table does).
                    cluster = add(sf.group(1), 'Service Fabric', url, desc,
                                  link_tag='ExplorerUrl', fallback_domain=col_domain)
                    app_type = text.strip()
                    if app_type:
                        hosted = cluster['tags'].setdefault('ServiceFabricApp', [])
                        if app_type not in hosted:
                            hosted.append(app_type)
                    in_cell.append(cluster)
                else:
                    unrepresentable.append(('skipped-link', desc, text.strip() + ' -> ' + url[:55]))
        # A bare 'Search: ...' note is a portal search hint for the resource in the same cell.
        if search_note and in_cell:
            in_cell[0]['tags']['Search'] = search_note

# ---- 2. App Services matrix: environments + operational links --------------------
hdr = [clean(x) for x in lines[209].strip('|').split('|')]
for i in (211, 212):
    cells = lines[i].strip('|').split('|')
    app   = clean(cells[0])
    for env_hdr, cell in zip(hdr[1:], cells[1:]):
        env, site = norm_env(env_hdr), None
        if env in RETIRED_ENVIRONMENTS:      # environment no longer used; its sites are not catalogued
            continue
        for text, url in re.findall(r'\[([^\]]*)\]\(([^)]*)\)', cell):
            m = re.search(r'/providers/microsoft\.web/sites/([^/?#]+)', url, re.I)
            if m:
                site = add(m.group(1), 'App Service', url, 'App Service - ' + app, env=env)
            elif site and text.strip().lower() in EXTRA_LINK_TAG:
                site['tags'][EXTRA_LINK_TAG[text.strip().lower()]] = url
        if site:
            edge = (slug(app), site['key'])
            if edge not in xdomain_edges:
                xdomain_edges.append(edge)                 # Application -> App Service

# ---- 3. virtual layer: Systems and Applications ---------------------------------
for name, desc in SYSTEMS.items():
    resources[slug(name)] = {'key': slug(name), 'name': name, 'type': 'System',
                             'description': desc, 'tags': {'Domain': 'shared'}}
for name, (system, prefixes) in APPS.items():
    tags = {'Domain': 'shared', 'System': system}
    if prefixes: tags['ConfigPrefix'] = prefixes if len(prefixes) > 1 else prefixes[0]
    resources[slug(name)] = {'key': slug(name), 'name': name, 'type': 'Application',
                             'description': system + ' application', 'tags': tags,
                             'dependencies': [slug(system)]}          # same-domain: importable

# ---- 4. matrix tables: per-environment values as multi-valued tags ---------------
# Each value already names its own environment (alert-aa(dev), macropoint-alerting-prod-...,
# MacropointAlerting.SF-dev/...), so no per-environment tag keys are needed.
# The Service Bus table (line 241) is deliberately NOT read here. Its queue names are stale --
# Azure has no 'db-aggregator' queues -- and the queues are catalogued as resources in section 6
# instead, where each one carries an edge back to the application it belongs to.
MATRIX = [(52, 'AppInsightsFilter'), (222, 'DbAppName'), (258, 'ServiceFabricApp')]
EMPTY = {'', '--', 'n/a', '(restricted)', '-'}
# Wiki errors corrected here and logged; push the fixes back to the wiki separately.
CORRECTIONS = {
    'macropoint-alerting-prod-trigger-aggreagor': 'macropoint-alerting-dev-trigger-aggregator',
    'macropoint-alerting-dev-trigger-aggregator@production': 'macropoint-alerting-prod-trigger-aggregator',
    'macropont-alerting-prod-rule-manager': 'macropoint-alerting-prod-rule-manager',
}
corrections_made = []

def rows_of(hdr_idx):
    i = hdr_idx + 2
    while i < len(lines) and lines[i].startswith('|'):
        yield [clean(x).replace('`', '').strip() for x in lines[i].strip('|').split('|')]
        i += 1

def matrix_tag(app_label, tag_key, env, value):
    """Attach one per-environment value to the application it belongs to."""
    name = ALIASES.get(app_label, app_label)
    r = resources.get(slug(name))
    if r is None or r['type'] != 'Application':
        return
    fixed = CORRECTIONS.get(value + '@' + env) or CORRECTIONS.get(value)
    if fixed:
        corrections_made.append((app_label, env, value, fixed))
        value = fixed
    cur = r['tags'].setdefault(tag_key, [])
    if value not in cur:
        cur.append(value)

for hdr_idx, tag_key in MATRIX:
    hdr = [clean(x).replace('`', '').strip() for x in lines[hdr_idx].strip('|').split('|')]
    for row in rows_of(hdr_idx):
        label = row[0]
        if not label or label in NOT_APPLICATIONS:
            continue
        for env_hdr, value in zip(hdr[1:], row[1:]):
            env = norm_env(env_hdr)
            if env in RETIRED_ENVIRONMENTS or value.strip().lower() in EMPTY:
                continue
            if len(re.sub(r'[^A-Za-z0-9]', '', value)) < 2:
                continue
            # Markdown link broken across a cell boundary in the wiki source - unusable.
            if '[' in value or ']' in value or value.rstrip().endswith('\\'):
                unrepresentable.append(('malformed-cell', label + ' / ' + tag_key, value[:60]))
                continue
            matrix_tag(label, tag_key, env, value.strip())

# Storage Account blob containers -> multi-valued Container tag on the account.
for row in rows_of(279):
    acct = resources.get(slug(row[0]))
    if acct is None:
        continue
    for value in row[1:]:
        v = value.strip()
        if v.lower() in EMPTY or len(re.sub(r'[^A-Za-z0-9]', '', v)) < 2:
            continue
        cur = acct['tags'].setdefault('Container', [])
        if v not in cur:
            cur.append(v)

# ---- 5. queues and their producers/consumers ------------------------------------
# Source: wiki page 13215, '/Visibility/Queues <--> Services'. A curated 28 out of the ~700
# queues each namespace actually holds, so the list is a deliberate selection, not an inventory.
QUEUE_SOURCE = os.path.join(B, 'wiki-queues-services.md')
NAMESPACES = {   # tier -> (namespace, resource group, subscription id)
    'prod':     ('USSPI-SBN-PD-MPLT-02', 'USSPI-RSG-PD-MPLT-11', '6904df25-40a0-4531-9fb0-f6fde48f6785'),
    'non-prod': ('USSPI-SBN-DT-MPLT-02', 'USSPI-RSG-DT-MPLT-27', '1d6dcbc4-27e3-4f74-a34d-89a09505ccd3'),
}
PORTAL = ('https://portal.azure.com/#@descartessystems.onmicrosoft.com/resource'
          '/subscriptions/%s/resourceGroups/%s/providers/Microsoft.ServiceBus'
          '/namespaces/%s/queues/%s')
# Verified against Azure: every wiki queue exists in prod; these four do NOT exist in the
# non-prod namespace (three are named for the prod environment, so their non-prod twins carry
# different names; geocodelonglockqueue is prod-only).
PROD_ONLY_QUEUES = {'geocodelonglockqueue',
                    'macropoint-alerting-prod-compliance-manager',
                    'macropoint-alerting-prod-trigger-aggregator',
                    'macropoint-services-prod-fraud-detection'}
# Producer/consumer labels that are already applications in this catalog, or are not applications.
QUEUE_APP_ALIASES = {'ComplianceManager': 'Compliance Manager',
                     'TriggerAggregator': 'Trigger Aggregator'}
QUEUE_APP_SYSTEM  = {'FraudDetection': 'Visibility'}      # consumer; its producer is a TBD LITE app
# Cells whose value is too uncertain to act on (question marks in the wiki), and the wildcard
# that means 'and others'. Both are recorded in Live_Seed_README.md instead.
QUEUE_APP_SKIP = {'*', 'LocationHandler?', 'Notification? BulkLoad?', 'Notification?', 'BulkLoad?'}

def queue_apps(cell, alias=True):
    """Application names from a producer/consumer cell (split on <br/> and commas).

    Names are normalised to the catalog's own spelling, so the Producer/Consumer tag values
    join straight onto Resource.ResourceName - which is how Live_Seed_PostImport.sql derives
    the application-to-queue edges without a hardcoded list."""
    out = []
    for piece in re.split(r'<br\s*/?>|,', cell):
        nm = clean(piece).replace('`', '').strip()
        if alias:
            nm = QUEUE_APP_ALIASES.get(nm, nm)
        if nm and nm not in QUEUE_APP_SKIP and nm not in out:
            out.append(nm)
    return out

queue_edges = []                                   # (application key, queue key) - cross-domain

def add_queue(qname, tier, env=None, producers=(), consumers=(), msgtypes=()):
    """Create or extend one Queue resource, and record its application edges.

    A queue name exists in both namespaces, so the dict key carries the tier while the
    resource's own key stays the bare queue name - identity is Domain + Type + Key."""
    ns, rg, sub = NAMESPACES[tier]
    qkey = slug(qname + '-' + tier)
    r = resources.setdefault(qkey, {
        'key': qname, 'name': qname, 'type': 'Queue', 'description': 'Service Bus queue',
        'tags': {'Domain': tier, 'PortalUrl': PORTAL % (sub, rg, ns, qname),
                 'ResourceGroup': rg.lower(), 'Subscription': SUBS[sub][0]},
        'dependencies': [slug(ns)]})                    # queue and namespace share a tier
    if env:
        cur = r['tags'].setdefault('Environment', [])
        if env not in cur: cur.append(env)
    for key, values in (('Producer', producers), ('Consumer', consumers), ('MessageType', msgtypes)):
        for v in values:
            cur = r['tags'].setdefault(key, [])
            if v not in cur: cur.append(v)
    for name in list(producers) + list(consumers):
        edge = (slug(name), qname)
        if edge not in queue_edges:
            queue_edges.append(edge)
    return r
for line in io.open(QUEUE_SOURCE, encoding='utf-8'):
    m = re.match(r'\|\s*`([^`]+)`\s*\|(.*)\|\s*$', line.rstrip())
    if not m:
        continue
    qname = m.group(1).strip()
    cells = m.group(2).split('|')
    consumers = queue_apps(cells[0]) if len(cells) > 0 else []
    producers = queue_apps(cells[1]) if len(cells) > 1 else []
    msgtypes  = queue_apps(cells[2], alias=False) if len(cells) > 2 else []

    # Every named producer/consumer becomes an Application, defaulting to the LITE system.
    for name in consumers + producers:
        if slug(name) in resources:
            continue
        system = QUEUE_APP_SYSTEM.get(name, 'LITE')
        resources[slug(name)] = {'key': slug(name), 'name': name, 'type': 'Application',
                                 'description': system + ' application', 'tags':
                                     {'Domain': 'shared', 'System': system},
                                 'dependencies': [slug(system)]}

    tiers = ['prod'] if qname in PROD_ONLY_QUEUES else ['prod', 'non-prod']
    for tier in tiers:
        add_queue(qname, tier, producers=producers, consumers=consumers, msgtypes=msgtypes)

# ---- 6. per-application, per-environment queues --------------------------------
# The Alerting and Services queues follow a strict naming convention, so each name yields its
# environment and the application it serves. Enumerated from the two namespaces with
# `az servicebus queue list` (verified 2026-09-13) rather than from the wiki, whose Service Bus
# table still names queues 'db-aggregator' that do not exist.
#
# Deliberately excluded:
#   macropoint-alerting-{prod,ppd}-db-ingestor  -- superseded by heavy-ingestor; prod has not
#                                                  finished the rename, non-prod has.
#   macropoint-alerting-shyam-trigger-ingestor  -- a developer's personal queue.
QUEUE_ENVIRONMENTS = {              # name infix -> (environment, tier)
    'lh':   ('localhost',   'non-prod'),
    'dev':  ('development', 'non-prod'),
    'ppd':  ('preprod',     'prod'),
    'pvw':  ('preview',     'prod'),
    'prod': ('production',  'prod'),
}
CONVENTION_QUEUES = [
    # (prefix, name suffix -> application, environments the queue exists in)
    ('macropoint-alerting', {
        'alert-aggregator':   'Alert Aggregator',
        'compliance-manager': 'Compliance Manager',
        'heavy-ingestor':     'Heavy Ingestor',
        'rule-manager':       'Rule Manager',
        'rule-processor':     'Rule Processor',
        'signal-processor':   'Signal Processor',
        'trigger-aggregator': 'Trigger Aggregator',
        'trigger-ingestor':   'Trigger Ingestor',
    }, ['lh', 'dev', 'ppd', 'prod']),
    ('macropoint-services', {
        'fraud-detection': 'FraudDetection',
    }, ['lh', 'dev', 'ppd', 'pvw', 'prod']),
]
for prefix, suffix_app, envs in CONVENTION_QUEUES:
    for infix in envs:
        env, tier = QUEUE_ENVIRONMENTS[infix]
        for suffix, app in suffix_app.items():
            add_queue('%s-%s-%s' % (prefix, infix, suffix), tier, env=env, consumers=[app])

# Section 4 no longer reads the wiki's Service Bus table, so drop any stale tag it once set.
for r in resources.values():
    r['tags'].pop('ServiceBusQueue', None)

# Per-resource description corrections (see RESOURCE_DESCRIPTIONS).
for rkey, text in RESOURCE_DESCRIPTIONS.items():
    if rkey in resources:
        resources[rkey]['description'] = text
    else:
        unrepresentable.append(('stale-override', 'RESOURCE_DESCRIPTIONS', rkey + ' no longer exists'))

# Environment lists of one value collapse to a bare string (single-valued in the contract).
for r in resources.values():
    envs = r['tags'].get('Environment')
    if isinstance(envs, list) and len(envs) == 1:
        r['tags']['Environment'] = envs[0]

doc = {'version': '1.0', 'policy': {'onConflict': 'upsert'}, 'resources': list(resources.values())}
json.dump(doc, io.open(os.path.join(B, 'LiveCatalog.json'), 'w', encoding='utf-8'), indent=2)
json.dump({'crossDomainEdges': xdomain_edges},
          io.open(os.path.join(B, 'cross-domain-edges.json'), 'w', encoding='utf-8'), indent=2)

types = {}
for r in resources.values():
    types[r['type']] = types.get(r['type'], 0) + 1
print(str(len(resources)) + ' resources across ' + str(len(types)) + ' types')
for t, c in sorted(types.items()):
    print('   %3d  %s' % (c, t))
print('\n' + str(len(xdomain_edges)) + ' cross-domain edges for the SQL script (Application -> App Service):')
for a, s in xdomain_edges:
    print('   %-14s -> %s' % (a, s))
print('\nNOT REPRESENTABLE / DEFERRED:')
for kind, where, what in unrepresentable:
    print('   [%s] %s: %s' % (kind, where, what))
print('\nWIKI CORRECTIONS APPLIED:')
for app, env, was, now in corrections_made:
    print('   %s [%s]: %r -> %r' % (app, env, was, now))
