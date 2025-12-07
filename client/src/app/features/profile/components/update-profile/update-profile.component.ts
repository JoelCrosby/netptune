import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  signal,
} from '@angular/core';
import { disabled, email, Field, form, required } from '@angular/forms/signals';
import { MatButton } from '@angular/material/button';
import { Store } from '@ngrx/store';
import { updateProfile } from '@profile/store/profile.actions';
import {
  selectProfile,
  selectUpdateProfileLoading,
} from '@profile/store/profile.selectors';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-update-profile',
  templateUrl: './update-profile.component.html',
  styleUrls: ['./update-profile.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Field, FormInputComponent, MatButton],
})
export class UpdateProfileComponent {
  private store = inject(Store);

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

  editProfilePicture = signal(false);
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
    this.editProfilePicture.set(true);
  }

  onCropped({ blob, src }: { blob: Blob; src: string }) {
    if (!blob) return;

    const data = new FormData();

    data.append('file', blob, 'profile-picture.png');

    this.editProfilePicture.set(false);
    this.profileForm.pictureUrl().value.set(src);

    const profile = this.currentProfile();

    if (!profile) return;

    this.store.dispatch(updateProfile({ profile, image: data }));
  }

  onCropperCanceled() {
    this.editProfilePicture.set(false);
  }

  onCropperCleared() {
    const profile = this.currentProfile();

    if (!profile) return;

    this.store.dispatch(
      updateProfile({
        profile: {
          ...profile,
          pictureUrl: null,
        },
      })
    );

    this.profileForm.pictureUrl().reset();
    this.editProfilePicture.set(false);
  }
}
