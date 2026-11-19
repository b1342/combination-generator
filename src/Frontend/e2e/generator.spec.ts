import { Locator, Page, expect, test } from '@playwright/test';

/**
 * בדיקות קצה-לקצה: דפדפן אמיתי מול המסך האמיתי והשרת האמיתי, בדיוק כמו משתמשת.
 * כל בדיקה מקבלת לשונית חדשה, ולכן גם Session חדש.
 */

// ---------- עזרים ----------

function button(page: Page, name: string): Locator {
  return page.getByRole('button', { name, exact: true });
}

function currentLine(page: Page): Locator {
  return page.locator('.current-line');
}

function pageStatus(page: Page): Locator {
  return page.locator('.page-status');
}

function rows(page: Page): Locator {
  return page.locator('tbody tr');
}

async function start(page: Page, n: number): Promise<void> {
  await page.goto('/');
  await page.getByLabel('מספר האיברים (1–20)').fill(String(n));
  await button(page, 'התחל').click();
  await expect(page.locator('.stats')).toBeVisible();
}

async function clickNext(page: Page, times: number): Promise<void> {
  for (let i = 1; i <= times; i++) {
    await button(page, 'הבא').click();
    await expect(currentLine(page)).toContainText(`קומבינציה מספר ${i} –`);
  }
}

// ---------- מסך ההתחלה ----------

test('n out of range is rejected on the screen, without starting a session', async ({ page }) => {
  await page.goto('/');

  for (const value of ['0', '21']) {
    await page.getByLabel('מספר האיברים (1–20)').fill(value);
    await button(page, 'התחל').click();
    await expect(page.getByText('יש להזין מספר שלם בין 1 ל-20.')).toBeVisible();
  }

  await expect(page.locator('.stats')).toHaveCount(0);
});

test('start shows n and the number of possibilities', async ({ page }) => {
  await start(page, 3);

  await expect(page.locator('.stats')).toContainText('מספר איברים');
  await expect(page.locator('.stats')).toContainText('סה״כ האפשרויות לסדר 3 מספרים בשורה הוא:');
  await expect(page.locator('.stat-value').nth(1)).toHaveText('6');
});

// ---------- הבא והקודם ----------

test('"next" walks the assignment sequence, "previous" goes back', async ({ page }) => {
  await start(page, 3);
  await expect(page.getByText('לחצו על "הבא" להצגת הקומבינציה הראשונה.')).toBeVisible();
  await expect(button(page, 'הקודם')).toBeDisabled();

  const expected = ['[1,2,3]', '[1,3,2]', '[2,1,3]', '[2,3,1]', '[3,1,2]', '[3,2,1]'];
  for (let i = 0; i < expected.length; i++) {
    await button(page, 'הבא').click();
    await expect(currentLine(page)).toHaveText(`קומבינציה מספר ${i + 1} – ${expected[i]}`);
  }

  await button(page, 'הקודם').click();
  await expect(currentLine(page)).toHaveText('קומבינציה מספר 5 – [3,1,2]');
});

test('after the last combination: "no more combinations"', async ({ page }) => {
  await start(page, 1);

  await button(page, 'הבא').click();
  await expect(currentLine(page)).toHaveText('קומבינציה מספר 1 – [1]');

  await button(page, 'הבא').click();
  await expect(page.locator('.alert-info')).toHaveText('אין יותר קומבינציות');
  await expect(currentLine(page)).toHaveText('קומבינציה מספר 1 – [1]');
});

// ---------- כל הקומבינציות ----------

test('"all combinations" starts after the last one shown; "back" shows the last row of the page', async ({ page }) => {
  await start(page, 3);
  await clickNext(page, 2);

  await button(page, 'כל הקומבינציות').click();
  await expect(rows(page)).toHaveCount(4); // 3..6
  await expect(rows(page).first()).toContainText('3');
  await expect(rows(page).first()).toContainText('[2, 1, 3]');

  await button(page, 'חזור').click();
  await expect(currentLine(page)).toHaveText('קומבינציה מספר 6 – [3,2,1]');

  await button(page, 'הבא').click();
  await expect(page.locator('.alert-info')).toHaveText('אין יותר קומבינציות');
});

test('paging: page size, next, last, first, jump, page link and back', async ({ page }) => {
  await start(page, 4); // 24 קומבינציות
  await button(page, 'כל הקומבינציות').click();

  await page.getByLabel('שורות בעמוד').selectOption('5');
  await expect(pageStatus(page)).toHaveText('דף 1 מתוך 5');
  await expect(rows(page)).toHaveCount(5);
  await expect(button(page, 'ראשון')).toBeDisabled();
  await expect(button(page, 'קודם')).toBeDisabled();

  await button(page, 'הבא').click();
  await expect(pageStatus(page)).toHaveText('דף 2 מתוך 5');
  await expect(rows(page).first()).toContainText('[1, 4, 3, 2]'); // מספר 6

  await button(page, 'אחרון').click();
  await expect(pageStatus(page)).toHaveText('דף 5 מתוך 5');
  await expect(rows(page)).toHaveCount(4); // עמוד אחרון חלקי: 21..24
  await expect(rows(page).last()).toContainText('[4, 3, 2, 1]');
  await expect(button(page, 'אחרון')).toBeDisabled();

  await button(page, 'ראשון').click();
  await expect(pageStatus(page)).toHaveText('דף 1 מתוך 5');

  await page.getByLabel('מעבר לדף').fill('3');
  await button(page, 'עבור').click();
  await expect(pageStatus(page)).toHaveText('דף 3 מתוך 5');
  await expect(rows(page).first()).toContainText('[2, 4, 1, 3]'); // מספר 11

  await button(page, '4').click();
  await expect(pageStatus(page)).toHaveText('דף 4 מתוך 5');

  await button(page, 'חזור').click();
  await expect(currentLine(page)).toHaveText('קומבינציה מספר 20 – [4,1,3,2]');
});

test('jump to a page that does not exist shows the server message and keeps the page', async ({ page }) => {
  await start(page, 3);
  await button(page, 'כל הקומבינציות').click();
  await expect(pageStatus(page)).toHaveText('דף 1 מתוך 1');

  await page.getByLabel('מעבר לדף').fill('9');
  await button(page, 'עבור').click();

  await expect(page.getByRole('alert')).toHaveText('מספר הדף לא קיים.');
  await expect(pageStatus(page)).toHaveText('דף 1 מתוך 1');
});

// ---------- מספרים גדולים ----------

test('n = 20: exact numbers far beyond 2^53, last page computed directly', async ({ page }) => {
  await start(page, 20);
  await expect(page.locator('.stat-value').nth(1)).toHaveText('2,432,902,008,176,640,000');

  await button(page, 'כל הקומבינציות').click();
  await button(page, 'אחרון').click();

  await expect(pageStatus(page)).toHaveText('דף 121,645,100,408,832,000 מתוך 121,645,100,408,832,000');
  await expect(rows(page).last()).toContainText('2,432,902,008,176,640,000');
  await expect(rows(page).last()).toContainText('[20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1]');
});

// ---------- רענון, איפוס ולשוניות ----------

test('a page refresh restores the same state from the server', async ({ page }) => {
  await start(page, 3);
  await clickNext(page, 2);

  await page.reload();
  await expect(currentLine(page)).toHaveText('קומבינציה מספר 2 – [1,3,2]');

  await button(page, 'כל הקומבינציות').click();
  await expect(pageStatus(page)).toHaveText('דף 1 מתוך 1');
  await page.reload();
  await expect(pageStatus(page)).toHaveText('דף 1 מתוך 1');
  await expect(rows(page).first()).toContainText('[2, 1, 3]');
});

test('"reset" returns to the start screen, also after a refresh', async ({ page }) => {
  await start(page, 3);

  await button(page, 'איפוס').click();
  await expect(page.getByLabel('מספר האיברים (1–20)')).toBeVisible();
  await expect(page.locator('.stats')).toHaveCount(0);

  await page.reload();
  await expect(page.getByLabel('מספר האיברים (1–20)')).toBeVisible();
});

test('two tabs are two independent sessions', async ({ context }) => {
  const first = await context.newPage();
  const second = await context.newPage();

  await start(first, 3);
  await start(second, 4);
  await clickNext(first, 1);
  await clickNext(second, 3);

  await first.reload();
  await second.reload();
  await expect(currentLine(first)).toHaveText('קומבינציה מספר 1 – [1,2,3]');
  await expect(currentLine(second)).toHaveText('קומבינציה מספר 3 – [1,3,2,4]');
  await expect(first.locator('.stat-value').nth(1)).toHaveText('6');
  await expect(second.locator('.stat-value').nth(1)).toHaveText('24');
});
