const path = require('path');
const { launch, waitForServer } = require('./mud-helpers');
const BASE_URL = 'http://localhost:5200';
const shotDir = process.env.E2E_SHOT_DIR;
const ROUTES = [
  ['/', 'home'],
  ['/resources', 'create'],
  ['/resources/DEMOEXP-mobile-bff', 'details'],
  ['/resource-types', 'types'],
  ['/import', 'import'],
  ['/explore/DEMOEXP-checkout', 'explore'],
];
(async () => {
  await waitForServer(BASE_URL);
  const { browser, page } = await launch();
  await page.setViewportSize({ width: 1600, height: 1000 });
  for (const [route, name] of ROUTES) {
    const resp = await page.goto(BASE_URL + route, { waitUntil: 'networkidle' }).catch(e => null);
    await page.waitForTimeout(900);
    const info = await page.evaluate(() => ({
      title: document.querySelector('.rm-shell-title')?.textContent.trim() || '(none)',
      crumbs: [...document.querySelectorAll('.rm-breadcrumb a, .rm-breadcrumb li')]
        .map(e => e.textContent.trim()).filter(t => t && t !== '›'),
      errs: [...document.querySelectorAll('.mud-alert-filled-error, .mud-alert-outlined-error')]
        .map(e => e.textContent.trim().slice(0, 70)),
      docScrolls: document.documentElement.scrollHeight > document.documentElement.clientHeight + 1,
    }));
    console.log(`${route} -> ${resp ? resp.status() : 'ERR'} title=${JSON.stringify(info.title)} crumbs=${JSON.stringify(info.crumbs)} docScrolls=${info.docScrolls} errs=${JSON.stringify(info.errs)}`);
    await page.screenshot({ path: path.join(shotDir, `route-${name}.png`) });
  }
  console.log('CONSOLE_ERRORS=' + JSON.stringify(page.consoleErrors));
  await browser.close();
})().catch(e => { console.error('SCRIPT_ERROR', e); process.exit(1); });
