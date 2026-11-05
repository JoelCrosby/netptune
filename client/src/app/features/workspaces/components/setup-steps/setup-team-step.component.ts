import {
  Component,
  computed,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import {
  email,
  form,
  FormField,
  maxLength,
  pattern,
  submit,
} from '@angular/forms/signals';
import { UsersService } from '@core/services/users.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { mutation } from '@core/util/mutation';
import { requireSuccess } from '@core/util/rxjs-operators';
import { LucideUsers } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import { CheckLabelComponent } from '@static/components/check-label/check-label.component';
import { FormErrorComponent } from '@static/components/form-error/form-error.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { MediaRowComponent } from '@static/components/media-row.component';
import { ValueChipComponent } from '@static/components/value-chip/value-chip.component';

// The email validator accepts an address with no dot in its domain, which can never be delivered.
const addressWithDottedDomain = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

const invalidEmail = $localize`:Body of a dialog or validation message:Enter a valid email address.`;

@Component({
  selector: 'app-setup-team-step',
  imports: [
    CheckLabelComponent,
    FlatButtonComponent,
    FormErrorComponent,
    FormField,
    FormInputComponent,
    IconTileComponent,
    InlineButtonComponent,
    MediaRowComponent,
    ValueChipComponent,
  ],
  host: { class: 'flex flex-col gap-4' },
  template: `
    @if (invitedCount() > 0) {
      <app-media-row
        i18n-heading="Heading of the invitations row in workspace setup"
        heading="Invitations">
        <app-icon-tile
          mediaRowMedia
          size="medium"
          tone="purple"
          [icon]="teamIcon" />

        <span
          i18n="How many people have been invited to the workspace"
          >{invitedCount(), plural,
            =1 {1 invitation sent.}
            other {{{invitedCount()}} invitations sent.}
          }</span
        >

        <app-check-label
          mediaRowActions
          i18n="Shown once workspace invitations have gone out"
          >Sent</app-check-label
        >
      </app-media-row>
    }

    <form novalidate (submit)="$event.preventDefault(); add()">
      <app-form-input
        [noMargin]="true"
        [formField]="inviteForm.email"
        type="email"
        i18n-label="Label of the field for an invitee's e-mail address"
        label="Invitee Email"
        maxLength="128"
        i18n-placeholder="Placeholder of the invitee e-mail address field"
        placeholder="name@company.com">
        <button
          app-inline-button
          formInputAction
          type="button"
          [disabled]="!hasAddress()"
          (click)="add()">
          <span i18n="Button that adds an e-mail address to the invitation list"
            >Add</span
          >
        </button>
      </app-form-input>
    </form>

    @if (invitees().length) {
      <ul class="m-0 flex list-none flex-wrap gap-1.5 p-0">
        @for (invitee of invitees(); track invitee) {
          <li>
            <app-value-chip
              [label]="invitee"
              i18n-removeLabel="
                Button that removes an address from the invitation list
              "
              removeLabel="Remove"
              (removed)="remove(invitee)" />
          </li>
        }
      </ul>

      <button
        app-flat-button
        class="self-end max-md:w-full"
        type="button"
        [disabled]="request.pending()"
        (click)="send()">
        <span i18n="Button that sends the workspace invitations"
          >Send Invites</span
        >
      </button>
    }

    @if (request.error(); as message) {
      <app-form-error role="alert">{{ message }}</app-form-error>
    }
  `,
})
export class SetupTeamStepComponent {
  private readonly users = inject(UsersService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  readonly invitedCount = input.required<number>();
  readonly invitees = model.required<string[]>();

  readonly invited = output();

  protected readonly teamIcon = LucideUsers;

  private readonly inviteModel = signal({ email: '' });

  protected readonly inviteForm = form(this.inviteModel, (schema) => {
    email(schema.email, { message: invalidEmail });
    pattern(schema.email, addressWithDottedDomain, { message: invalidEmail });
    maxLength(schema.email, 128);
  });

  protected readonly request = mutation({
    fallbackError: $localize`:Error shown after an action fails:The invite(s) could not be sent.`,
  });

  protected readonly hasAddress = computed(() => {
    return !!this.inviteModel().email.trim();
  });

  protected add() {
    if (!this.hasAddress()) return;

    void submit(this.inviteForm, async () => {
      const address = this.inviteModel().email.trim().toLowerCase();

      this.invitees.update((items) => {
        return items.includes(address) ? items : [...items, address];
      });

      this.inviteForm().reset({ email: '' });
    });
  }

  protected remove(address: string) {
    this.invitees.update((items) => items.filter((item) => item !== address));
  }

  protected send() {
    this.request.run(
      this.users.inviteUsersToWorkspace(this.invitees()).pipe(requireSuccess()),
      {
        onSuccess: () => {
          this.invitees.set([]);
          this.workspaceRefresh.refresh(['users']);
          this.invited.emit();
        },
      }
    );
  }
}
