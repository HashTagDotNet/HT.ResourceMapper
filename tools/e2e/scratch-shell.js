// Scratch driver for azure-ux-design-v1 steps 1-3: shell (fixed chrome + one scroll region),
// footer wizard nav, and the General tab's field anatomy. Also measures the §4a claims.
const path = require('path');
const { launch, waitForServer } = require('./mud-helpers');

const BASE_URL = process.env.E2E_BASE_URL || 'http://localhost:5200';
const shotDir = process.env.E2E_SHOT_DIR || __dirname;
const UID = process.env.E2E_UID || 'DEMOEXP-mobile-bff';
const W = parseInt(process.env.E2E_W || '1600', 10);

(async () => {
  await waitForServer(BASE_URL);
  const { browser, page } = await launch();
  await page.setViewportSize({ width: W, height: 1000 });

  await page.goto(`${BASE_URL}/resources/${UID}`, { waitUntil: 'networkidle' });
  await page.waitForSelector('.rm-page-shell', { timeout: 15000 });
  await page.waitForTimeout(500);

  // Only the body region may scroll.
  const scroll0 = null; const scroll = await page.evaluate(() => {
    const g = s => document.querySelector(s);
    const m = (el) => el ? { scrollH: el.scrollHeight, clientH: el.clientHeight,
                             scrolls: el.scrollHeight > el.clientHeight + 1 } : null;
    return { main: m(g('.rm-main')), panels: m(g('.mud-tabs-panels')),
             headFixed: !!g('.rm-shell-head'), footFixed: !!g('.rm-shell-foot') };
  });
  console.log('SCROLL=' + JSON.stringify(scroll));

  console.log('BREADCRUMB=' + JSON.stringify(
    await page.$$eval('.rm-breadcrumb li, .rm-breadcrumb a, .rm-breadcrumb .mud-breadcrumb-item',
      els => [...new Set(els.map(e => e.textContent.trim()).filter(Boolean))])));

  // Field anatomy: label column width, field height, label/field/helper font sizes.
  const anatomy = await page.evaluate(() => {
    const row = document.querySelector('.rm-form-row');
    if (!row) return null;
    const label = row.querySelector('.rm-form-label');
    const field = row.querySelector('.rm-form-field .mud-input');
    const input = row.querySelector('.rm-form-field input, .rm-form-field .mud-input-root');
    const helper = document.querySelector('.rm-form-field .mud-input-helper-text');
    const cs = e => e ? getComputedStyle(e) : null;
    return {
      gridCols: getComputedStyle(row).gridTemplateColumns,
      labelFont: cs(label)?.fontSize,
      fieldHeight: field ? Math.round(field.getBoundingClientRect().height) : null,
      inputFont: cs(input)?.fontSize,
      helperFont: cs(helper)?.fontSize,
      infoIcons: document.querySelectorAll('.rm-form-info').length,
      helpers: document.querySelectorAll('.rm-form-field .mud-input-helper-text').length,
      rows: document.querySelectorAll('.rm-form-row').length,
      sections: document.querySelectorAll('.rm-section-heading').length,
    };
  });
  console.log('ANATOMY=' + JSON.stringify(anatomy));

  // Footer wizard nav.
  const footBtns = await page.$$eval('.rm-shell-foot button',
    els => els.map(e => ({ text: e.textContent.trim(), disabled: e.disabled })));
  console.log('FOOTER=' + JSON.stringify(footBtns));
  await page.screenshot({ path: path.join(shotDir, `shell-${W}-general.png`) });

  // Next should move to Tags and relabel to the following destination.
  await page.locator('.rm-shell-foot button', { hasText: 'Next' }).click();
  await page.waitForTimeout(500);
  const afterNext = await page.$$eval('.rm-shell-foot button',
    els => els.map(e => ({ text: e.textContent.trim(), disabled: e.disabled })));
  const activeTab = await page.$eval('.mud-tab.mud-tab-active', e => e.textContent.trim()).catch(() => '?');
  console.log('AFTER_NEXT activeTab=' + JSON.stringify(activeTab) + ' footer=' + JSON.stringify(afterNext));
  await page.screenshot({ path: path.join(shotDir, `shell-${W}-tags.png`) });

  console.log('CONSOLE_ERRORS=' + JSON.stringify(page.consoleErrors));
  await browser.close();
})().catch(err => { console.error('SCRIPT_ERROR', err); process.exit(1); });
