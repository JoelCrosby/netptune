import { Component, effect, inject, signal } from '@angular/core';
import {
  apply,
  disabled,
  email,
  form,
  FormField,
  maxLength,
  required,
  submit,
} from '@angular/forms/signals';
import { Store } from '@ngrx/store';
import { updateProfile } from '@app/core/store/profile/profile.actions';
import {
  selectProfile,
  selectUpdateProfileLoading,
} from '@app/core/store/profile/profile.selectors';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { UpdateProfileImageComponent } from '@profile/components/update-profile-image/update-profile-image.component';
import { DialogService } from '@core/services/dialog.service';
import { SelectProfileImageDialogComponent } from '@profile/components/select-profile-image-dialog/select-profile-image-dialog.component';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

@Component({
  selector: 'app-update-profile',
  imports: [
    FormField,
    FormInputComponent,
    StrokedButtonComponent,
    UpdateProfileImageComponent,
  ],
  template: `
    <form
      class="flex flex-row justify-start gap-24 px-0 max-[1036px]:flex-col-reverse"
      (submit)="updateClicked($event)">
      <div class="w-full max-w-120">
        <app-form-input [formField]="profileForm.firstname" label="Firstname" />
        <app-form-input [formField]="profileForm.lastname" label="Lastname" />
        <app-form-input [formField]="profileForm.email" label="Email Address" />

        <input type="hidden" [formField]="profileForm.pictureUrl" />

        <button
          class="mt-3 ml-auto block"
          app-stroked-button
          type="submit"
          [disabled]="loadingUpdate()">
          Update Profile
        </button>
      </div>

      <app-update-profile-image
        [pictureUrl]="profileForm.pictureUrl().value()"
        (changePictureClicked)="onChangePictureClicked()" />
    </form>
  `,
})
export class UpdateProfileComponent {
  private store = inject(Store);
  private dialog = inject(DialogService);

  profileFormModel = signal({
    firstname: '',
    lastname: '',
    email: '',
    pictureUrl: '',
  });

  profileForm = form(this.profileFormModel, (schema) => {
    apply(
      schema.firstname,
      requiredTextSchema({ label: 'First name', maxLength: 128 })
    );
    apply(
      schema.lastname,
      requiredTextSchema({ label: 'Last name', maxLength: 128 })
    );
    required(schema.email, { message: 'Email is required.' });
    maxLength(schema.email, 128);
    email(schema.email, { message: 'Enter a valid email address.' });
    disabled(schema, () => this.loadingUpdate());
  });

  currentProfile = this.store.selectSignal(selectProfile);
  loadingUpdate = this.store.selectSignal(selectUpdateProfileLoading);

  constructor() {
    effect(() => {
      const profile = this.store.selectSignal(selectProfile);
      const value = profile();

      if (!value) return;

      this.profileFormModel.set({
        firstname: value.firstname,
        lastname: value.lastname,
        email: value.email,
        pictureUrl: value.pictureUrl ?? '',
      });
    });
  }

  updateClicked(event: Event) {
    event.preventDefault();
    const profile = this.currentProfile();

    if (!profile) return;

    submit(this.profileForm, async () => {
      this.store.dispatch(
        updateProfile.init({
          profile: {
            ...profile,
            firstname: this.profileForm.firstname().value().trim(),
            lastname: this.profileForm.lastname().value().trim(),
            email: this.profileForm.email().value().trim(),
          },
        })
      );
    });
  }

  onChangePictureClicked() {
    this.dialog.open(SelectProfileImageDialogComponent, { width: '360px' });
  }
}
