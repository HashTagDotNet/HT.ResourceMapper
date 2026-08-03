const { launch, waitForServer, selectTypeAndDomain, fillNameAndWaitForSlug } = require('./mud-helpers');
const BASE_URL = 'http://localhost:5200';
(async () => {
  await waitForServer(BASE_URL);
  const { browser, page } = await launch();
  await page.setViewportSize({ width: 1500, height: 1000 });
  await page.goto(BASE_URL + '/resources', { waitUntil: 'networkidle' });
  await selectTypeAndDomain(page, 'E2eDepType', 'non-prod');
  await page.getByLabel('Name', { exact: true }).fill('E2E Dbg Subject'); await page.keyboard.press('Tab'); await page.waitForTimeout(800);

  await page.getByRole('tab', { name: 'Tags' }).click();
  await page.waitForTimeout(1200);
  const info = await page.evaluate(() => ({
    rowCount: document.querySelectorAll('.rm-tag-row').length,
    reqMarkers: document.querySelectorAll('.rm-tag-required').length,
    rows: [...document.querySelectorAll('.rm-tag-row')].map(r => ({
      label: r.querySelector('.rm-tag-label')?.textContent.trim(),
      required: !!r.querySelector('.rm-tag-required'),
      hasSelect: !!r.querySelector('.mud-select-input'),
      selectVisible: !!r.querySelector('.mud-select-input'),
      inputs: r.querySelectorAll('input').length,
    })),
  }));
  console.log(JSON.stringify(info, null, 1));
  await page.screenshot({ path: process.env.E2E_SHOT_DIR + '/tagdbg.png' });
  await browser.close();
})().catch(e => { console.error('SCRIPT_ERROR', e.message); process.exit(1); });
