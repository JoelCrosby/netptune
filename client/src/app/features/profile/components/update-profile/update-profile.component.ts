import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  signal,
} from '@angular/core';
import {
  disabled,
  email,
  form,
  FormField,
  required,
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

@Component({
  selector: 'app-update-profile',
  templateUrl: './update-profile.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    FormInputComponent,
    StrokedButtonComponent,
    UpdateProfileImageComponent,
  ],
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
    required(schema.firstname);
    required(schema.lastname);
    required(schema.email);
    email(schema.email);
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

  updateClicked() {
    const profile = this.currentProfile();

    if (!profile) return;

    this.store.dispatch(
      updateProfile({
        profile: {
          ...profile,
          firstname: this.profileForm.firstname().value(),
          lastname: this.profileForm.lastname().value(),
          email: this.profileForm.email().value(),
        },
      })
    );
  }

  onChangePictureClicked() {
    this.dialog.open(SelectProfileImageDialogComponent, { width: '360px' });
  }
}
