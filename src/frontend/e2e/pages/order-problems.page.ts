import { Locator, Page } from '@playwright/test';

/** "Report a problem" (mocks dialogs/report-problem): what's wrong, description, photos, Cancel and Send to TuTuMuch. */
export class ReportProblemDialog {
  readonly root: Locator;
  readonly reasons: Locator;
  readonly description: Locator;
  readonly photoInput: Locator;
  readonly photos: Locator;
  readonly summary: Locator;
  readonly errors: Locator;
  readonly alert: Locator;
  readonly cancel: Locator;
  readonly send: Locator;

  constructor(page: Page) {
    this.root = page.getByRole('dialog', { name: 'Report a problem' });
    this.reasons = this.root.getByRole('radio');
    this.description = this.root.getByLabel('Describe it');
    this.photoInput = this.root.locator('input[type="file"]');
    this.photos = this.root.locator('.uploader__tile');
    this.summary = this.root.locator('.form-summary');
    this.errors = this.root.locator('.field__error');
    this.alert = this.root.getByRole('alert');
    this.cancel = this.root.getByRole('button', { name: 'Cancel' });
    this.send = this.root.locator('.dialog__footer .btn--primary');
  }

  reason(label: string): Locator {
    return this.root.getByRole('radio', { name: label });
  }
}

/** The problem block of the order and sale pages: the warning alert, the thread and the seller's reply form. */
export class ProblemSection {
  readonly alert: Locator;
  readonly thread: Locator;
  readonly entries: Locator;
  readonly reply: Locator;
  readonly sendReply: Locator;
  readonly reportProblem: Locator;

  constructor(page: Page) {
    const main = page.locator('main');
    this.alert = main.locator('.alert');
    this.thread = main.locator('.thread');
    this.entries = this.thread.locator('.thread__item');
    this.reply = main.getByLabel('Your reply');
    this.sendReply = main.getByRole('button', { name: /Send reply|Try again/ });
    this.reportProblem = main.getByRole('button', { name: 'Report a problem' });
  }
}

/** The administrator's problem page at /admin/problems/{reference} and its Resolve dialog (mocks pages/problem-detail, dialogs/resolve-problem). */
export class ProblemDetailPage {
  readonly topBar: Locator;
  readonly heading: Locator;
  readonly subtitle: Locator;
  readonly thread: Locator;
  readonly photos: Locator;
  readonly panel: Locator;
  readonly facts: Locator;
  readonly resolve: Locator;
  readonly alert: Locator;
  readonly footer: Locator;
  readonly loading: Locator;
  readonly error: Locator;
  readonly dialog: Locator;
  readonly refund: Locator;
  readonly release: Locator;
  readonly reason: Locator;
  readonly reasonError: Locator;
  readonly confirm: Locator;
  readonly cancel: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.topBar = page.locator('header.topbar');
    this.heading = main.getByRole('heading', { level: 1 });
    this.subtitle = main.locator('.subtitle');
    this.thread = main.locator('.thread__item');
    this.photos = main.locator('.thread img');
    this.panel = main.getByRole('complementary', { name: 'Order' });
    this.facts = this.panel.locator('.facts');
    this.resolve = this.panel.getByRole('button', { name: 'Resolve' });
    this.alert = main.locator('.alert');
    this.footer = page.locator('footer');
    this.loading = main.locator('[aria-busy="true"]');
    this.error = main.locator('.empty');
    this.dialog = page.getByRole('dialog', { name: /Resolve problem/ });
    this.refund = this.dialog.getByRole('radio', { name: /Refund buyer in full/ });
    this.release = this.dialog.getByRole('radio', { name: /Release payout to seller/ });
    this.reason = this.dialog.getByLabel('Reason');
    this.reasonError = this.dialog.locator('.field__error');
    this.confirm = this.dialog.locator('.dialog__footer .btn--primary');
    this.cancel = this.dialog.getByRole('button', { name: 'Cancel' });
  }

  async goto(reference = 'P-1001'): Promise<void> {
    await this.page.goto(`/admin/problems/${reference}`);
  }
}
