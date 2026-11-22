import { expect, test, type Page } from '@playwright/test';

test.skip(process.env.PLAYWRIGHT_GUIDED_E2E !== '1', 'Run against a dedicated disposable API database.');

async function mutate(page: Page, method: string, path: string, data: unknown) {
  const csrf = (await page.context().cookies()).find(cookie => cookie.name === 'll_csrf')?.value || '';
  const response = await page.request.fetch(path, {
    method, data, headers: { 'X-CSRF-Token': csrf }
  });
  expect(response.ok(), `${method} ${path}: ${await response.text()}`).toBeTruthy();
  return response;
}

test('guided lesson saves progress and opens the next lesson', async ({ page }) => {
  const login = await page.request.post('/api/v1/auth/login', {
    data: { email: 'admin@languagelearning.app', password: 'Admin@123456' }
  });
  expect(login.ok()).toBeTruthy();
  const languages = await (await page.request.get('/api/v1/languages')).json() as { id: string; code: string }[];
  const english = languages.find(item => item.code === 'en') ?? await (await mutate(page, 'POST', '/api/v1/admin/languages', {
    code: 'en', name: 'English', nativeName: 'English', isActive: true
  })).json() as { id: string; code: string };
  const course = await (await mutate(page, 'POST', '/api/v1/admin/courses', {
    languageId: english!.id, title: `Guided E2E ${Date.now()}`, level: 'A1', domain: 'general'
  })).json() as { id: string };
  const unit = await (await mutate(page, 'POST', `/api/v1/admin/courses/${course.id}/units`, {
    title: 'First unit', sortOrder: 1
  })).json() as { id: string };
  const lessonIds: string[] = [];
  for (const order of [1, 2]) {
    const lesson = await (await mutate(page, 'POST', `/api/v1/admin/courses/units/${unit.id}/lessons`, {
      title: `Lesson ${order}`, lessonType: 'Vocabulary', sortOrder: order
    })).json() as { id: string };
    lessonIds.push(lesson.id);
  }
  const draft = await (await page.request.get(`/api/v1/courses/${course.id}/material`)).json() as {
    units: { lessons: Record<string, unknown>[] }[]
  };
  for (const [index, lesson] of draft.units[0].lessons.entries()) {
    lesson.summary = `Objective for lesson ${index + 1}`;
    lesson.lessonSteps = [
      { id: 'intro', type: 'introduction', title: 'Introduction', order: 0, required: true, completionPolicy: { kind: 'viewed' } },
      { id: 'recap', type: 'recap', title: 'Recap', order: 1, required: true, completionPolicy: { kind: 'viewed' } }
    ];
    await mutate(page, 'PUT', `/api/v1/admin/courses/${course.id}/lessons/${lessonIds[index]}/material`, lesson);
  }
  await mutate(page, 'PUT', '/api/v1/me/study-target', {
    languageId: english!.id, languageCode: 'en', level: 'A1', domain: 'general', domains: ['general']
  });

  await page.goto(`/learn/${course.id}/${lessonIds[0]}`);
  await expect(page.getByText('Objective for lesson 1').first()).toBeVisible();
  await page.getByRole('button', { name: /Got it.*Continue|Đã hiểu mục tiêu/ }).click();
  await page.getByRole('button', { name: /Finish recap|Hoàn tất tổng kết/ }).click();
  await expect(page.getByText(/Lesson complete|Đã hoàn thành bài học/)).toBeVisible();
  await page.goto(`/learn/${course.id}/${lessonIds[1]}`);
  await expect(page.getByText('Objective for lesson 2').first()).toBeVisible();
});
