const path = require('path');
const { launch, waitForServer } = require('./mud-helpers');
const BASE_URL = 'http://localhost:5200';
const shotDir = process.env.E2E_SHOT_DIR;
(async () => {
  await waitForServer(BASE_URL);
  const { browser, page } = await launch();
  for (const w of [1600, 900]) {
    await page.setViewportSize({ width: w, height: 900 });
    await page.goto(`${BASE_URL}/resources/DEMOEXP-mobile-bff`, { waitUntil: 'networkidle' });
    await page.waitForSelector('.rm-form-row', { timeout: 15000 });
    await page.waitForTimeout(600);
    const info = await page.evaluate(() => {
      const row = document.querySelector('.rm-form-row');
      const label = row.querySelector('.rm-form-label');
      const field = row.querySelector('.rm-form-field');
      const lb = label.getBoundingClientRect(), fb = field.getBoundingClientRect();
      return {
        gridCols: getComputedStyle(row).gridTemplateColumns,
        stacked: Math.round(fb.top) > Math.round(lb.bottom) - 2,   // field below label = stacked
        labelTop: Math.round(lb.top), fieldTop: Math.round(fb.top),
        hOverflow: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
      };
    });
    console.log(`w=${w} ${JSON.stringify(info)}`);
    await page.screenshot({ path: path.join(shotDir, `resp-${w}.png`) });
  }
  console.log('CONSOLE_ERRORS=' + JSON.stringify(page.consoleErrors));
  await browser.close();
})().catch(e => { console.error('SCRIPT_ERROR', e); process.exit(1); });
