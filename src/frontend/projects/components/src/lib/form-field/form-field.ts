import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Visible label, projected control, help, error and counter (design system: Form field). The
 * control binds describedBy() and invalid() through a template reference so help and error are
 * read with it.
 */
@Component({
  selector: 'tm-form-field',
  templateUrl: './form-field.html',
  styleUrl: './form-field.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormFieldComponent {
  readonly fieldId = input.required<string>();
  readonly label = input.required<string>();
  readonly help = input<string>();
  readonly error = input<string | null>();
  readonly counter = input<string>();
  readonly optional = input(false);

  readonly invalid = computed(() => (this.error() ? 'true' : null));
  readonly describedBy = computed(
    () =>
      [this.help() ? `${this.fieldId()}-help` : '', this.error() ? `${this.fieldId()}-error` : '']
        .filter(Boolean)
        .join(' ') || null,
  );
}
