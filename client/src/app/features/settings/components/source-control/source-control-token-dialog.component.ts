import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import {
  SourceControlConnection,
  SourceControlProvider,
  sourceControlProviderNames,
} from '@core/models/source-control';
import { SourceControlCommandsService } from '@core/services/source-control-commands.service';
import { mutation } from '@core/util/mutation';
import {
  AccessTokenFormComponent,
  AccessTokenFormValue,
  emptyAccessTokenFormValue,
} from '@static/components/access-token-form/access-token-form.component';
import { BulletListComponent } from '@static/components/bullet-list.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { SupportingTextComponent } from '@static/components/supporting-text.component';
import { TextLinkComponent } from '@static/components/text-link.component';

export interface SourceControlTokenDialogData {
  provider: SourceControlProvider;
}

const ORGANISATION_NAME = /^[A-Za-z0-9](?:[A-Za-z0-9-]{0,48}[A-Za-z0-9])?$/;
const DEV_AZURE_HOST = 'dev.azure.com';
const VISUAL_STUDIO_HOST_SUFFIX = '.visualstudio.com';

@Component({
  selector: 'app-source-control-token-dialog',
  imports: [
    AccessTokenFormComponent,
    BulletListComponent,
    CalloutComponent,
    DialogTitleComponent,
    SupportingTextComponent,
    TextLinkComponent,
  ],
  template: `
    <app-dialog-title
      i18n="
        Title of the dialog that connects a source control provider with a token
      "
      >Connect {{ providerName }}</app-dialog-title
    >

    <app-access-token-form
      [(value)]="draft"
      i18n-accountLabel="
        Label of the field for the Azure DevOps organisation to connect
      "
      accountLabel="Organisation"
      [accountPlaceholder]="organisationPlaceholder"
      i18n-tokenLabel="
        Label of the field for an Azure DevOps personal access token
      "
      tokenLabel="Personal access token"
      i18n-expiryHint="
        Hint under the optional expiry date of a source control access token
      "
      expiryHint="Shown on the connection so you know when to replace it"
      i18n-submitLabel="
        Button that connects a source control provider with a token
      "
      submitLabel="Connect"
      [error]="connect.error()"
      [pending]="connect.pending()"
      (submitted)="onSubmit($event)"
      (cancelled)="dialogRef.close()">
      <app-callout class="mb-5">
        <div class="flex flex-col gap-2">
          <p
            i18n="
              Introduces the Azure DevOps scopes a personal access token needs
            ">
            Create the token with these scopes:
          </p>
          <ul app-bullet-list>
            <li i18n="Azure DevOps token scope for reading repositories">
              Code (read)
            </li>
            <li i18n="Azure DevOps token scope for reading work items">
              Work Items (read)
            </li>
            <li i18n="Azure DevOps token scope for reading pipelines">
              Build (read)
            </li>
            <li i18n="Azure DevOps token scope for reading projects and teams">
              Project and Team (read)
            </li>
          </ul>
          <p
            i18n="
              Explains that an Azure DevOps token must belong to one
              organisation
            ">
            Scope the token to this one organisation, not to all accessible
            organisations.
          </p>
          @if (tokensUrl(); as url) {
            <p app-supporting-text class="break-all">
              <span
                i18n="
                  Introduces the address where Azure DevOps tokens are created
                "
                >Create it at</span
              >
              {{ url }}
              <a
                app-text-link
                target="_blank"
                rel="noopener noreferrer"
                [href]="url"
                i18n="Link that opens the Azure DevOps token page in a new tab"
                >Open</a
              >
            </p>
          }
        </div>
      </app-callout>
    </app-access-token-form>
  `,
})
export class SourceControlTokenDialogComponent {
  protected readonly dialogRef =
    inject<DialogRef<SourceControlConnection>>(DialogRef);
  private readonly data = inject<SourceControlTokenDialogData>(DIALOG_DATA);
  private readonly commands = inject(SourceControlCommandsService);

  protected readonly providerName =
    sourceControlProviderNames[this.data.provider];
  protected readonly organisationPlaceholder = 'https://dev.azure.com/your-org';

  protected readonly draft = signal<AccessTokenFormValue>(
    emptyAccessTokenFormValue
  );

  protected readonly connect = mutation({
    fallbackError: $localize`:Fallback when completing a source control connection fails:The provider did not confirm the connection.`,
  });

  protected readonly tokensUrl = computed(() => {
    const organisation = readOrganisation(this.draft().account);

    if (!organisation) {
      return null;
    }

    return `https://dev.azure.com/${encodeURIComponent(organisation)}/_usersSettings/tokens`;
  });

  protected onSubmit(value: AccessTokenFormValue) {
    const parameters = {
      organisation: value.account,
      token: value.token,
      expiresOn: value.expiresOn,
    };

    this.connect.run(
      this.commands.completeTokenConnect(this.data.provider, parameters),
      { onSuccess: (connection) => this.dialogRef.close(connection) }
    );
  }
}

function readOrganisation(input: string): string | null {
  const trimmed = input.trim().replace(/\/+$/, '');
  const isName = ORGANISATION_NAME.test(trimmed);

  if (isName) {
    return trimmed;
  }

  const address = trimmed.includes('://') ? trimmed : `https://${trimmed}`;
  const url = parseUrl(address);

  if (!url) {
    return null;
  }

  const name = readOrganisationFromUrl(url);
  const isValidName = !!name && ORGANISATION_NAME.test(name);

  return isValidName ? name : null;
}

function readOrganisationFromUrl(url: URL): string | null {
  const host = url.hostname.toLowerCase();

  if (host === DEV_AZURE_HOST) {
    const firstSegment = url.pathname.split('/').find(Boolean);

    return firstSegment ?? null;
  }

  const isVisualStudioHost = host.endsWith(VISUAL_STUDIO_HOST_SUFFIX);

  return isVisualStudioHost
    ? host.slice(0, -VISUAL_STUDIO_HOST_SUFFIX.length)
    : null;
}

function parseUrl(address: string): URL | null {
  try {
    return new URL(address);
  } catch {
    return null;
  }
}
