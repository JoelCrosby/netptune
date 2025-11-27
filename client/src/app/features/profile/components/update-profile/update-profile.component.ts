import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  signal,
} from '@angular/core';
import {
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { Store } from '@ngrx/store';
import {
  updateProfile,
  uploadProfilePicture,
} from '@profile/store/profile.actions';
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
  imports: [FormsModule, ReactiveFormsModule, FormInputComponent, MatButton],
})
export class UpdateProfileComponent {
  private store = inject(Store);
  private fb = inject(FormBuilder);

  formGroup = this.fb.nonNullable.group({
    firstname: ['', [Validators.required]],
    lastname: ['', [Validators.required]],
    email: ['', [Validators.required]],
    pictureUrl: ['' as string | null],
  });

  editProfilePicture = signal(false);
  currentProfile = this.store.selectSignal(selectProfile);
  loadingUpdate = this.store.selectSignal(selectUpdateProfileLoading);

  constructor() {
    effect(() => {
      this.loadingUpdate() ? this.formGroup.disable() : this.formGroup.enable();
    });

    effect(() => {
      const profile = this.store.selectSignal(selectProfile);
      const value = profile();

      if (!value) return;

      this.formGroup.setValue(
        {
          firstname: value.firstname,
          lastname: value.lastname,
          email: value.email,
          pictureUrl: value.pictureUrl ?? null,
        },
        { emitEvent: false }
      );
    });
  }

  updateClicked() {
    const profile = this.currentProfile();

    if (!profile) return;

    this.store.dispatch(
      updateProfile({
        profile: {
          ...profile,
          firstname: this.formGroup.controls.firstname.value,
          lastname: this.formGroup.controls.lastname.value,
          email: this.formGroup.controls.email.value,
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
    this.formGroup.controls.pictureUrl.setValue(src);

    const profile = this.currentProfile();

    if (!profile) return;

    this.store.dispatch(updateProfile({ profile }));
    this.store.dispatch(uploadProfilePicture({ data }));
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

    this.formGroup.controls.pictureUrl.reset();
    this.editProfilePicture.set(false);
  }
}
