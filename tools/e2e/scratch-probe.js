const { launch, waitForServer } = require('./mud-helpers');
const BASE_URL = process.env.E2E_BASE_URL || 'http://localhost:5200';

(async () => {
  await waitForServer(BASE_URL);
  const { browser, page } = await launch();
  await page.setViewportSize({ width: 1600, height: 1000 });
  await page.goto(`${BASE_URL}/resources/DEMOEXP-mobile-bff`, { waitUntil: 'networkidle' });
  await page.waitForSelector('.rm-form-row', { timeout: 15000 });
  await page.waitForTimeout(400);

  console.log(JSON.stringify(await page.evaluate(() => {
    const out = {};
    // What classes does the tab-panel container actually carry?
    out.tabsChildren = [...document.querySelectorAll('.rm-editor-tabs > *')].map(e => e.className);
    const panels = document.querySelector('.mud-tabs-panels');
    out.panels = panels ? { cls: panels.className, sh: panels.scrollHeight, ch: panels.clientHeight,
                            overflowY: getComputedStyle(panels).overflowY } : null;

    // The Key row is a plain MudTextField — inspect its real input classes + font.
    const rows = [...document.querySelectorAll('.rm-form-row')];
    const keyRow = rows.find(r => r.querySelector('.rm-form-label')?.textContent.includes('Key'));
    const inp = keyRow?.querySelector('input');
    const ctrl = keyRow?.querySelector('.mud-input-control');
    const inner = keyRow?.querySelector('.mud-input');
    out.key = {
      inputCls: inp?.className,
      inputFont: inp ? getComputedStyle(inp).fontSize : null,
      controlH: ctrl ? Math.round(ctrl.getBoundingClientRect().height) : null,
      inputBoxCls: inner?.className,
      inputBoxH: inner ? Math.round(inner.getBoundingClientRect().height) : null,
    };
    return out;
  }), null, 1));

  await browser.close();
})().catch(e => { console.error('SCRIPT_ERROR', e); process.exit(1); });
