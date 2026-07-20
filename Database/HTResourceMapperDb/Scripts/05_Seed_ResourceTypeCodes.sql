-- Per-resource-type short codes + icon keys for the explorer node badges. Idempotent (UPDATE by
-- TypeName). IconKey is an opaque key the UI maps to a glyph.
UPDATE t
SET ShortCode = v.ShortCode, IconKey = v.IconKey, UpdatedOn = SYSUTCDATETIME()
FROM [HTResourceMapper].[ResourceType] t
INNER JOIN (VALUES
    ('Azure App Service',  'APP', 'web'),
    ('Application',        'APL', 'apps'),
    ('App',                'APP', 'web'),
    ('Azure App Config',   'APC', 'settings'),
    ('Azure App Insights', 'AI',  'insights'),
    ('Azure Redis',        'RDS', 'memory'),
    ('Azure ServiceBus',   'SB',  'bus'),
    ('Azure ServcieFabric','SF',  'hub'),
    ('Azure ResourceGroup','RG',  'folder'),
    ('Service',            'SVC', 'dns'),
    ('Database',           'DB',  'database'),
    ('Queue',              'SBQ', 'queue')
) AS v(TypeName, ShortCode, IconKey) ON v.TypeName = t.TypeName;
