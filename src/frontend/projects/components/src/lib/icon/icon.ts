import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Names of the inline SVG icons (design system: Iconography). */
export type IconName =
  | 'menu'
  | 'close'
  | 'plus'
  | 'check'
  | 'check-circle'
  | 'info'
  | 'alert'
  | 'x-circle'
  | 'lock'
  | 'mail'
  | 'refresh'
  | 'clock'
  | 'camera'
  | 'tag'
  | 'wifi-off'
  | 'edit'
  | 'star'
  | 'truck'
  | 'external'
  | 'search'
  | 'flag'
  | 'eye-off'
  | 'bag'
  | 'image'
  | 'trash'
  | 'map-pin'
  | 'play'
  | 'pause'
  | 'captions'
  | 'bell';

/** One decorative inline SVG icon from the TuTuMuch set, hidden from assistive technology (design system: Icon). */
@Component({
  selector: 'tl-icon',
  templateUrl: './icon.html',
  styleUrl: './icon.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IconComponent {
  readonly name = input.required<IconName>();
  readonly size = input<'sm' | 'md' | 'lg'>('md');
}
