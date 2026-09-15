import { Component, effect, inject, signal, untracked } from '@angular/core';
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
import { profileResource } from '@core/resources/profile.resource';
import { ProfileCommandsService } from '@core/services/profile-commands.service';
import { LucideUserRound } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { UpdateProfileImageComponent } from '@profile/components/update-profile-image/update-profile-image.component';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PanelFooterComponent } from '@static/components/panel-footer.component';

interface ProfileFields {
  firstname: string;
  lastname: string;
  email: string;
}

@Component({
  selector: 'app-update-profile',
  imports: [
    FlatButtonComponent,
    FormField,
    FormInputComponent,
    PanelBodyComponent,
    PanelComponent,
    PanelFooterComponent,
    PanelHeaderComponent,
    UpdateProfileImageComponent,
  ],
  template: `
    <form app-panel surface="card" (submit)="updateClicked($event)">
      <app-panel-header
        density="comfortable"
        [icon]="profileIcon"
        i18n-heading="Heading of the profile details card"
        heading="Profile details"
        i18n-description="Explains what the profile details card controls"
        description="Your name, email address and picture." />

      <app-panel-body>
        <app-update-profile-image
          class="mb-6"
          [pictureUrl]="currentProfile()?.pictureUrl"
          [name]="currentProfile()?.displayName"
          [uploading]="updatingPicture()"
          (fileSelected)="onPictureSelected($event)"
          (gravatarClicked)="onGravatarClicked()"
          (removeClicked)="onRemovePictureClicked()" />

        <div class="grid grid-cols-1 gap-x-4 md:grid-cols-2">
          <app-form-input
            [formField]="profileForm.firstname"
            i18n-label="Label of the given-name field"
            label="Firstname" />
          <app-form-input
            [formField]="profileForm.lastname"
            i18n-label="Label of the family-name field"
            label="Lastname" />
        </div>

        <div class="max-w-120">
          <app-form-input
            [formField]="profileForm.email"
            i18n-label="Label of the e-mail address field"
            label="Email Address" />
        </div>
      </app-panel-body>

      <app-panel-footer class="flex flex-wrap items-center gap-3">
        <button app-flat-button type="submit" [disabled]="loadingUpdate()">
          <span i18n="Button that saves profile changes">Update Profile</span>
        </button>
        <span
          class="text-muted text-xs"
          i18n="
            Explains that profile picture changes do not need the save button
          ">
          Picture changes save as soon as you pick one.
        </span>
      </app-panel-footer>
    </form>
  `,
})
export class UpdateProfileComponent {
  protected readonly profileIcon = LucideUserRound;

  profileFormModel = signal<ProfileFields>({
    firstname: '',
    lastname: '',
    email: '',
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

  private readonly profileCommands = inject(ProfileCommandsService);
  private readonly profile = profileResource();

  currentProfile = this.profile.value;
  loadingUpdate = this.profileCommands.isUpdating;
  updatingPicture = this.profileCommands.isUpdatingPicture;

  constructor() {
    effect(() => {
      const value = this.currentProfile();

      if (!value) return;

      const next = {
        firstname: value.firstname,
        lastname: value.lastname,
        email: value.email,
      };

      // A picture change reloads the profile too, which must not wipe unsaved edits.
      const keepEdits = untracked(() => {
        return this.profileForm().dirty() && !this.matchesForm(next);
      });

      if (keepEdits) return;

      this.profileForm().reset(next);
    });
  }

  updateClicked(event: Event) {
    event.preventDefault();
    const profile = this.currentProfile();

    if (!profile) return;

    submit(this.profileForm, async () => {
      this.profileCommands.update({
        ...profile,
        firstname: this.profileForm.firstname().value().trim(),
        lastname: this.profileForm.lastname().value().trim(),
        email: this.profileForm.email().value().trim(),
      });
    });
  }

  onPictureSelected(file: File) {
    this.profileCommands.uploadPicture(file);
  }

  onGravatarClicked() {
    const profile = this.currentProfile();

    if (!profile) return;

    this.profileCommands.useGravatar(profile.id, profile.email);
  }

  onRemovePictureClicked() {
    const profile = this.currentProfile();

    if (!profile) return;

    this.profileCommands.removePicture(profile.id);
  }

  private matchesForm(values: ProfileFields) {
    const current = this.profileFormModel();

    return (
      current.firstname === values.firstname &&
      current.lastname === values.lastname &&
      current.email === values.email
    );
  }
}
