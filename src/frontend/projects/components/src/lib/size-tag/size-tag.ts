import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** The Tutu Much size mark, "Girls", with the last word emphasised (design system: Size tag). */
@Component({
  selector: 'tm-size-tag',
  templateUrl: './size-tag.html',
  styleUrl: './size-tag.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SizeTagComponent {
  /** Short size label, for example "Girls". */
  readonly label = input.required<string>();
  readonly large = input(false);

  readonly prefix = computed(() => this.label().split(' ').slice(0, -1).join(' '));
  readonly value = computed(() => this.label().split(' ').at(-1) ?? '');
}
