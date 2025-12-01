import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,  // Run tests serially for more stability
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 1,  // Allow 1 retry locally
  workers: 1,  // Single worker to avoid race conditions
  reporter: 'html',
  timeout: 60000,  // 60 second timeout per test
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    actionTimeout: 15000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'echo "Using existing Docker stack"',
    url: 'http://localhost:5173',
    reuseExistingServer: true,
    timeout: 5000,
  },
});
