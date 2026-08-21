import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { ClientResponse } from '@core/models/client-response';
import {
  ArchiveImportMode,
  ArchiveImportPreview,
  ArchiveImportResult,
} from '@core/models/view-models/archive-import';
import { formatBytes } from '@core/util/bytes';
import {
  LucideCircleCheck,
  LucideFileArchive,
  LucideListTree,
  LucideUpload,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { FileDropzoneComponent } from '@static/components/file-dropzone/file-dropzone.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelComponent } from '@static/components/panel.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import {
  SummaryListComponent,
  SummaryListItem,
} from '@static/components/summary-list/summary-list.component';
import { SwitchComponent } from '@static/components/switch/switch.component';

const MaxArchiveBytes = 2 * 1024 * 1024 * 1024;

@Component({
  selector: 'app-archive-import-view',
  imports: [
    FileDropzoneComponent,
    FlatButtonComponent,
    FormInputComponent,
    LucideUpload,
    PageContainerComponent,
    PageHeaderComponent,
    PanelComponent,
    PanelHeaderComponent,
    RouterLink,
    SegmentedControlComponent,
    SummaryListComponent,
    SwitchComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the workspace archive import"
        title="Import Archive" />

      <app-panel>
        <app-panel-header
          [icon]="archiveIcon"
          i18n-heading="Heading of the archive file panel"
          heading="Archive"
          i18n-description="Explains what an archive import accepts"
          description="Upload a .nptz archive produced by an archive export." />

        <div class="flex flex-col gap-6 p-5">
          <app-file-dropzone
            [acceptTypes]="acceptedExtensions"
            [maxBytes]="maxBytes"
            [disabled]="isBusy()"
            (filesSelected)="onFileSelected($event)" />

          @if (file(); as chosen) {
            <p class="text-sm font-medium">
              {{ chosen.name }}
              <span class="text-muted font-normal">
                {{ formatBytes(chosen.size) }}
              </span>
            </p>
          }

          <div class="flex flex-col gap-2">
            <span
              class="text-sm font-medium"
              i18n="Label for where an archive is imported into">
              Destination
            </span>
            @if (modes().length > 1) {
              <app-segmented-control
                [options]="modes()"
                [(value)]="mode"
                i18n-ariaLabel="
                  Accessible label for the archive destination choice
                "
                ariaLabel="Archive destination" />
            }
            <span class="text-muted text-xs">
              @if (mode() === 'clone') {
                <ng-container i18n="Explains what cloning an archive does">
                  A new workspace is created from the archive.
                </ng-container>
              } @else {
                <ng-container i18n="Explains what restoring an archive does">
                  The archive is restored into this workspace, which has to have
                  no projects yet.
                </ng-container>
              }
            </span>
          </div>

          @if (mode() === 'clone') {
            <app-form-input
              name="target-slug"
              [(value)]="targetSlug"
              i18n-label="
                Label of the slug for the workspace an archive creates
              "
              label="New workspace slug"
              i18n-hint="Explains what the new workspace slug is used for"
              hint="This becomes the workspace's address, so it has to be unused." />
          }

          <div class="flex items-center justify-between gap-4">
            <span
              class="text-sm font-medium"
              i18n="Label of the archive member invite toggle">
              Invite members without an account
            </span>
            <app-switch
              [(checked)]="inviteUnmatchedMembers"
              i18n-ariaLabel="Label of the archive member invite toggle"
              ariaLabel="Invite members without an account" />
          </div>

          <div class="flex items-center gap-3">
            <button
              app-flat-button
              type="button"
              [disabled]="!canCheck()"
              (click)="check()">
              <svg lucideUpload class="h-4 w-4"></svg>
              <span i18n="Button that reads an archive without importing it">
                Check Archive
              </span>
            </button>
            @if (isBusy()) {
              <span
                class="text-muted text-sm"
                i18n="Shown while an archive is being read or imported">
                Working…
              </span>
            }
          </div>

          @if (error(); as message) {
            <p class="text-destructive text-sm" aria-live="polite">
              {{ message }}
            </p>
          }
        </div>
      </app-panel>

      @if (preview(); as archive) {
        <app-panel class="mt-6 block">
          <app-panel-header
            [icon]="contentsIcon"
            i18n-heading="Heading of the archive contents panel"
            heading="Archive contents" />

          <app-summary-list [items]="summary()" />

          @if (counts().length) {
            <app-summary-list [items]="counts()" />
          }

          @if (archive.schemaUpgrades.length) {
            <div class="px-5 py-4 text-sm">
              <p
                class="font-medium"
                i18n="
                  Heading above the schema changes an archive import applies
                ">
                Schema upgrades
              </p>
              <ul class="text-muted m-0 mt-1 list-disc pl-5">
                @for (upgrade of archive.schemaUpgrades; track upgrade) {
                  <li>{{ upgrade }}</li>
                }
              </ul>
            </div>
          }

          @if (archive.unmatchedMemberEmails.length) {
            <div class="px-5 py-4 text-sm">
              <p
                class="font-medium"
                i18n="Heading above archive members with no Netptune account">
                Members without an account
              </p>
              <ul class="text-muted m-0 mt-1 list-disc pl-5">
                @for (email of archive.unmatchedMemberEmails; track email) {
                  <li>{{ email }}</li>
                }
              </ul>
            </div>
          }

          @if (archive.blockers.length) {
            <div class="text-destructive px-5 py-4 text-sm">
              <p
                class="font-medium"
                i18n="Heading above the reasons an archive cannot be imported">
                This archive cannot be imported
              </p>
              <ul class="m-0 mt-1 list-disc pl-5">
                @for (blocker of archive.blockers; track blocker) {
                  <li>{{ blocker }}</li>
                }
              </ul>
            </div>
          }

          <div class="border-border border-t px-5 py-4">
            <button
              app-flat-button
              type="button"
              [disabled]="!canImport()"
              (click)="import()">
              <span i18n="Button that commits an archive import">
                Import Archive
              </span>
            </button>
          </div>
        </app-panel>
      }

      @if (result(); as imported) {
        <app-panel class="mt-6 block">
          <app-panel-header
            [icon]="completeIcon"
            i18n-heading="Heading of the finished archive import panel"
            heading="Import complete" />

          <app-summary-list [items]="created()" />

          @if (imported.warnings.length) {
            <div class="px-5 py-4 text-sm">
              <p
                class="font-medium"
                i18n="Heading above problems an archive import worked around">
                Warnings
              </p>
              <ul class="text-muted m-0 mt-1 list-disc pl-5">
                @for (warning of imported.warnings; track warning) {
                  <li>{{ warning }}</li>
                }
              </ul>
            </div>
          }

          <div class="border-border border-t px-5 py-4">
            <a app-flat-button [routerLink]="['/', imported.workspaceSlug]">
              <span i18n="Link to the workspace an archive import produced">
                Open Workspace
              </span>
            </a>
          </div>
        </app-panel>
      }
    </app-page-container>
  `,
})
export class ArchiveImportViewComponent {
  private readonly http = inject(HttpClient);

  protected readonly formatBytes = formatBytes;
  protected readonly archiveIcon = LucideFileArchive;
  protected readonly contentsIcon = LucideListTree;
  protected readonly completeIcon = LucideCircleCheck;
  protected readonly maxBytes = MaxArchiveBytes;
  protected readonly acceptedExtensions = '.nptz,.zip';

  private readonly canCloneWorkspace = hasPermission(
    PERMISSIONS.workspace.create
  );

  protected readonly modes = computed<SegmentedOption<ArchiveImportMode>[]>(
    () => {
      const restore: SegmentedOption<ArchiveImportMode> = {
        value: 'restore',
        label: $localize`:Archive destination that fills the current workspace:This workspace`,
      };

      if (!this.canCloneWorkspace()) {
        return [restore];
      }

      return [
        {
          value: 'clone',
          label: $localize`:Archive destination that creates a workspace:New workspace`,
        },
        restore,
      ];
    }
  );

  protected readonly file = signal<File | null>(null);
  protected readonly mode = signal<ArchiveImportMode>(
    this.canCloneWorkspace() ? 'clone' : 'restore'
  );
  protected readonly targetSlug = signal('');
  protected readonly inviteUnmatchedMembers = signal(false);

  protected readonly isBusy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly preview = signal<ArchiveImportPreview | null>(null);
  protected readonly result = signal<ArchiveImportResult | null>(null);

  protected readonly canCheck = computed(() => {
    const needsSlug = this.mode() === 'clone' && !this.targetSlug().trim();

    return !!this.file() && !needsSlug && !this.isBusy();
  });

  protected readonly canImport = computed(() => {
    const archive = this.preview();

    return !!archive && !archive.blockers.length && !this.isBusy();
  });

  protected readonly summary = computed((): SummaryListItem[] => {
    const archive = this.preview();

    if (!archive) return [];

    return [
      {
        label: $localize`:Name of the workspace an archive came from:Source workspace`,
        value: `${archive.workspaceName} (${archive.workspaceSlug})`,
        truncate: true,
      },
      {
        label: $localize`:When an archive was produced:Created`,
        value: new Date(archive.createdAt).toLocaleString(),
      },
      {
        label: $localize`:Version of the archive file format:Schema version`,
        value: `${archive.schemaVersion}`,
      },
      {
        label: $localize`:Size of an archive file:Archive size`,
        value: formatBytes(archive.fileBytes),
      },
      {
        label: $localize`:Storage a workspace has left:Storage remaining`,
        value: formatBytes(archive.remainingQuotaBytes),
        muted: true,
      },
    ];
  });

  protected readonly counts = computed((): SummaryListItem[] => {
    const archive = this.preview();

    if (!archive) return [];

    return Object.entries(archive.countsByType).map(([type, count]) => {
      return { label: type, value: `${count}`, muted: true };
    });
  });

  protected readonly created = computed((): SummaryListItem[] => {
    const imported = this.result();

    if (!imported) return [];

    return Object.entries(imported.createdByType).map(([type, count]) => {
      return { label: type, value: `${count}` };
    });
  });

  protected onFileSelected(files: File[]): void {
    this.file.set(files[0] ?? null);
    this.preview.set(null);
    this.result.set(null);
    this.error.set(null);
  }

  protected check(): void {
    this.send<ArchiveImportPreview>('api/import/archive/preview', (payload) => {
      this.preview.set(payload);
    });
  }

  protected import(): void {
    this.send<ArchiveImportResult>('api/import/archive', (payload) => {
      this.result.set(payload);
      this.preview.set(null);
      this.file.set(null);
    });
  }

  private send<T>(url: string, onSuccess: (payload: T) => void): void {
    const chosen = this.file();

    if (!chosen) return;

    const form = new FormData();

    form.append('file', chosen, chosen.name);
    this.isBusy.set(true);
    this.error.set(null);

    this.http
      .post<ClientResponse<T>>(`${url}?${this.query()}`, form)
      .subscribe({
        next: (response) => {
          this.isBusy.set(false);

          if (!response.payload) {
            this.error.set(response.message ?? this.failedMessage);

            return;
          }

          onSuccess(response.payload);
        },
        error: (response: { error?: ClientResponse<T> }) => {
          this.isBusy.set(false);
          this.error.set(response.error?.message ?? this.failedMessage);
        },
      });
  }

  private query(): string {
    const params = new URLSearchParams({ mode: this.mode() });

    if (this.mode() === 'clone') {
      params.set('targetSlug', this.targetSlug().trim());
    }

    if (this.inviteUnmatchedMembers()) {
      params.set('inviteUnmatchedMembers', 'true');
    }

    return params.toString();
  }

  private readonly failedMessage = $localize`:Shown when an archive could not be read or imported:The archive could not be read.`;
}
