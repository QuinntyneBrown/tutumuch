import { Injectable, signal } from '@angular/core';

export type ToastTone = 'info' | 'success' | 'warning' | 'danger';

/** What a toast says; one optional action such as "Try again" runs and dismisses the toast. */
export interface ToastOptions {
  tone: ToastTone;
  title: string;
  text?: string;
  action?: { label: string; run: () => void };
}

export interface Toast extends ToastOptions {
  id: number;
}

/** How long a toast other than danger stays, paused while hovered or focused (design system: Toast). */
export const TOAST_DURATION_MS = 8000;

/**
 * Shows short, transient messages in the shell's `tl-toast-region` (design system: Toast). Focus
 * never moves to a toast. Danger toasts carry role="alert" and stay until dismissed; the others
 * leave after 8 seconds.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();
  readonly toasts = signal<readonly Toast[]>([]);

  show(options: ToastOptions): number {
    const toast: Toast = { ...options, id: this.nextId++ };
    this.toasts.update((toasts) => [...toasts, toast]);
    this.resume(toast.id);
    return toast.id;
  }

  dismiss(id: number): void {
    this.pause(id);
    this.toasts.update((toasts) => toasts.filter((toast) => toast.id !== id));
  }

  /** Stops the timer while the pointer or focus is on the toast. */
  pause(id: number): void {
    clearTimeout(this.timers.get(id));
    this.timers.delete(id);
  }

  /** Restarts the timer of a toast that times out. */
  resume(id: number): void {
    const toast = this.toasts().find((candidate) => candidate.id === id);
    if (!toast || toast.tone === 'danger' || this.timers.has(id)) return;
    this.timers.set(
      id,
      setTimeout(() => this.dismiss(id), TOAST_DURATION_MS),
    );
  }
}
