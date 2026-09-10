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
import { profileResource } from '@core/resources/profile.resource';
import { ProfileCommandsService } from '@core/services/profile-commands.service';
import { LucideUserRound } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { UpdateProfileImageComponent } from '@profile/components/update-profile-image/update-profile-image.component';
import { DialogService } from '@core/services/dialog.service';
import { SelectProfileImageDialogComponent } from '@profile/components/select-profile-image-dialog/select-profile-image-dialog.component';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PanelFooterComponent } from '@static/components/panel-footer.component';

@Component({
  selector: 'app-update-profile',
  imports: [
    FormField,
    FormInputComponent,
    PanelBodyComponent,
    PanelComponent,
    PanelFooterComponent,
    PanelHeaderComponent,
    StrokedButtonComponent,
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

      <app-panel-body
        class="flex flex-row justify-start gap-16 max-[1036px]:flex-col-reverse">
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
      </app-panel-body>

      <app-panel-footer>
        <button app-stroked-button type="submit" [disabled]="loadingUpdate()">
          <span i18n="Button that saves profile changes">Update Profile</span>
        </button>
      </app-panel-footer>
    </form>
  `,
})
export class UpdateProfileComponent {
  protected readonly profileIcon = LucideUserRound;

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

  private readonly profileCommands = inject(ProfileCommandsService);
  private readonly profile = profileResource();

  currentProfile = this.profile.value;
  loadingUpdate = this.profileCommands.isUpdating;

  constructor() {
    effect(() => {
      const value = this.currentProfile();

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
      this.profileCommands.update({
        ...profile,
        firstname: this.profileForm.firstname().value().trim(),
        lastname: this.profileForm.lastname().value().trim(),
        email: this.profileForm.email().value().trim(),
      });
    });
  }

  onChangePictureClicked() {
    this.dialog.open(SelectProfileImageDialogComponent, { width: '360px' });
  }
}
