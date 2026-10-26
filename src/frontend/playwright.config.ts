import { defineConfig, devices } from '@playwright/test';

/** PORT lets several workspaces run their suites side by side; e2e/serve.mjs reads the same variable. */
const port = Number(process.env['PORT'] ?? 4200);

export default defineConfig({
  testDir: './e2e/tests',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  reporter: process.env['CI'] ? 'github' : 'list',
  use: { baseURL: `http://localhost:${port}`, trace: 'retain-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'node e2e/serve.mjs',
    url: `http://localhost:${port}`,
    reuseExistingServer: !process.env['CI'],
  },
});
