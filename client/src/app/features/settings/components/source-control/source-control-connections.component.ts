import { DatePipe } from '@angular/common';
import { Component, input, output, signal } from '@angular/core';
import { LucideRefreshCw } from '@lucide/angular';
import {
  ScmConnectionStatus,
  SourceControlConnection,
  SourceControlProvider,
  sourceControlProviderNames,
} from '@core/models/source-control';
import { fromNow } from '@core/util/dates';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { MediaRowComponent } from '@static/components/media-row.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelComponent } from '@static/components/panel.component';
import { StatusChecklistComponent } from '@static/components/status-checklist/status-checklist.component';
import { TextLinkComponent } from '@static/components/text-link.component';
import { HintTextDirective } from '@static/directives/hint-text.directive';
import { countCheckProblems, toChecklistItems } from './connection-checks';

@Component({
  selector: 'app-source-control-connections',
  imports: [
    AvatarComponent,
    BadgeComponent,
    DatePipe,
    EmptyStateComponent,
    HintTextDirective,
    LucideRefreshCw,
    MediaRowComponent,
    PanelBodyComponent,
    PanelComponent,
    PanelHeaderComponent,
    StatusChecklistComponent,
    StrokedButtonComponent,
    TextLinkComponent,
  ],
  host: { class: 'block' },
  template: `
    <section app-panel surface="card">
      <app-panel-header
        density="comfortable"
        i18n-heading="Heading of the source control connections card"
        heading="Connections"
        i18n-description="Explains what a source control connection is"
        description="Accounts and organisations Netptune can read code from. Only linked repositories are used." />

      <div app-panel-body class="flex flex-col gap-3">
        @for (connection of connections(); track connection.id) {
          <app-media-row [heading]="connection.accountLogin">
            <app-avatar
              mediaRowMedia
              size="lg"
              [tooltip]="false"
              [name]="connection.accountLogin"
              [imageUrl]="connection.accountAvatarUrl" />

            <div class="flex flex-wrap items-center gap-2">
              <span>{{ providerName(connection) }}</span>
              <span aria-hidden="true">·</span>
              <span
                i18n="
                  Number of repositories linked through a source control
                  connection
                "
                >{connection.linkedRepoCount, plural,
                  =0 {No linked repositories}
                  =1 {1 linked repository}
                  other {{{ connection.linkedRepoCount }} linked repositories}
                }</span
              >
              @if (connection.credentialHint) {
                <span aria-hidden="true">·</span>
                @if (connection.credentialExpiresAt) {
                  <span
                    i18n="
                      Describes a connection's personal access token by its last
                      four characters and its expiry date
                    "
                    >Personal access token
                    <code class="font-mono"
                      >••••{{ connection.credentialHint }}</code
                    >
                    · expires
                    {{
                      connection.credentialExpiresAt | date: 'mediumDate'
                    }}</span
                  >
                } @else {
                  <span
                    i18n="
                      Describes a connection's personal access token by its last
                      four characters
                    "
                    >Personal access token
                    <code class="font-mono"
                      >••••{{ connection.credentialHint }}</code
                    ></span
                  >
                }
              }
              @if (connection.checks.length) {
                @let problems = checkProblems(connection);
                @if (problems.failures) {
                  <app-badge
                    color="warn"
                    i18n="Badge counting the failed checks of a connection"
                    >{problems.failures, plural,
                      =1 {1 failed}
                      other {{{ problems.failures }} failed}
                    }</app-badge
                  >
                } @else if (problems.warnings) {
                  <app-badge
                    color="pending"
                    i18n="Badge counting the check warnings of a connection"
                    >{problems.warnings, plural,
                      =1 {1 warning}
                      other {{{ problems.warnings }} warnings}
                    }</app-badge
                  >
                } @else {
                  <app-badge
                    color="success"
                    i18n="Badge on a connection whose checks all passed"
                    >All checks passed</app-badge
                  >
                }
              }
              @if (connection.status === suspended) {
                <app-badge
                  color="warn"
                  i18n="
                    Badge on a source control connection the provider has
                    suspended
                  "
                  >Suspended</app-badge
                >
              }
            </div>

            @if (canManage()) {
              <div mediaRowActions class="flex flex-wrap items-center gap-2">
                @if (connection.canCheck) {
                  <button
                    app-stroked-button
                    color="neutral"
                    type="button"
                    [disabled]="checkingIds().has(connection.id)"
                    (click)="check.emit(connection)">
                    <svg
                      lucideRefreshCw
                      class="h-4 w-4"
                      aria-hidden="true"></svg>
                    <span i18n="Button that re-runs a connection's checks"
                      >Check Connection</span
                    >
                  </button>
                }
                @if (isTokenProvider(connection)) {
                  <button
                    app-stroked-button
                    color="neutral"
                    type="button"
                    (click)="replaceToken.emit(connection)">
                    <span
                      i18n="
                        Button that opens the token dialog to give a connection
                        a new token
                      "
                      >Replace Token</span
                    >
                  </button>
                }
                <button
                  app-stroked-button
                  color="warn"
                  type="button"
                  (click)="disconnect.emit(connection)">
                  <span i18n="Button that removes a source control connection"
                    >Disconnect</span
                  >
                </button>
                <button
                  app-stroked-button
                  color="neutral"
                  type="button"
                  (click)="chooseRepos.emit(connection)">
                  <span
                    i18n="
                      Button that opens the repository picker for a connection
                    "
                    >Choose Repositories</span
                  >
                </button>
              </div>
            }

            @if (connection.checks.length) {
              <div mediaRowFooter class="flex flex-col gap-2">
                <div appHintText class="flex flex-wrap items-center gap-3">
                  @if (connection.lastCheckedAt) {
                    <span
                      i18n="
                        When a connection's checks last ran, as a relative time
                      "
                      >Last checked {{ lastChecked(connection) }}</span
                    >
                  }
                  @if (!needsAttention(connection)) {
                    <button
                      app-text-link
                      size="small"
                      type="button"
                      [attr.aria-expanded]="isExpanded(connection)"
                      (click)="toggleChecks(connection)">
                      @if (isExpanded(connection)) {
                        <span i18n="Hides a connection's check results"
                          >Hide checks</span
                        >
                      } @else {
                        <span i18n="Shows a connection's check results"
                          >Show checks</span
                        >
                      }
                    </button>
                  }
                </div>
                @if (needsAttention(connection) || isExpanded(connection)) {
                  <app-status-checklist [items]="checklist(connection)" />
                }
              </div>
            }
          </app-media-row>
        } @empty {
          <app-empty-state
            compact
            outlined
            i18n-title="
              Shown when a workspace has no source control connections
            "
            title="No connections yet"
            i18n-description="
              Explains how to add the first source control connection
            "
            description="Connect an account or organisation to link its repositories to this workspace." />
        }
      </div>
    </section>
  `,
})
export class SourceControlConnectionsComponent {
  readonly connections = input.required<SourceControlConnection[]>();
  readonly canManage = input(false);

  readonly tokenProviders = input<readonly SourceControlProvider[]>([]);
  readonly checkingIds = input<ReadonlySet<number>>(new Set());

  readonly chooseRepos = output<SourceControlConnection>();
  readonly disconnect = output<SourceControlConnection>();
  readonly check = output<SourceControlConnection>();
  readonly replaceToken = output<SourceControlConnection>();

  protected readonly suspended = ScmConnectionStatus.suspended;

  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());

  protected checkProblems(connection: SourceControlConnection) {
    return countCheckProblems(connection.checks);
  }

  protected needsAttention(connection: SourceControlConnection): boolean {
    const problems = countCheckProblems(connection.checks);

    return problems.failures > 0 || problems.warnings > 0;
  }

  protected checklist(connection: SourceControlConnection) {
    return toChecklistItems(connection.checks);
  }

  protected lastChecked(connection: SourceControlConnection): string {
    return fromNow(connection.lastCheckedAt);
  }

  protected isTokenProvider(connection: SourceControlConnection): boolean {
    return this.tokenProviders().includes(connection.provider);
  }

  protected isExpanded(connection: SourceControlConnection): boolean {
    return this.expandedIds().has(connection.id);
  }

  protected toggleChecks(connection: SourceControlConnection) {
    this.expandedIds.update((ids) => {
      const next = new Set(ids);

      if (next.has(connection.id)) {
        next.delete(connection.id);
      } else {
        next.add(connection.id);
      }

      return next;
    });
  }

  protected providerName(connection: SourceControlConnection): string {
    return sourceControlProviderNames[connection.provider];
  }
}
