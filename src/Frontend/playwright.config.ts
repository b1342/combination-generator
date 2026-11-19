import { defineConfig, devices } from '@playwright/test';

/**
 * בדיקות קצה-לקצה: דפדפן אמיתי, מסך Angular אמיתי ושרת .NET אמיתי.
 * Playwright מפעיל את שניהם לבד (webServer), או משתמש בהם אם הם כבר רצים.
 *
 * דפדפן: ברירת המחדל היא Chromium של Playwright (npx playwright install chromium).
 * אם ההורדה חסומה (למשל ברשת ארגונית), אפשר להשתמש ב-Edge שכבר מותקן: PW_CHANNEL=msedge
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  reporter: [['list'], ['html', { open: 'never' }]],
  timeout: 30_000,
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], channel: process.env['PW_CHANNEL'] },
    },
  ],
  webServer: [
    {
      command: 'dotnet run --project ../Backend/CombinationGenerator.Api --launch-profile http',
      url: 'http://localhost:5080/swagger/index.html',
      reuseExistingServer: true,
      timeout: 180_000,
    },
    {
      command: 'npm run start',
      url: 'http://localhost:4200',
      reuseExistingServer: true,
      timeout: 180_000,
    },
  ],
});
