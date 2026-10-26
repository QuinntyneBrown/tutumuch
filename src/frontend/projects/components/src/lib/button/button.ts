import { Directive, input } from '@angular/core';

/**
 * Styles a native button or link as a TuTuMuch button (design system: Button). Variants primary,
 * secondary (default), ghost, link and danger; sizes sm, md and lg; block fills the row; busy
 * sets aria-busy and keeps the name.
 */
@Directive({
  selector: '[tlButton]',
  host: {
    class: 'btn',
    '[class.btn--primary]': "tlButton() === 'primary'",
    '[class.btn--ghost]': "tlButton() === 'ghost'",
    '[class.btn--link]': "tlButton() === 'link'",
    '[class.btn--danger]': "tlButton() === 'danger'",
    '[class.btn--sm]': "size() === 'sm'",
    '[class.btn--lg]': "size() === 'lg'",
    '[class.btn--block]': 'block()',
    '[attr.aria-busy]': "busy() ? 'true' : null",
  },
})
export class ButtonDirective {
  readonly tlButton = input<'' | 'primary' | 'ghost' | 'link' | 'danger'>('');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly block = input(false);
  readonly busy = input(false);
}
