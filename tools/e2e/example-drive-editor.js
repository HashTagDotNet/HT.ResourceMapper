// Example / template driver script — copy this to a scratch location and adapt per slice.
// Demonstrates: launching Chrome, waiting for the dev server, driving MudBlazor selects/dialogs,
// screenshotting each step, and reporting console errors. See ../README.md for setup + gotchas.
//
// Run against an already-running server:
//   ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5200" \
//     dotnet run --project UI/ResourceMapper.UI.Web --no-launch-profile &
//   node example-drive-editor.js

const path = require('path');
const { launch, clickSelect, pickOption, waitForServer } = require('./mud-helpers');

const BASE_URL = process.env.E2E_BASE_URL || 'http://localhost:5200';
const shotDir = process.env.E2E_SHOT_DIR || __dirname;
const shot = (page, n, name) => page.screenshot({ path: path.join(shotDir, `shot-${n}-${name}.png`), fullPage: true });

(async () => {
  await waitForServer(BASE_URL);

  const { browser, page } = await launch();

  await page.goto(`${BASE_URL}/resources`, { waitUntil: 'networkidle' });
  await shot(page, 1, 'create-general');

  // MudSelect: scope by its floating label, click the :visible trigger, pick the popover option.
  await clickSelect(page, 'Resource Type');
  await page.waitForTimeout(200);
  // await pickOption(page, 'YourSeededType');

  // Plain MudTextField: getByLabel works fine (no hidden/visible-sibling quirk on text fields).
  await page.getByLabel('Name', { exact: true }).fill('Example Resource');

  await shot(page, 2, 'general-filled');

  console.log('CONSOLE_ERRORS=' + JSON.stringify(page.consoleErrors));
  await browser.close();
})().catch(err => {
  console.error('SCRIPT_ERROR', err);
  process.exit(1);
});
