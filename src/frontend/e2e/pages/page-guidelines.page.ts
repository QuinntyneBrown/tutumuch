import { Page } from '@playwright/test';

/** What the page-guideline checks of L2-096 found on one rendered page. */
export interface GuidelineReport {
  h1Count: number;
  visibleLogoLinks: number;
  unlabelledIconControls: string[];
  imagesWithoutAlt: string[];
  tillyAlts: string[];
  inlineHexOrGradient: string[];
  visiblePrimaryButtons: number;
  headingFont: string;
  bodyFont: string;
  titanOneOutsideLogo: string[];
}

/**
 * Reads a rendered page against the checkable rules of docs/page-guidelines.md (L2-096): one h1,
 * one logo, labelled icon-only controls, alt text, at most one Tilly, tokens instead of inline hex
 * or gradients, one primary button per view, Fraunces headings, DM Sans text, Titan One only in
 * the logo and mark.
 */
export class PageGuidelines {
  constructor(private readonly page: Page) {}

  async report(): Promise<GuidelineReport> {
    return this.page.evaluate(() => {
      const visible = (el: Element) => {
        const box = (el as HTMLElement).getBoundingClientRect();
        const style = getComputedStyle(el);
        return box.width > 0 && box.height > 0 && style.visibility !== 'hidden';
      };
      const describe = (el: Element) => `${el.tagName.toLowerCase()}.${(el as HTMLElement).className}`.slice(0, 80);
      const inDialog = (el: Element) => !!el.closest('[role="dialog"], [role="alertdialog"]');
      const controls = [...document.querySelectorAll('button, a[href]')];
      const unlabelled = controls.filter(
        (el) =>
          !(el.textContent ?? '').trim() &&
          !el.getAttribute('aria-label') &&
          !el.getAttribute('aria-labelledby') &&
          !el.querySelector('img[alt]:not([alt=""])'),
      );
      const images = [...document.querySelectorAll('img')];
      const styled = [...document.querySelectorAll('[style]')].filter((el) =>
        /#[0-9a-f]{3,8}\b|gradient\(/i.test(el.getAttribute('style') ?? ''),
      );
      const logoFont = (el: Element) => !!el.closest('tl-logo, tl-mark, .logo, .mark');
      const titan = [...document.querySelectorAll('body *')].filter(
        (el) =>
          !logoFont(el) &&
          el.childNodes.length > 0 &&
          [...el.childNodes].some((n) => n.nodeType === Node.TEXT_NODE && (n.textContent ?? '').trim()) &&
          getComputedStyle(el).fontFamily.includes('Titan One'),
      );
      const h1 = document.querySelector('h1');
      return {
        h1Count: document.querySelectorAll('h1').length,
        visibleLogoLinks: [...document.querySelectorAll('a[aria-label="TuTuMuch home"]')].filter(visible).length,
        unlabelledIconControls: unlabelled.map(describe),
        imagesWithoutAlt: images.filter((img) => !img.hasAttribute('alt')).map((img) => img.src),
        tillyAlts: images.filter((img) => img.src.includes('/tilly/')).map((img) => img.alt),
        inlineHexOrGradient: styled.map(describe),
        visiblePrimaryButtons: [...document.querySelectorAll('.btn--primary')].filter(
          (el) => visible(el) && !inDialog(el),
        ).length,
        headingFont: h1 ? getComputedStyle(h1).fontFamily : '',
        bodyFont: getComputedStyle(document.body).fontFamily,
        titanOneOutsideLogo: titan.map(describe),
      };
    });
  }
}
