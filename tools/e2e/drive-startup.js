const { chromium } = require('playwright-core');
const API = 'http://localhost:5200/api/saved-views';

const api = async (page, method, path, body) => page.evaluate(async ([m, p, b]) => {
  const r = await fetch(p, {
    method: m,
    headers: b ? { 'Content-Type': 'application/json' } : undefined,
    body: b ? JSON.stringify(b) : undefined
  });
  return r.status === 204 ? null : await r.json().catch(() => null);
}, [method, path, body]);

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage({ viewport: { width: 1500, height: 950 } });
  await page.goto('http://localhost:5200/', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);

  // A view narrow enough that its row count is unmistakable.
  const created = await api(page, 'POST', API, { name: 'E2eStartup', queryString: '?q=Redis&n=100&s=ResourceName:asc' });
  const uid = created.data.savedViewUid;

  // --- no default set: a bare URL must NOT show the saved view ---
  await api(page, 'PUT', API + '/default');
  await page.goto('http://localhost:5200/', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  console.log('no default   -> rows', await page.locator('table tbody tr').count(),
              '| url', new URL(page.url()).search || '(bare)');

  // --- default set: a bare URL must land on it ---
  await api(page, 'PUT', `${API}/${uid}/default`);
  await page.goto('http://localhost:5200/', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3500);
  console.log('default set  -> rows', await page.locator('table tbody tr').count(),
              '| url', new URL(page.url()).search);

  // --- an explicit URL must still win over the default ---
  await page.goto('http://localhost:5200/?q=Queue&n=100&s=ResourceName:asc', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3500);
  console.log('explicit url -> rows', await page.locator('table tbody tr').count(),
              '| url', new URL(page.url()).search);

  await api(page, 'DELETE', `${API}/${uid}`);
  await browser.close();
})().catch(e => { console.error('FAILED:', e.message); process.exit(1); });
