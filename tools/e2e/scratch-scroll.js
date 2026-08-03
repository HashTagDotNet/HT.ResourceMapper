const { launch, waitForServer } = require('./mud-helpers');
const BASE_URL = 'http://localhost:5200';
(async () => {
  await waitForServer(BASE_URL);
  const { browser, page } = await launch();
  // Deliberately short viewport so the form MUST overflow.
  await page.setViewportSize({ width: 1400, height: 560 });
  await page.goto(`${BASE_URL}/resources/DEMOEXP-mobile-bff`, { waitUntil: 'networkidle' });
  await page.waitForSelector('.rm-form-row', { timeout: 15000 });
  await page.waitForTimeout(500);

  const before = await page.evaluate(() => {
    const g = s => document.querySelector(s);
    const r = el => el ? Math.round(el.getBoundingClientRect().top) : null;
    const m = el => el ? { sh: el.scrollHeight, ch: el.clientHeight, scrolls: el.scrollHeight > el.clientHeight + 1 } : null;
    return { doc: { sh: document.documentElement.scrollHeight, ch: document.documentElement.clientHeight },
             main: m(g('.rm-main')), panels: m(g('.mud-tabs-panels')),
             tabbarTop: r(g('.mud-tabs-tabbar')), footTop: r(g('.rm-shell-foot')),
             titleTop: r(g('.rm-shell-title')) };
  });
  console.log('BEFORE=' + JSON.stringify(before));

  // Scroll the panel region to the bottom.
  await page.evaluate(() => { const p = document.querySelector('.mud-tabs-panels'); p.scrollTop = p.scrollHeight; });
  await page.waitForTimeout(300);
  const after = await page.evaluate(() => {
    const g = s => document.querySelector(s);
    const r = el => el ? Math.round(el.getBoundingClientRect().top) : null;
    return { panelScrollTop: Math.round(g('.mud-tabs-panels').scrollTop),
             tabbarTop: r(g('.mud-tabs-tabbar')), footTop: r(g('.rm-shell-foot')), titleTop: r(g('.rm-shell-title')) };
  });
  console.log('AFTER_SCROLL=' + JSON.stringify(after));
  console.log('CHROME_STAYED_PUT=' + (before.tabbarTop === after.tabbarTop && before.footTop === after.footTop && before.titleTop === after.titleTop));
  await page.screenshot({ path: process.env.E2E_SHOT_DIR + '/scroll-short.png' });
  await browser.close();
})().catch(e => { console.error('SCRIPT_ERROR', e); process.exit(1); });
