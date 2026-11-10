import { Component, inject, input, output } from '@angular/core';
import {
  SourceControlConnection,
  SourceControlProvider,
  SourceControlProviderInfo,
  sourceControlProviderNames,
  sourceControlProviderSlug,
} from '@core/models/source-control';
import { SourceControlCommandsService } from '@core/services/source-control-commands.service';
import { mutation } from '@core/util/mutation';
import { SourceControlRepoPickerComponent } from '@shared/components/source-control-repo-picker/source-control-repo-picker.component';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { CheckLabelComponent } from '@static/components/check-label/check-label.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { FormErrorComponent } from '@static/components/form-error/form-error.component';
import { MediaRowComponent } from '@static/components/media-row.component';
import { ProviderMarkComponent } from '@static/components/provider-mark.component';

@Component({
  selector: 'app-setup-repository-step',
  imports: [
    AvatarComponent,
    CheckLabelComponent,
    ErrorStateComponent,
    FlatButtonComponent,
    FormErrorComponent,
    MediaRowComponent,
    ProviderMarkComponent,
    SourceControlRepoPickerComponent,
  ],
  host: { class: 'flex flex-col gap-4' },
  template: `
    @if (loadFailed()) {
      <app-error-state
        compact
        i18n-title="
          Shown when the source control step of workspace setup cannot load
        "
        title="Source control settings could not be loaded"
        (retry)="retry.emit()" />
    } @else {
      @for (connection of connections(); track connection.id) {
        <app-media-row [heading]="connection.accountLogin">
          <app-avatar
            mediaRowMedia
            size="lg"
            [tooltip]="false"
            [name]="connection.accountLogin"
            [imageUrl]="connection.accountAvatarUrl" />

          <span>{{ providerName(connection.provider) }}</span>

          <app-check-label
            mediaRowActions
            i18n="Shown when a source control account is connected"
            >Connected</app-check-label
          >
        </app-media-row>

        <app-source-control-repo-picker #picker [connection]="connection" />

        <button
          app-flat-button
          color="ghost"
          class="self-start"
          type="button"
          [disabled]="picker.refresh.pending()"
          (click)="picker.sync()">
          <span i18n="Button that reloads the repository list from the provider"
            >Refresh List</span
          >
        </button>
      } @empty {
        @for (provider of redirectProviders(); track provider.provider) {
          <app-media-row [heading]="providerName(provider.provider)">
            <app-provider-mark
              mediaRowMedia
              [provider]="providerSlug(provider.provider)"
              [size]="24" />

            @if (connect.error(); as message) {
              <app-form-error role="alert">{{ message }}</app-form-error>
            } @else {
              <span
                i18n="
                  Explains the redirect when connecting a source control
                  provider
                ">
                You go to the provider, then return to this step.
              </span>
            }

            <button
              mediaRowActions
              app-flat-button
              type="button"
              [disabled]="connect.pending()"
              (click)="beginConnect(provider)">
              @if (connect.error()) {
                <span i18n="Button that retries a failed action"
                  >Try Again</span
                >
              } @else {
                <span i18n="Button that connects a source control provider"
                  >Connect {{ providerName(provider.provider) }}</span
                >
              }
            </button>
          </app-media-row>
        }
      }
    }
  `,
})
export class SetupRepositoryStepComponent {
  private readonly commands = inject(SourceControlCommandsService);

  readonly workspace = input.required<string>();
  readonly returnTo = input.required<string>();
  readonly redirectProviders = input.required<SourceControlProviderInfo[]>();
  readonly connections = input.required<SourceControlConnection[]>();
  readonly loadFailed = input(false);

  readonly retry = output();

  protected readonly connect = mutation({
    fallbackError: $localize`:Shown when starting a source control connection fails:The connection could not be started.`,
  });

  protected providerName(provider: SourceControlProvider): string {
    return sourceControlProviderNames[provider];
  }

  protected providerSlug(provider: SourceControlProvider): string {
    return sourceControlProviderSlug(provider);
  }

  protected beginConnect({ provider }: SourceControlProviderInfo) {
    this.connect.run(
      this.commands.beginConnect({
        workspace: this.workspace(),
        provider,
        returnTo: this.returnTo(),
      })
    );
  }
}
