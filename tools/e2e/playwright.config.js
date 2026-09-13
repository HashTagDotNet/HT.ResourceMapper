// @ts-check
const { defineConfig } = require('@playwright/test');
const path = require('path');

// Same port as the ad-hoc manual drive convention (README) so a dev server a developer already
// started manually is reused rather than double-started (reuseExistingServer below — RD19).
const PORT = 5200;
const BASE_URL = `http://localhost:${PORT}`;

module.exports = defineConfig({
  testDir: './tests',
  globalSetup: require.resolve('./global-setup'),
  globalTeardown: require.resolve('./global-teardown'),
  // A single shared localdb has no parallel isolation (RD17) — specs must run one at a time.
  workers: 1,
  fullyParallel: false,
  retries: 0,
  timeout: 60_000,
  reporter: [['list']],
  use: {
    baseURL: BASE_URL,
    // System Chrome, no bundled Chromium download (RD20) — the proven #7 tooling setup.
    channel: 'chrome',
    headless: true,
    launchOptions: { args: ['--no-sandbox'] },
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  webServer: {
    command: 'dotnet run --project UI/ResourceMapper.UI.Web --no-launch-profile',
    // Repo root, NOT this folder — the exact cwd bug hit while smoke-testing the #7 tooling
    // (a project-relative `dotnet run` can't resolve its path from tools/e2e) — RD19.
    cwd: path.resolve(__dirname, '..', '..'),
    url: BASE_URL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: {
      // Both names, deliberately: DOTNET_ENVIRONMENT wins over ASPNETCORE_ENVIRONMENT when both are
      // set, so pinning only the latter left the suite at whatever the machine's ambient value was.
      // 'localhost' is the convention here and the name appsettings.localhost.json is selected by.
      DOTNET_ENVIRONMENT: 'localhost',
      ASPNETCORE_ENVIRONMENT: 'localhost',
      ASPNETCORE_URLS: BASE_URL,
    },
  },
});
