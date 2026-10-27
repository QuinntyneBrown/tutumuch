import { ChangeDetectionStrategy, Component } from '@angular/core';

/** The "or" between a provider button and the email form (design system: Provider button). */
@Component({
  selector: 'tl-or-divider',
  templateUrl: './or-divider.html',
  styleUrl: './or-divider.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrDividerComponent {}
