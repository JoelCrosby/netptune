import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { formatDate } from '@angular/common';
import { Component, LOCALE_ID, computed, inject, signal } from '@angular/core';
import { LucideExternalLink, LucideRefreshCw } from '@lucide/angular';
import {
  ScmConnectionCheck,
  SourceControlConnection,
  SourceControlConnectCompletion,
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
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import {
  CheckSummaryComponent,
  CheckSummaryRowComponent,
} from '@static/components/check-summary/check-summary.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import {
  FieldTableComponent,
  FieldTableRow,
} from '@static/components/field-table/field-table.component';
import {
  StatusChecklistComponent,
  StatusChecklistItem,
} from '@static/components/status-checklist/status-checklist.component';
import { SupportingTextComponent } from '@static/components/supporting-text.component';
import { TextLinkComponent } from '@static/components/text-link.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { countCheckProblems, toChecklistItems } from './connection-checks';

export interface SourceControlTokenDialogData {
  provider: SourceControlProvider;
  existingOrganisation?: string;
}

type DialogPhase = 'setup' | 'checking' | 'results';

const ORGANISATION_NAME = /^[A-Za-z0-9](?:[A-Za-z0-9-]{0,48}[A-Za-z0-9])?$/;
const DEV_AZURE_HOST = 'dev.azure.com';
const VISUAL_STUDIO_HOST_SUFFIX = '.visualstudio.com';

// Microsoft's own sign-in link for Azure DevOps, from "Create an organization" on Microsoft Learn;
// it lands on the page with the New organization button.
const CREATE_ORGANISATION_URL =
  'https://go.microsoft.com/fwlink/?LinkId=307137';

const READ = 'Read';

@Component({
  selector: 'app-source-control-token-dialog',
  imports: [
    AccessTokenFormComponent,
    CalloutComponent,
    CheckSummaryComponent,
    CheckSummaryRowComponent,
    DialogActionsDirective,
    DialogTitleComponent,
    FieldTableComponent,
    FlatButtonComponent,
    LucideExternalLink,
    LucideRefreshCw,
    StatusChecklistComponent,
    StrokedButtonComponent,
    SupportingTextComponent,
    TextLinkComponent,
  ],
  template: `
    @if (isReplacing) {
      <app-dialog-title
        i18n="
          Title of the dialog that replaces the token of an existing source
          control connection
        "
        >Replace Token</app-dialog-title
      >
    } @else {
      <app-dialog-title
        i18n="
          Title of the dialog that connects a source control provider with a
          token
        "
        >Connect {{ providerName }}</app-dialog-title
      >
    }

    @if (phase() === 'setup') {
      <app-access-token-form
        [(value)]="draft"
        i18n-accountStepTitle="
          Title of the step that names the Azure DevOps organisation to connect
        "
        accountStepTitle="Organisation"
        i18n-setupStepTitle="
          Title of the step that explains how to create an Azure DevOps personal
          access token
        "
        setupStepTitle="Create a personal access token"
        i18n-tokenStepTitle="
          Title of the step where the Azure DevOps token is pasted
        "
        tokenStepTitle="Paste the token"
        i18n-accountLabel="
          Label of the field for the Azure DevOps organisation to connect
        "
        accountLabel="Organisation name or URL"
        [accountPlaceholder]="organisationPlaceholder"
        [accountRecognised]="!!organisation()"
        [accountReadonly]="isReplacing"
        i18n-tokenLabel="
          Label of the field for an Azure DevOps personal access token
        "
        tokenLabel="Personal access token"
        i18n-expiryLabel="
          Label of the optional field for the date an Azure DevOps token expires
        "
        expiryLabel="Expires on (optional)"
        i18n-submitLabel="
          Button that checks an Azure DevOps token and connects when the checks
          pass
        "
        submitLabel="Check and connect"
        [error]="connect.error()"
        [pending]="connect.pending()"
        (submitted)="onSubmit($event)"
        (cancelled)="dialogRef.close()">
        <div accessTokenFormAccountHelp class="flex flex-col gap-2">
          @if (!isReplacing) {
            @if (organisation(); as organisation) {
              <ul app-check-summary>
                <li
                  app-check-summary-row
                  [mono]="true"
                  i18n-label="
                    Label before the Azure DevOps organisation name read from
                    what was typed
                  "
                  label="Connecting"
                  [value]="organisation"></li>
              </ul>
            }
            <p app-supporting-text>
              <span
                i18n="
                  Start of the hint for people without an Azure DevOps
                  organisation
                "
                >No organisation yet?</span
              >
              <a
                app-text-link
                target="_blank"
                rel="noopener noreferrer"
                class="inline-flex items-center gap-1"
                [href]="createOrganisationUrl"
                ><span
                  i18n="Link that opens Azure DevOps to create an organisation"
                  >Create one in Azure DevOps</span
                ><svg
                  lucideExternalLink
                  class="h-3 w-3"
                  aria-hidden="true"></svg
              ></a>
              <span
                i18n="
                  End of the hint for people without an Azure DevOps
                  organisation
                "
                >and come back. Azure DevOps asks for an Azure subscription when
                you create one, and the first five users are free.</span
              >
            </p>
          }
        </div>

        <div accessTokenFormSetup class="flex flex-col gap-2">
          <p app-supporting-text>
            @if (organisation(); as organisation) {
              <span
                i18n="
                  Says who can create the Azure DevOps token for a named
                  organisation. Custom defined is the Azure DevOps option name
                "
                >You must be a Project Collection Administrator of
                <strong>{{ organisation }}</strong
                >. In the token page, choose <strong>Custom defined</strong> and
                match these settings.</span
              >
            } @else {
              <span
                i18n="
                  Says who can create the Azure DevOps token. Custom defined is
                  the Azure DevOps option name
                "
                >You must be a Project Collection Administrator of the
                organisation. In the token page, choose
                <strong>Custom defined</strong> and match these settings.</span
              >
            }
          </p>

          <app-field-table
            i18n-heading="
              Heading of the table listing the settings for a new Azure DevOps
              token
            "
            heading="Settings for the new token"
            [rows]="tokenSettings()">
            @if (tokensUrl(); as url) {
              <a
                fieldTableAction
                app-text-link
                size="small"
                target="_blank"
                rel="noopener noreferrer"
                class="inline-flex items-center gap-1"
                [href]="url"
                ><span
                  i18n="
                    Link that opens the Azure DevOps token page of an
                    organisation in a new tab
                  "
                  >Open token page for {{ organisation() }}</span
                ><svg
                  lucideExternalLink
                  class="h-3 w-3"
                  aria-hidden="true"></svg
              ></a>
            }
          </app-field-table>
        </div>

        <p accessTokenFormTokenHelp app-supporting-text>
          <span
            i18n="
              Explains that Netptune checks the token and that Azure DevOps
              doesn't report its expiry date
            "
            >Netptune checks the token before saving it. Azure DevOps doesn't
            tell Netptune the expiry date, so copy it from the token page if you
            want a reminder.</span
          >
        </p>
      </app-access-token-form>
    } @else {
      <div class="flex flex-col gap-4">
        <ul app-check-summary>
          <li
            app-check-summary-row
            i18n-label="
              Label before the Azure DevOps organisation being checked
            "
            label="Organisation"
            [value]="organisation() ?? ''"></li>
          <li
            app-check-summary-row
            [mono]="true"
            i18n-label="
              Label before the last characters of the token being checked
            "
            label="Token"
            [value]="tokenHint()"
            [detail]="tokenExpiry()">
            @if (phase() === 'results') {
              <button
                app-text-link
                type="button"
                (click)="changeSetup()"
                i18n="Returns to the token form to change what was entered">
                Change
              </button>
            }
          </li>
        </ul>

        @if (phase() === 'results') {
          @if (hasFailure()) {
            <app-callout
              color="warn"
              role="alert"
              i18n-title="Headline when a required connection check failed"
              title="Netptune can't connect with this token.">
              <p
                i18n="
                  Tells the user to fix the failed check in the provider. {0} is
                  the provider name
                ">
                Fix what the failed check describes in {{ providerName }}, then
                check again.
              </p>
            </app-callout>
          } @else if (hasWarning()) {
            <app-callout
              color="caution"
              i18n-title="
                Headline when only optional connection checks have warnings
              "
              title="You can connect, but some features won't work.">
              <p
                i18n="
                  Explains the choice when only optional connection checks have
                  warnings
                ">
                Add the missing scopes to the token now, or connect and add them
                later. Netptune re-checks when you choose Check Connection.
              </p>
            </app-callout>
          }
        }

        @if (phase() === 'checking') {
          <app-status-checklist [items]="pendingItems()" />
        } @else {
          @if (requiredItems().length) {
            <app-status-checklist
              i18n-heading="
                Heading of the connection checks that must pass to connect
              "
              heading="Required"
              [items]="requiredItems()" />
          }
          @if (optionalItems().length) {
            <app-status-checklist
              i18n-heading="
                Heading of the connection checks only later features need
              "
              heading="Used by later features"
              [items]="optionalItems()" />
          }
        }
      </div>

      <div app-dialog-actions align="end" class="items-center">
        @if (phase() === 'checking') {
          <p
            app-supporting-text
            class="mr-auto"
            i18n="
              Shown while Netptune checks a token with the provider. {0} is the
              provider name
            ">
            Checking with {{ providerName }}…
          </p>
        }
        <button
          app-stroked-button
          type="button"
          [disabled]="phase() === 'checking'"
          (click)="dialogRef.close()">
          <span i18n="Dismisses a dialog without acting">Cancel</span>
        </button>
        @if (phase() === 'checking') {
          <button app-flat-button color="primary" type="button" disabled>
            <span i18n="Disabled button shown while connection checks run"
              >Checking…</span
            >
          </button>
        } @else if (hasFailure()) {
          <button
            app-flat-button
            color="primary"
            type="button"
            (click)="checkAgain()">
            <svg lucideRefreshCw class="h-4 w-4" aria-hidden="true"></svg>
            <span i18n="Button that runs the connection checks again"
              >Check again</span
            >
          </button>
        } @else {
          <button app-stroked-button type="button" (click)="checkAgain()">
            <svg lucideRefreshCw class="h-4 w-4" aria-hidden="true"></svg>
            <span i18n="Button that runs the connection checks again"
              >Check again</span
            >
          </button>
          <button
            app-flat-button
            color="primary"
            type="button"
            (click)="connectAnyway()">
            <span
              i18n="
                Button that connects even though optional connection checks have
                warnings
              "
              >Connect</span
            >
          </button>
        }
      </div>
    }
  `,
})
export class SourceControlTokenDialogComponent {
  protected readonly dialogRef =
    inject<DialogRef<SourceControlConnection>>(DialogRef);
  private readonly data = inject<SourceControlTokenDialogData>(DIALOG_DATA);
  private readonly commands = inject(SourceControlCommandsService);
  private readonly locale = inject(LOCALE_ID);

  protected readonly providerName =
    sourceControlProviderNames[this.data.provider];
  protected readonly organisationPlaceholder = 'https://dev.azure.com/your-org';
  protected readonly createOrganisationUrl = CREATE_ORGANISATION_URL;
  protected readonly isReplacing = !!this.data.existingOrganisation;

  protected readonly draft = signal<AccessTokenFormValue>({
    ...emptyAccessTokenFormValue,
    account: this.data.existingOrganisation ?? '',
  });
  protected readonly phase = signal<DialogPhase>('setup');
  protected readonly checks = signal<ScmConnectionCheck[]>([]);
  private readonly submitted = signal<AccessTokenFormValue | null>(null);

  protected readonly connect = mutation({
    fallbackError: $localize`:Fallback when completing a source control connection fails:The provider did not confirm the connection.`,
  });

  protected readonly organisation = computed(() => {
    return readOrganisation(this.draft().account);
  });

  protected readonly tokensUrl = computed(() => {
    const organisation = this.organisation();

    if (!organisation) {
      return null;
    }

    return `https://dev.azure.com/${encodeURIComponent(organisation)}/_usersSettings/tokens`;
  });

  protected readonly tokenSettings = computed<FieldTableRow[]>(() => {
    const organisation =
      this.organisation() ??
      $localize`:Placeholder for the Azure DevOps organisation in the token settings table:your organisation`;

    return [
      {
        label: 'Organization',
        value: organisation,
        note: $localize`:Note that the Azure DevOps token must not cover every organisation. All accessible organizations is the Azure DevOps option name:(not "All accessible organizations")`,
      },
      {
        label: 'Expiration',
        value: 'Custom defined',
        note: $localize`:Note on the longest expiry an Azure DevOps token can have:up to 1 year`,
      },
      { label: 'Code', value: READ },
      { label: 'Project and Team', value: READ },
      {
        label: 'Work Items',
        value: READ,
        tag: $localize`:Tag saying which feature needs the Azure DevOps Work Items scope:For imports`,
      },
      {
        label: 'Build',
        value: READ,
        tag: $localize`:Tag saying which feature needs the Azure DevOps Build scope:For build status`,
      },
    ];
  });

  protected readonly tokenHint = computed(() => {
    return `••••${(this.submitted()?.token ?? '').slice(-4)}`;
  });

  protected readonly tokenExpiry = computed(() => {
    const expiresOn = this.submitted()?.expiresOn;

    if (!expiresOn) {
      return null;
    }

    const date = formatDate(expiresOn, 'mediumDate', this.locale);

    return $localize`:Expiry date of the token being checked. {0} is the date:expires ${date}:date:`;
  });

  protected readonly requiredItems = computed(() => {
    const required = this.checks().filter((check) => check.isRequired);

    return toChecklistItems(required);
  });

  protected readonly optionalItems = computed(() => {
    const optional = this.checks().filter((check) => !check.isRequired);

    return toChecklistItems(optional);
  });

  protected readonly pendingItems = computed<StatusChecklistItem[]>(() => {
    const checkingDetail = $localize`:Detail of a connection check that is still running:Checking…`;
    const previous = this.checks();

    if (!previous.length) {
      return [
        {
          key: 'checking',
          status: 'pending',
          title: $localize`:Title of the placeholder shown while connection checks run. {0} is the provider name:Checking with ${this.providerName}:provider:`,
          detail: checkingDetail,
        },
      ];
    }

    return previous.map((check) => {
      return {
        key: check.key,
        status: 'pending',
        title: check.title,
        detail: checkingDetail,
      };
    });
  });

  private readonly problems = computed(() => countCheckProblems(this.checks()));

  protected readonly hasFailure = computed(() => this.problems().failures > 0);
  protected readonly hasWarning = computed(() => this.problems().warnings > 0);

  protected onSubmit(value: AccessTokenFormValue) {
    this.submitted.set(value);
    this.run(value, false);
  }

  protected checkAgain() {
    const value = this.submitted();

    if (!value) {
      return;
    }

    this.run(value, false);
  }

  protected connectAnyway() {
    const value = this.submitted();

    if (!value) {
      return;
    }

    this.run(value, true);
  }

  private setPhase(phase: DialogPhase) {
    this.phase.set(phase);
    this.dialogRef.disableClose = phase === 'checking';
  }

  protected changeSetup() {
    this.connect.clearError();
    this.setPhase('setup');
  }

  private run(value: AccessTokenFormValue, acceptWarnings: boolean) {
    const parameters = {
      organisation: value.account,
      token: value.token,
      expiresOn: value.expiresOn,
    };

    this.setPhase('checking');
    this.connect.run(
      this.commands.completeTokenConnect(
        this.data.provider,
        parameters,
        acceptWarnings
      ),
      {
        onSuccess: (completion) => this.onCompleted(completion),
        onError: () => this.setPhase('setup'),
      }
    );
  }

  private onCompleted(completion: SourceControlConnectCompletion) {
    if (completion.connection) {
      this.dialogRef.close(completion.connection);

      return;
    }

    this.checks.set(completion.checks ?? []);
    this.setPhase('results');
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
