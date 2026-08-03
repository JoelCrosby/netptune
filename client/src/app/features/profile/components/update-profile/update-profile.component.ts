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
import { LucideUserRound } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
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
    IconTileComponent,
    StrokedButtonComponent,
    UpdateProfileImageComponent,
  ],
  template: `
    <form
      class="border-border bg-card overflow-hidden rounded-lg border shadow-sm"
      (submit)="updateClicked($event)">
      <header class="border-border border-b px-6 py-5">
        <div class="flex min-w-0 items-center gap-3">
          <app-icon-tile [icon]="profileIcon" />

          <div class="min-w-0">
            <h2
              class="font-overpass text-base font-semibold"
              i18n="Heading of the profile details card">
              Profile details
            </h2>
            <p
              class="text-muted mt-1 text-sm"
              i18n="Explains what the profile details card controls">
              Your name, email address and picture.
            </p>
          </div>
        </div>
      </header>

      <div
        class="flex flex-row justify-start gap-16 px-6 py-5 max-[1036px]:flex-col-reverse">
        <div class="w-full max-w-120">
          <app-form-input
            [formField]="profileForm.firstname"
            i18n-label="Label of the given-name field"
            label="Firstname" />
          <app-form-input
            [formField]="profileForm.lastname"
            i18n-label="Label of the family-name field"
            label="Lastname" />
          <app-form-input
            [formField]="profileForm.email"
            i18n-label="Label of the e-mail address field"
            label="Email Address" />

          <input type="hidden" [formField]="profileForm.pictureUrl" />
        </div>

        <app-update-profile-image
          [pictureUrl]="profileForm.pictureUrl().value()"
          (changePictureClicked)="onChangePictureClicked()" />
      </div>

      <footer class="border-border border-t px-6 py-4">
        <button app-stroked-button type="submit" [disabled]="loadingUpdate()">
          <span i18n="Button that saves profile changes">Update Profile</span>
        </button>
      </footer>
    </form>
  `,
})
export class UpdateProfileComponent {
  protected readonly profileIcon = LucideUserRound;

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
      requiredTextSchema({
        label: $localize`:Label shown in the interface:First name`,
        maxLength: 128,
      })
    );
    apply(
      schema.lastname,
      requiredTextSchema({
        label: $localize`:Label shown in the interface:Last name`,
        maxLength: 128,
      })
    );
    required(schema.email, {
      message: $localize`:Body of a dialog or validation message:Email is required.`,
    });
    maxLength(schema.email, 128);
    email(schema.email, {
      message: $localize`:Body of a dialog or validation message:Enter a valid email address.`,
    });
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
