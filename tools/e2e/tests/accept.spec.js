// Beta slice 9 acceptance walk (HardReview-Simplify.md section 0). Creates ZZ Accept * resources through
// quick add, drives each UI criterion, then removes them in afterAll. Not part of the regression suite.
const { test, expect } = require('@playwright/test');
const { execFileSync } = require('child_process');
const { DOMAIN_LABEL, pickOption } = require('../mud-helpers');

const SHOTS = 'C:/Users/spowell/AppData/Local/Temp/claude/C--src-HT-ResourceMapper/72e475d7-8807-4dc8-be52-9c14d039e6af/scratchpad';
const SQLCMD = 'C:\\Program Files\\Microsoft SQL Server\\Client SDK\\ODBC\\170\\Tools\\Binn\\SQLCMD.EXE';
const IGNORED = [/404 \(Not Found\)/];
const SUB_OLD = '1d6dcbc4-27e3-4f74-a34d-89a09505ccd3';
const SUB_NEW = '6904df25-40a0-4531-9fb0-f6fde48f6785';
const RG_FRAGMENT = 'rg-zzaccept-eastus2';

const ID = {
  alphaOld: `/subscriptions/${SUB_OLD}/resourceGroups/usspi-rsg-zzaccept-01/providers/Microsoft.Web/sites/usspi-aps-zzaccept-alpha`,
  alphaNew: `/subscriptions/${SUB_NEW}/resourceGroups/${RG_FRAGMENT}/providers/Microsoft.Web/sites/app-zzaccept-alpha-eus2`,
  bravo: `/subscriptions/${SUB_NEW}/resourceGroups/${RG_FRAGMENT}/providers/Microsoft.Web/sites/app-zzaccept-bravo-eus2`,
  charlie: `/subscriptions/${SUB_NEW}/resourceGroups/${RG_FRAGMENT}/providers/Microsoft.Web/sites/app-zzaccept-charlie-eus2`,
};

function sql(q) {
  return execFileSync(SQLCMD, ['-S', '(localdb)\\MSSQLLocalDB', '-d', 'ResourceExplorer', '-I', '-f', '65001', '-W', '-h', '-1', '-Q', 'SET NOCOUNT ON; ' + q]).toString().trim();
}

function cleanup() {
  sql(`DELETE rel FROM ResourceExplorer.ResourceRelationship rel JOIN ResourceExplorer.Resource r ON r.ResourceId IN (rel.FromResourceId, rel.ToResourceId) WHERE r.ResourceName LIKE 'ZZ Accept%';
       DELETE t FROM ResourceExplorer.ResourceTag t JOIN ResourceExplorer.Resource r ON r.ResourceId=t.ResourceId WHERE r.ResourceName LIKE 'ZZ Accept%';
       DELETE FROM ResourceExplorer.Resource WHERE ResourceName LIKE 'ZZ Accept%';`);
}

const tagRow = (page, label) => page.locator('.rm-form-row').filter({ has: page.locator('.rm-form-label', { hasText: label }) }).first();
const shot = (page, n) => page.screenshot({ path: `${SHOTS}/accept-${n}.png`, fullPage: true });

async function selectType(page, typeName) {
  const trigger = page.locator('.rm-form-row').filter({ has: page.locator('.rm-form-label', { hasText: /^Type/ }) })
    .locator('.mud-select-input:visible').first();
  await page.waitForTimeout(1500);
  await trigger.click();
  try { await page.getByRole('option', { name: typeName, exact: true }).waitFor({ timeout: 4000 }); }
  catch {
    console.log('options seen:', await page.getByRole('option').allInnerTexts());
    await trigger.click();
  }
  await pickOption(page, typeName);
  await expect(page.getByText('Key Tags', { exact: true })).toBeVisible({ timeout: 8000 });
}

async function addAzureId(page, id) {
  const azure = tagRow(page, 'Azure Resource Id');
  await azure.locator('input:visible').first().fill(id);
  await azure.getByRole('button', { name: 'Add this value to the tag' }).click();
  await expect(azure.locator('.mud-chip', { hasText: id })).toBeVisible();
}

async function fillPortal(page) {
  const row = tagRow(page, 'Portal');
  await row.locator('input:visible').first().fill('https://portal.azure.com/zzaccept');
  await page.keyboard.press('Tab');
  const add = row.getByRole('button', { name: 'Add this value to the tag' });
  if (await add.count() > 0) await add.click();
}

/** Quick-adds one App Service resource; returns the new resource's uid. */
async function quickAdd(page, { name, ids, dependsOn }) {
  await page.goto('/resources/new');
  await selectType(page, 'App Service');
  await page.getByLabel('Name', { exact: true }).fill(name);
  await page.keyboard.press('Tab');
  await expect(page.getByText('Not yet catalogued as this type.')).toBeVisible({ timeout: 8000 });
  for (const id of ids) await addAzureId(page, id);
  await fillPortal(page);
  if (dependsOn) {
    await page.getByPlaceholder('Add existing resource…').fill(dependsOn);
    await page.getByRole('option', { name: new RegExp(dependsOn) }).first().click();
    await expect(page.locator('.rm-dep-row', { hasText: dependsOn })).toBeVisible();
  }
  await page.getByRole('button', { name: 'Save', exact: true }).click();
  await page.waitForURL(/\/resources\/[0-9a-f-]{36}\/view$/, { timeout: 10000 });
  return page.url().match(/resources\/([0-9a-f-]{36})\/view/)[1];
}

const rows = page => page.locator('.mud-table-body .mud-table-row');
const search = page => page.getByLabel('Filter for any field');
async function searchFor(page, text) {
  await page.goto('/');
  await expect(rows(page).first()).toBeVisible();
  await page.waitForTimeout(1500); // let the circuit attach, else the first fill is wiped by the interactive re-render
  await search(page).fill(text);
  await page.keyboard.press('Tab');
  await expect(page.getByText(/^Showing 100 of/)).toHaveCount(0, { timeout: 8000 });
  await page.waitForTimeout(500);
}

test.describe.configure({ mode: 'serial' });

test.describe('Beta acceptance', () => {
  const uids = {};
  const consoleErrors = [];

  test.beforeAll(() => cleanup());
  test.afterAll(() => cleanup());
  test.beforeEach(({ page }) => {
    page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); });
    page.on('pageerror', e => consoleErrors.push('pageerror: ' + e.message));
  });

  test('C6 quick entry: Alpha (two ids), Bravo (all fields on one form), Charlie', async ({ page }) => {
    uids.alpha = await quickAdd(page, { name: 'ZZ Accept Alpha', ids: [ID.alphaOld, ID.alphaNew] });
    await shot(page, 'c6-alpha-view');

    // Bravo is the "single form" subject: Name, Type, one Azure id, one tag, one dependency.
    await page.goto('/resources/new');
    await expect(tagRow(page, DOMAIN_LABEL)).toHaveCount(0);
    await selectType(page, 'App Service');
    await page.getByLabel('Name', { exact: true }).fill('ZZ Accept Bravo');
    await page.keyboard.press('Tab');
    await expect(page.getByText('Not yet catalogued as this type.')).toBeVisible({ timeout: 8000 });
    await addAzureId(page, ID.bravo);
    await fillPortal(page);
    await page.getByPlaceholder('Add existing resource…').fill('ZZ Accept Alpha');
    await page.getByRole('option', { name: /ZZ Accept Alpha/ }).first().click();
    await expect(page.locator('.rm-dep-row', { hasText: 'ZZ Accept Alpha' })).toBeVisible();
    await page.getByRole('button', { name: 'More Options' }).click();
    await expect(page.getByLabel('Key', { exact: true })).toHaveValue('zz-accept-bravo');
    await expect(tagRow(page, DOMAIN_LABEL).locator('.mud-select-input:visible').first()).toContainText('non-prod');
    await expect(page.getByRole('tab')).toHaveCount(0);
    await expect(page.locator('.mud-dialog')).toHaveCount(0);
    await shot(page, 'c6-bravo-form');
    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForURL(/\/resources\/[0-9a-f-]{36}\/view$/, { timeout: 10000 });
    uids.bravo = page.url().match(/resources\/([0-9a-f-]{36})\/view/)[1];

    uids.charlie = await quickAdd(page, { name: 'ZZ Accept Charlie', ids: [ID.charlie], dependsOn: 'ZZ Accept Bravo' });
    await shot(page, 'c6-charlie-view');
    await expect(page.locator('[data-testid="depends-on"]').getByRole('link', { name: 'ZZ Accept Bravo' })).toBeVisible();
  });

  test('C1 name search returns the resource and its view shows every Azure id with Old/New', async ({ page }) => {
    await searchFor(page, 'ZZ Accept Alpha');
    await expect(rows(page).filter({ hasText: 'ZZ Accept Alpha' })).toHaveCount(1);
    await shot(page, 'c1-search-name');
    await rows(page).filter({ hasText: 'ZZ Accept Alpha' }).getByRole('link', { name: 'ZZ Accept Alpha' }).click();
    await page.waitForURL(/\/view$/);
    const ids = page.locator('[data-testid="azure-ids"] .rm-view-idrow');
    await expect(ids).toHaveCount(2);
    await expect(ids.filter({ hasText: 'usspi-aps-zzaccept-alpha' }).locator('.rm-view-badge')).toHaveText('Old');
    await expect(ids.filter({ hasText: 'app-zzaccept-alpha-eus2' }).locator('.rm-view-badge')).toHaveText('New');
    await shot(page, 'c1-view-alpha');
    // copy affordance
    await expect(page.locator('[data-testid="azure-ids"]').getByRole('button', { name: /copy/i }).first()).toBeVisible();
  });

  test('C2 full ARM id (old and new) and trailing segment return the owner', async ({ page }) => {
    for (const [i, q] of [ID.alphaOld, ID.alphaNew, 'usspi-aps-zzaccept-alpha', 'app-zzaccept-alpha-eus2'].entries()) {
      await searchFor(page, q);
      const r = rows(page);
      await expect(r, `search ${q}`).toHaveCount(1);
      await expect(r.first()).toContainText('ZZ Accept Alpha');
      await shot(page, `c2-search-${i}`);
    }
  });

  test('C3 resource-group fragment returns every resource whose Azure id contains it', async ({ page }) => {
    await searchFor(page, RG_FRAGMENT);
    const r = rows(page);
    await expect(r).toHaveCount(3);
    for (const n of ['Alpha', 'Bravo', 'Charlie']) await expect(r.filter({ hasText: `ZZ Accept ${n}` })).toHaveCount(1);
    await shot(page, 'c3-rg-fragment');
    // sanity: subscription fragment too
    await searchFor(page, 'usspi-rsg-zzaccept-01');
    await expect(rows(page)).toHaveCount(1);
  });

  test('C4/C5 Used By and Depends On expand hop by hop until none; repeats marked', async ({ page }) => {
    await page.goto(`/resources/${uids.alpha}/view`);
    const usedBy = page.locator('[data-testid="used-by"]');
    await expect(usedBy.locator('.rm-view-row')).toHaveCount(1);
    await usedBy.getByRole('button', { name: 'Expand ZZ Accept Bravo' }).click();
    await expect(usedBy.locator('.rm-view-row[data-uid="' + uids.charlie + '"]')).toBeVisible();
    await usedBy.getByRole('button', { name: 'Expand ZZ Accept Charlie' }).click();
    await expect(usedBy.getByText('None.')).toBeVisible();
    await shot(page, 'c4-used-by-chain');

    await page.goto(`/resources/${uids.charlie}/view`);
    const dep = page.locator('[data-testid="depends-on"]');
    await dep.getByRole('button', { name: 'Expand ZZ Accept Bravo' }).click();
    await dep.getByRole('button', { name: 'Expand ZZ Accept Alpha' }).click();
    await expect(dep.getByText('None.')).toBeVisible();
    await shot(page, 'c5-depends-on-chain');

    // Close the loop in SQL (Alpha depends on Charlie): the third hop must be marked, not expandable.
    sql(`INSERT ResourceExplorer.ResourceRelationship(FromResourceId,ToResourceId) SELECT a.ResourceId,c.ResourceId FROM ResourceExplorer.Resource a, ResourceExplorer.Resource c WHERE a.ResourceName='ZZ Accept Alpha' AND c.ResourceName='ZZ Accept Charlie'`);
    await page.goto(`/resources/${uids.alpha}/view`);
    const ub = page.locator('[data-testid="used-by"]');
    await ub.getByRole('button', { name: 'Expand ZZ Accept Bravo' }).click();
    await ub.getByRole('button', { name: 'Expand ZZ Accept Charlie' }).click();
    const cyc = ub.locator('.rm-view-row[data-uid="' + uids.alpha + '"]');
    await expect(cyc).toContainText('(cycle)');
    await expect(cyc.getByRole('button')).toHaveCount(0);
    await shot(page, 'c4-used-by-cycle');
    const dp = page.locator('[data-testid="depends-on"]');
    await dp.getByRole('button', { name: 'Expand ZZ Accept Charlie' }).click();
    await dp.getByRole('button', { name: 'Expand ZZ Accept Bravo' }).click();
    await expect(dp.locator('.rm-view-row[data-uid="' + uids.alpha + '"]')).toContainText('(cycle)');
    await shot(page, 'c5-depends-on-cycle');
    sql(`DELETE rel FROM ResourceExplorer.ResourceRelationship rel JOIN ResourceExplorer.Resource a ON a.ResourceId=rel.FromResourceId JOIN ResourceExplorer.Resource c ON c.ResourceId=rel.ToResourceId WHERE a.ResourceName='ZZ Accept Alpha' AND c.ResourceName='ZZ Accept Charlie'`);
  });

  test('C7a duplicate identity refused from the quick-add UI', async ({ page }) => {
    await page.goto('/resources/new');
    await selectType(page, 'App Service');
    await page.getByLabel('Name', { exact: true }).fill('ZZ Accept Alpha');
    await page.keyboard.press('Tab');
    await page.waitForTimeout(1500);
    await fillPortal(page);
    await shot(page, 'c7-duplicate-form');
    const before = sql(`SELECT COUNT(*) FROM ResourceExplorer.Resource WHERE ResourceName='ZZ Accept Alpha'`);
    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForTimeout(2000);
    await shot(page, 'c7-duplicate-after-save');
    expect(page.url()).toMatch(/\/resources\/new$/);
    expect(sql(`SELECT COUNT(*) FROM ResourceExplorer.Resource WHERE ResourceName='ZZ Accept Alpha'`)).toBe(before);
    console.log('C7a page text:', (await page.locator('body').innerText()).split('\n').filter(l => /already|catalogued|exists|duplicate/i.test(l)).join(' | '));
  });

  test('C7b duplicate refused by the import endpoint, including with the domain omitted', async ({ request }) => {
    const key = 'zz-accept-alpha';
    const res = (extra = {}) => ({ version: '1.0', policy: { onConflict: 'fail' }, defaults: extra.defaults ?? {}, resources: [{ key, name: 'ZZ Accept Alpha', type: 'App Service', tags: extra.tags ?? {} }] });
    const withDomain = await request.post('/api/resources/import', { data: res({ defaults: { domain: 'non-prod' }, tags: { PortalUrl: 'https://portal.azure.com/x' } }) });
    console.log('C7b with domain:', withDomain.status(), JSON.stringify(await withDomain.json()));
    const noDomain = await request.post('/api/resources/import', { data: res({ tags: { PortalUrl: 'https://portal.azure.com/x' } }) });
    console.log('C7b no domain:', noDomain.status(), JSON.stringify(await noDomain.json()));
    const noDomainUpsert = await request.post('/api/resources/import', { data: { ...res({ tags: { PortalUrl: 'https://portal.azure.com/x' } }), policy: { onConflict: 'upsert' } } });
    console.log('C7b no domain upsert:', noDomainUpsert.status(), JSON.stringify(await noDomainUpsert.json()));
    expect(withDomain.status()).toBe(400);
    expect(noDomain.status()).toBe(400);
    expect(noDomainUpsert.status()).toBe(400);
    expect(sql(`SELECT COUNT(*) FROM ResourceExplorer.Resource WHERE ResourceName='ZZ Accept Alpha'`)).toBe('1');
  });

  test('console is clean', async () => {
    const unexpected = consoleErrors.filter(e => !IGNORED.some(p => p.test(e)));
    expect(unexpected).toEqual([]);
  });
});
