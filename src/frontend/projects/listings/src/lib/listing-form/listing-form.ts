import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ControlContainer, ReactiveFormsModule } from '@angular/forms';
import {
  ChoiceComponent,
  EarningsComponent,
  FixedSizeComponent,
  FormFieldComponent,
  InputGroupComponent,
  PhotoUploaderComponent,
  SegmentedComponent,
} from '@tutumuch/components';

import { ListingFormModel } from './listing-form-model';

/**
 * The fields of a listing (mocks pages/sell and pages/edit-listing): photos, item, price and the
 * size confirmation. It renders inside the page's form, whose group is the model's form, so the
 * page owns the error summary, the alerts and the actions.
 */
@Component({
  selector: 'tm-listing-form',
  imports: [
    ReactiveFormsModule,
    ChoiceComponent,
    EarningsComponent,
    FixedSizeComponent,
    FormFieldComponent,
    InputGroupComponent,
    PhotoUploaderComponent,
    SegmentedComponent,
  ],
  templateUrl: './listing-form.html',
  styleUrl: './listing-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  viewProviders: [{ provide: ControlContainer, useFactory: () => inject(ControlContainer, { skipSelf: true }) }],
})
export class ListingFormComponent {
  readonly model = input.required<ListingFormModel>();
}
