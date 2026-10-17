import { Component, computed, inject, signal } from '@angular/core';
import {
  apply,
  FormField,
  form,
  submit as submitForm,
} from '@angular/forms/signals';
import {
  SourceControlIdentity,
  SourceControlProvider,
  sourceControlProviderNames,
  sourceControlProviderSlug,
} from '@core/models/source-control';
import {
  sourceControlIdentitiesResource,
  sourceControlProvidersResource,
} from '@core/resources/source-control.resource';
import { getErrorMessage } from '@core/util/error-message';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import { mutation } from '@core/util/mutation';
import { LucideGitBranch } from '@lucide/angular';
import {
  AccountListComponent,
  AccountListItem,
} from '@static/components/account-list/account-list.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelComponent } from '@static/components/panel.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { SourceControlIdentityCommandsService } from '@profile/services/source-control-identity-commands.service';

@Component({
  selector: 'app-source-control-accounts',
  imports: [
    AccountListComponent,
    FormField,
    FormInputComponent,
    PanelBodyComponent,
    PanelComponent,
    PanelHeaderComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    @if (provider(); as provider) {
      <section app-panel surface="card">
        <app-panel-header
          density="comfortable"
          [icon]="icon"
          i18n-heading="Heading above the source control accounts on a profile"
          heading="Source Control Accounts"
          i18n-description="
            Explains what mapping source control accounts on a profile does
          "
          description="Commits and pull requests by these accounts are shown as yours. Signing in with GitHub adds your GitHub account automatically." />

        <app-panel-body class="flex flex-col gap-5">
          <app-account-list
            [items]="items()"
            i18n-ariaLabel="
              Accessible label for the list of source control accounts
            "
            ariaLabel="Source control accounts"
            i18n-removeLabel="
              Accessible label prefix for removing a source control account
            "
            removeLabel="Remove"
            i18n-emptyLabel="
              Shown when a profile has no source control accounts
            "
            emptyLabel="No source control accounts yet."
            (removed)="onRemove($event)" />

          <form
            class="flex flex-wrap items-end gap-3"
            (submit)="onAdd($event, provider.provider)">
            <app-form-input
              class="min-w-56 flex-1"
              [formField]="accountForm.username"
              [prefix]="providerName(provider.provider)"
              i18n-label="Label of the source control username field"
              label="Username" />
            <button app-stroked-button type="submit" [disabled]="add.pending()">
              <span
                i18n="
                  Button that maps a source control username to the profile
                ">
                Link Account
              </span>
            </button>
          </form>
        </app-panel-body>
      </section>
    }
  `,
})
export class SourceControlAccountsComponent {
  private readonly commands = inject(SourceControlIdentityCommandsService);
  private readonly snackbar = inject(SnackbarService);

  private readonly providers = sourceControlProvidersResource();
  private readonly identities = sourceControlIdentitiesResource();

  protected readonly icon = LucideGitBranch;

  protected readonly add = mutation({
    fallbackError: $localize`:Shown when mapping a source control username fails:The account could not be linked.`,
  });

  protected readonly provider = computed(() => {
    const providers = this.providers.hasValue() ? this.providers.value() : [];

    return providers[0] ?? null;
  });

  private readonly labels = {
    verified: $localize`:Badge on a source control account proven by signing in:From sign-in`,
    manual: $localize`:Badge on a source control account the user added by hand:Added by you`,
    unknownUsername: $localize`:Shown for a source control account whose username isn't known yet:Username not known yet`,
  };

  protected readonly items = computed<AccountListItem[]>(() => {
    const identities = this.identities.hasValue()
      ? this.identities.value()
      : [];

    return identities.map((identity) => this.toItem(identity));
  });

  protected readonly accountFormModel = signal({ username: '' });
  protected readonly accountForm = form(this.accountFormModel, (schema) => {
    apply(
      schema.username,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Username is required.":Username`,
        minLength: 1,
        maxLength: 100,
      })
    );
  });

  protected providerName(provider: SourceControlProvider) {
    return sourceControlProviderNames[provider];
  }

  protected onAdd(event: Event, provider: SourceControlProvider) {
    event.preventDefault();

    submitForm(this.accountForm, async () => {
      const username = this.accountForm.username().value().trim();

      this.add.run(this.commands.add(provider, username), {
        onSuccess: () => this.accountFormModel.set({ username: '' }),
        onError: (message) => this.snackbar.error(message),
      });
    });
  }

  protected onRemove(identityId: number) {
    this.commands.remove(identityId).subscribe({
      error: (error: unknown) => {
        this.snackbar.error(
          getErrorMessage(
            error,
            $localize`:Shown when removing a source control account fails:The account could not be removed.`
          )
        );
      },
    });
  }

  private toItem(identity: SourceControlIdentity): AccountListItem {
    return {
      id: identity.id,
      provider: sourceControlProviderSlug(identity.provider),
      label: identity.username ?? this.labels.unknownUsername,
      badge: {
        label: identity.isVerified ? this.labels.verified : this.labels.manual,
        color: identity.isVerified ? 'success' : 'neutral',
      },
      removable: true,
    };
  }
}
