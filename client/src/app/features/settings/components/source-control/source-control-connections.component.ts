import { Component, input, output } from '@angular/core';
import {
  ScmConnectionStatus,
  SourceControlConnection,
  sourceControlProviderNames,
} from '@core/models/source-control';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { MediaRowComponent } from '@static/components/media-row.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelComponent } from '@static/components/panel.component';

@Component({
  selector: 'app-source-control-connections',
  imports: [
    AvatarComponent,
    BadgeComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    MediaRowComponent,
    PanelBodyComponent,
    PanelComponent,
    PanelHeaderComponent,
    StrokedButtonComponent,
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
              <div mediaRowActions class="flex items-center gap-2">
                <button
                  app-flat-button
                  color="ghost"
                  type="button"
                  class="text-warn"
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

  readonly chooseRepos = output<SourceControlConnection>();
  readonly disconnect = output<SourceControlConnection>();

  protected readonly suspended = ScmConnectionStatus.suspended;

  protected providerName(connection: SourceControlConnection): string {
    return sourceControlProviderNames[connection.provider];
  }
}
