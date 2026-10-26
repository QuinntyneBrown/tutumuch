import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';

import { IconComponent } from '../icon/icon';

/** info while more than a minute is left, warning in the last minute, danger once it ran out. */
export type ReservationPhase = 'info' | 'warning' | 'danger';

/** Seconds left at which the timer turns to warning. */
const WARNING_SECONDS = 60;

/**
 * "Reserved for you · 9:41 left" counting down to expiresAt (design system: Reservation timer,
 * L2-028). The countdown is not read aloud (aria-live off); reaching the last minute and the end
 * are each announced once through a polite status. Emits the phase whenever it changes.
 */
@Component({
  selector: 'tm-reservation-timer',
  imports: [IconComponent],
  templateUrl: './reservation-timer.html',
  styleUrl: './reservation-timer.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReservationTimerComponent {
  /** ISO time when the reservation ends, from Tutu Much API. */
  readonly expiresAt = input.required<string>();
  readonly phaseChange = output<ReservationPhase>();

  private readonly now = signal(Date.now());

  readonly secondsLeft = computed(() => Math.max(0, Math.ceil((Date.parse(this.expiresAt()) - this.now()) / 1000)));
  readonly phase = computed<ReservationPhase>(() => {
    const left = this.secondsLeft();
    return left === 0 ? 'danger' : left <= WARNING_SECONDS ? 'warning' : 'info';
  });
  readonly text = computed(() => {
    const left = this.secondsLeft();
    return `${Math.floor(left / 60)}:${String(left % 60).padStart(2, '0')}`;
  });
  readonly announcement = computed(() =>
    this.phase() === 'warning'
      ? 'Less than a minute left to pay. After that, the item goes back on sale.'
      : this.phase() === 'danger'
        ? 'Your reservation ended. You weren’t charged.'
        : '',
  );

  constructor() {
    const tick = setInterval(() => this.now.set(Date.now()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(tick));
    effect(() => this.phaseChange.emit(this.phase()));
  }
}
