import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { IconComponent, IconName } from '../icon/icon';
import { Toast, ToastService, ToastTone } from './toast-service';

const ICONS: Record<ToastTone, IconName> = {
  info: 'info',
  success: 'check-circle',
  warning: 'alert',
  danger: 'x-circle',
};

/** Polite live region that renders the toasts of `ToastService`, newest at the bottom (design system: Toast). */
@Component({
  selector: 'tl-toast-region',
  imports: [IconComponent],
  templateUrl: './toast-region.html',
  styleUrl: './toast-region.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastRegionComponent {
  readonly service = inject(ToastService);
  readonly icons = ICONS;

  act(toast: Toast): void {
    this.service.dismiss(toast.id);
    toast.action?.run();
  }
}
