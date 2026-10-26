import { ChangeDetectionStrategy, Component, computed, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * A radio group drawn as joined segments, inside a fieldset with a visible legend (design
 * system: Segmented control). Works as a form control; the value is the chosen option.
 */
@Component({
  selector: 'tl-segmented',
  templateUrl: './segmented.html',
  styleUrl: './segmented.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => SegmentedComponent), multi: true }],
})
export class SegmentedComponent implements ControlValueAccessor {
  readonly legend = input.required<string>();
  /** Base id; option n gets id `${groupId}-n` and the error `${groupId}-error`. */
  readonly groupId = input.required<string>();
  readonly options = input.required<readonly string[]>();
  readonly error = input<string | null>();

  readonly value = signal<string | null>(null);
  readonly disabled = signal(false);
  readonly describedBy = computed(() => (this.error() ? `${this.groupId()}-error` : null));

  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  select(option: string): void {
    this.value.set(option);
    this.onChange(option);
    this.onTouched();
  }

  writeValue(value: string | null): void {
    this.value.set(value || null);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.disabled.set(disabled);
  }
}
