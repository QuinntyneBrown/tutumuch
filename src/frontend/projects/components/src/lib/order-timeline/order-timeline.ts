import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** One step of the timeline with its formatted date, or null when it has not been reached. */
export interface TimelineStep {
  label: string;
  date: string | null;
}

/**
 * The steps of an order with the date each was reached (design system: Order timeline, L2-032).
 * The last dated step is current; the dated steps before it are done.
 */
@Component({
  selector: 'tm-order-timeline',
  templateUrl: './order-timeline.html',
  styleUrl: './order-timeline.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderTimelineComponent {
  readonly steps = input.required<TimelineStep[]>();

  readonly current = computed(() => this.steps().reduce((last, step, i) => (step.date ? i : last), -1));
}
