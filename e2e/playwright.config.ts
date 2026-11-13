import { defineConfig, devices } from '@playwright/test';

const PORT = 5080;
const BASE_URL = `http://localhost:${PORT}`;

/**
 * Starts the API with ASPNETCORE_ENVIRONMENT=E2E (see src/OpsTrack.Api/appsettings.E2E.json):
 * a separate SQLite file (opstrack-e2e.db) that is deleted and re-seeded on every start.
 * --no-launch-profile is needed, otherwise launchSettings.json would force "Development".
 * Stop any API you already run on port 5080 before running these tests.
 */
export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  retries: 0,
  reporter: [['list']],
  use: {
    baseURL: BASE_URL,
    trace: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: {
    command: `dotnet run --project ../src/OpsTrack.Api --no-launch-profile --urls ${BASE_URL}`,
    url: `${BASE_URL}/api/meta`,
    timeout: 180_000,
    reuseExistingServer: false,
    env: {
      ASPNETCORE_ENVIRONMENT: 'E2E',
    },
    stdout: 'ignore',
    stderr: 'pipe',
  },
});
