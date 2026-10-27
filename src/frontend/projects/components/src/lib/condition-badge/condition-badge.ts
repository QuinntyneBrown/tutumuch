import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Badge colour by the nearest step of the guideline condition scale (ADR general/0009): the domain
 * conditions keep their labels; renaming them to the four-step scale needs a domain change.
 */
const STEP: Record<string, 'new' | 'like-new' | 'loved' | 'well-loved'> = {
  'New with tags': 'new',
  'New without tags': 'like-new',
  'Used - Excellent': 'like-new',
  'Used - Good': 'loved',
  'Used - Fair': 'well-loved',
};

/**
 * A listing's condition as a pill (design system: Condition badge): New with tags raspberry, Like
 * new sunny, Gently loved blush, Well loved white with a thin ink ring. Text is bound as text.
 */
@Component({
  selector: 'tl-condition-badge',
  template: `<span class="condition-badge" [class]="'condition-badge--' + step()">{{ condition() }}</span>`,
  styles: ':host { display: inline-flex; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConditionBadgeComponent {
  readonly condition = input.required<string>();

  readonly step = computed(() => STEP[this.condition()] ?? 'well-loved');
}
