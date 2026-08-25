import { HttpClient } from '@angular/common/http';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { Router, RouterLink } from '@angular/router';
import { PERMISSIONS } from '@app/core/auth/permissions';
import { ClientResponse } from '@core/models/client-response';
import {
  ImportSessionProgressEvent,
  ImportSessionViewModel,
  ImportSourceKind,
  ImportStage,
} from '@core/models/view-models/import-session';
import {
  ExportFormat,
  ExportJobProgressEvent,
  ExportJobStatus,
  ExportJobViewModel,
} from '@core/models/view-models/export-job-view-model';
import { ConfirmationService } from '@core/services/confirmation.service';
import { TransferJobSseService } from '@core/sse/transfer-job-sse.service';
import { CoalescedAction } from '@core/util/coalesced-action';
import {
  LucideBan,
  LucideDatabase,
  LucideDownload,
  LucideFileArchive,
  LucideFileDown,
  LucideFileUp,
  LucidePlay,
  LucideTrash2,
  LucideUndo2,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import {
  FileTypeIconComponent,
  FileTypeIconGroup,
} from '@static/components/file-type-icon/file-type-icon.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelComponent } from '@static/components/panel.component';
import {
  TabGroupComponent,
  TabItem,
} from '@static/components/tab-group/tab-group.component';
import { FileSizePipe } from '@static/pipes/file-size.pipe';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { first } from 'rxjs';

type DataTab = 'exports' | 'imports';

const ProgressReloadWindowMs = 1500;

const ResumableStages = [
  ImportStage.uploaded,
  ImportStage.inspected,
  ImportStage.mapped,
  ImportStage.previewed,
  ImportStage.failed,
];

@Component({
  selector: 'app-data-transfer-view',
  imports: [
    BadgeComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    EmptyStateComponent,
    FileSizePipe,
    FileTypeIconComponent,
    FlatButtonComponent,
    IconButtonComponent,
    LucideBan,
    LucideDatabase,
    LucideDownload,
    LucideFileArchive,
    LucideFileDown,
    LucideFileUp,
    LucidePlay,
    LucideTrash2,
    LucideUndo2,
    PageContainerComponent,
    PageHeaderComponent,
    PanelComponent,
    PanelHeaderComponent,
    PrettyDatePipe,
    RouterLink,
    TabGroupComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the workspace import and export page"
        title="Data" />

      @if (tabs().length > 1) {
        <app-tab-group
          class="mb-6 block"
          [tabs]="tabs()"
          [value]="selectedTab()"
          (changed)="onTabChanged($event)" />
      }

      @if (selectedTab() === 'exports') {
        <app-panel>
          <app-panel-header
            [icon]="exportIcon"
            i18n-heading="Heading of the export history panel"
            heading="Exports"
            i18n-description="Explains how long an export stays downloadable"
            description="Exports are kept for seven days, then removed from workspace storage.">
            @if (canExport()) {
              <a panelHeaderActions app-flat-button [routerLink]="['export']">
                <svg lucideFileDown class="h-4 w-4"></svg>
                <span i18n="Button that opens the guided export builder">
                  Create Export
                </span>
              </a>
            }
          </app-panel-header>

          <app-datatable
            [rounded]="false"
            i18n-errorMessage="Shown when the export history fails to load"
            errorMessage="Exports could not be loaded."
            i18n-itemLabel="Names the rows of the export history table"
            itemLabel="exports"
            containerClass="border-0 overflow-x-auto"
            tableClass="min-w-[840px] table-fixed"
            [data]="exportData">
            <ng-template appDatatableCell="status" let-job>
              <app-badge [color]="statusColor(job)">
                {{ statusLabel(job) }}
              </app-badge>
            </ng-template>

            <ng-template appDatatableCell="name" let-job>
              <div class="flex min-w-0 items-center gap-3">
                <app-file-type-icon
                  size="small"
                  [group]="fileIconGroup(job.format)" />

                <div class="min-w-0">
                  <span class="block truncate font-medium">
                    {{ job.name ?? job.fileName ?? job.recordType }}
                  </span>
                  @if (job.error) {
                    <span class="text-warn block truncate text-xs">
                      {{ job.error }}
                    </span>
                  } @else if (isRunning(job)) {
                    <span class="text-muted block truncate text-xs">
                      {{ job.progressMessage }} · {{ job.progressPercent }}%
                    </span>
                  }
                </div>
              </div>
            </ng-template>

            <ng-template appDatatableCell="format" let-job>
              {{ formatLabel(job) }}
            </ng-template>

            <ng-template appDatatableCell="rowCount" let-job>
              {{ job.rowCount ?? '—' }}
            </ng-template>

            <ng-template appDatatableCell="sizeBytes" let-job>
              {{ job.sizeBytes ? (job.sizeBytes | fileSize) : '—' }}
            </ng-template>

            <ng-template appDatatableCell="createdAt" let-job>
              {{ toDate(job.createdAt) | prettyDate }}
            </ng-template>

            <ng-template appDatatableCell="actions" let-job>
              <div class="flex items-center justify-end gap-1">
                @if (job.hasArtefact) {
                  <button
                    app-icon-button
                    type="button"
                    i18n-aria-label="
                      Accessible label for the export download button
                    "
                    aria-label="Download this export"
                    (click)="download(job)">
                    <svg lucideDownload class="h-4 w-4"></svg>
                  </button>
                }
                @if (isCancellable(job)) {
                  <button
                    app-icon-button
                    type="button"
                    i18n-aria-label="
                      Accessible label for the export cancel button
                    "
                    aria-label="Cancel this export"
                    (click)="cancel(job)">
                    <svg lucideBan class="h-4 w-4"></svg>
                  </button>
                } @else {
                  <button
                    app-icon-button
                    type="button"
                    i18n-aria-label="
                      Accessible label for the export delete button
                    "
                    aria-label="Delete this export"
                    (click)="deleteExport(job)">
                    <svg lucideTrash2 class="h-4 w-4"></svg>
                  </button>
                }
              </div>
            </ng-template>

            <ng-template appDatatableEmpty>
              <app-empty-state
                compact
                i18n-title="
                  Heading when a workspace has never exported anything
                "
                title="No exports yet"
                i18n-description="Explains when export rows start appearing"
                description="Exports you run appear here with a download link.">
                <svg emptyStateIcon lucideDatabase class="h-8 w-8"></svg>
              </app-empty-state>
            </ng-template>
          </app-datatable>
        </app-panel>
      } @else {
        <app-panel>
          <app-panel-header
            [icon]="importIcon"
            i18n-heading="Heading of the import history panel"
            heading="Imports"
            i18n-description="Explains how long an import stays undoable"
            description="A committed import can be undone until its session expires.">
            <div panelHeaderActions class="flex items-center gap-2">
              @if (canImportArchive()) {
                <a app-flat-button color="neutral" [routerLink]="['archive']">
                  <svg lucideFileArchive class="h-4 w-4"></svg>
                  <span i18n="Button that opens the archive import page">
                    Import Archive
                  </span>
                </a>
              }
              @if (canImport()) {
                <a app-flat-button [routerLink]="['import']">
                  <svg lucideFileUp class="h-4 w-4"></svg>
                  <span i18n="Button that opens the guided import builder">
                    Create Import
                  </span>
                </a>
              }
            </div>
          </app-panel-header>

          <app-datatable
            [rounded]="false"
            i18n-errorMessage="Shown when the import history fails to load"
            errorMessage="Imports could not be loaded."
            i18n-itemLabel="Names the rows of the import history table"
            itemLabel="imports"
            containerClass="border-0 overflow-x-auto"
            tableClass="min-w-[840px] table-fixed"
            [data]="importData">
            <ng-template appDatatableCell="stage" let-session>
              <app-badge [color]="stageColor(session)">
                {{ stageLabel(session) }}
              </app-badge>
            </ng-template>

            <ng-template appDatatableCell="originalName" let-session>
              <div class="flex min-w-0 items-center gap-3">
                <app-file-type-icon
                  size="small"
                  [group]="fileIconGroup(session.sourceKind)" />

                <div class="min-w-0">
                  <span class="block truncate font-medium">
                    {{ session.originalName }}
                  </span>
                  @if (session.error) {
                    <span class="text-warn block truncate text-xs">
                      {{ session.error }}
                    </span>
                  } @else if (isImportRunning(session)) {
                    <span class="text-muted block truncate text-xs">
                      {{ session.progressMessage }} ·
                      {{ session.progressPercent }}%
                    </span>
                  }
                </div>
              </div>
            </ng-template>

            <ng-template appDatatableCell="targetRecordType" let-session>
              {{ session.targetRecordType }}
            </ng-template>

            <ng-template appDatatableCell="counts" let-session>
              {{ countsLabel(session) }}
            </ng-template>

            <ng-template appDatatableCell="createdBy" let-session>
              {{ session.createdByDisplayName ?? '—' }}
            </ng-template>

            <ng-template appDatatableCell="createdAt" let-session>
              {{ toDate(session.createdAt) | prettyDate }}
            </ng-template>

            <ng-template appDatatableCell="actions" let-session>
              <div class="flex items-center justify-end gap-1">
                @if (canResume(session)) {
                  <button
                    app-icon-button
                    (click)="router.navigate(['import', session.publicId])"
                    i18n-aria-label="
                      Accessible label for the import resume link
                    "
                    aria-label="Resume this import">
                    <svg lucidePlay class="h-4 w-4"></svg>
                  </button>
                }

                @if (session.canUndo) {
                  <button
                    app-icon-button
                    type="button"
                    [disabled]="undoing() === session.publicId"
                    i18n-aria-label="
                      Accessible label for the import undo button
                    "
                    aria-label="Undo this import"
                    (click)="undo(session)">
                    <svg lucideUndo2 class="h-4 w-4"></svg>
                  </button>
                }

                @if (canDeleteImport(session)) {
                  <button
                    app-icon-button
                    type="button"
                    i18n-aria-label="
                      Accessible label for the import delete button
                    "
                    aria-label="Delete this import"
                    (click)="deleteImport(session)">
                    <svg lucideTrash2 class="h-4 w-4"></svg>
                  </button>
                }
              </div>
            </ng-template>

            <ng-template appDatatableEmpty>
              <app-empty-state
                compact
                i18n-title="
                  Heading when a workspace has never imported anything
                "
                title="No imports yet"
                i18n-description="Explains when import rows start appearing"
                description="Files you import appear here with their results.">
                <svg emptyStateIcon lucideDatabase class="h-8 w-8"></svg>
              </app-empty-state>
            </ng-template>
          </app-datatable>
        </app-panel>
      }
    </app-page-container>
  `,
})
export class DataTransferViewComponent {
  readonly http = inject(HttpClient);
  readonly transferEvents = inject(TransferJobSseService);
  readonly confirmation = inject(ConfirmationService);
  readonly destroyRef = inject(DestroyRef);
  readonly router = inject(Router);

  protected readonly exportIcon = LucideFileDown;
  protected readonly importIcon = LucideFileUp;

  protected readonly canExport = hasPermission(PERMISSIONS.tasks.export);

  protected readonly canImport = hasPermission(PERMISSIONS.tasks.import);

  protected readonly canImportArchive = hasPermission(
    PERMISSIONS.data.importArchive
  );

  protected readonly undoing = signal<string | null>(null);

  protected readonly tabs = computed<TabItem[]>(() => {
    const tabs: TabItem[] = [
      {
        label: $localize`:Tab that shows the workspace export history:Exports`,
        value: 'exports',
      },
    ];

    if (this.canImport()) {
      tabs.push({
        label: $localize`:Tab that shows the workspace import history:Imports`,
        value: 'imports',
      });
    }

    return tabs;
  });

  private readonly requestedTab = signal<DataTab>('exports');

  protected readonly selectedTab = computed<DataTab>(() => {
    const requested = this.requestedTab();

    if (requested === 'imports' && !this.canImport()) {
      return 'exports';
    }

    return requested;
  });

  protected onTabChanged(value: string | number | null) {
    this.requestedTab.set(value === 'imports' ? 'imports' : 'exports');
  }

  private readonly reloadToken = signal(0);
  private readonly importReloadToken = signal(0);

  private readonly requestParams = computed(() => ({}));

  protected readonly exportData: DatatableDataSource<ExportJobViewModel> = {
    key: 'workspace-export-jobs',
    columns: [
      {
        id: 'status',
        header: $localize`:Column heading for the state of an export:Status`,
        cellClass: 'truncate',
        widthClass: 'w-28',
      },
      {
        id: 'name',
        header: $localize`:Column heading for what an export contains:Export`,
        cellClass: 'truncate',
      },
      {
        id: 'format',
        header: $localize`:Column heading for the file format of an export:Format`,
        cellClass: 'truncate',
        widthClass: 'w-24',
      },
      {
        id: 'rowCount',
        header: $localize`:Column heading for how many rows an export produced:Rows`,
        cellClass: 'truncate',
        widthClass: 'w-24',
        align: 'end',
      },
      {
        id: 'sizeBytes',
        header: $localize`:Column heading for the size of an export file:Size`,
        cellClass: 'truncate',
        widthClass: 'w-24',
        align: 'end',
      },
      {
        id: 'createdAt',
        header: $localize`:Column heading for when an export was requested:Requested`,
        cellClass: 'truncate',
        widthClass: 'w-40',
      },
      {
        id: 'actions',
        header: '',
        widthClass: 'w-24',
        align: 'end',
      },
    ],
    resource: {
      url: 'api/export/jobs',
      params: this.requestParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, job: ExportJobViewModel) => job.publicId,
    reloadSignal: this.reloadToken,
  };

  protected readonly importData: DatatableDataSource<ImportSessionViewModel> = {
    key: 'workspace-import-sessions',
    columns: [
      {
        id: 'stage',
        header: $localize`:Column heading for the state of an import:Stage`,
        cellClass: 'truncate',
        widthClass: 'w-28',
      },
      {
        id: 'originalName',
        header: $localize`:Column heading for the file an import came from:File`,
        cellClass: 'truncate',
      },
      {
        id: 'targetRecordType',
        header: $localize`:Column heading for what kind of record an import creates:Type`,
        cellClass: 'truncate',
        widthClass: 'w-24',
      },
      {
        id: 'counts',
        header: $localize`:Column heading for the created, updated and skipped row counts of an import:Rows`,
        cellClass: 'truncate',
        widthClass: 'w-44',
      },
      {
        id: 'createdBy',
        header: $localize`:Column heading for who ran an import:By`,
        cellClass: 'truncate',
        widthClass: 'w-36',
      },
      {
        id: 'createdAt',
        header: $localize`:Column heading for when an import was uploaded:Uploaded`,
        cellClass: 'truncate',
        widthClass: 'w-40',
      },
      {
        id: 'actions',
        header: '',
        widthClass: 'w-20',
        align: 'end',
      },
    ],
    resource: {
      url: 'api/import/sessions',
      params: this.requestParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, session: ImportSessionViewModel) => session.publicId,
    reloadSignal: this.importReloadToken,
  };

  private readonly exportReload = new CoalescedAction(
    () => this.reload(),
    ProgressReloadWindowMs
  );

  private readonly importReload = new CoalescedAction(
    () => this.reloadImports(),
    ProgressReloadWindowMs
  );

  constructor() {
    this.transferEvents.connect({
      onExport: (progress) => this.onExportProgress(progress),
      onImport: (progress) => this.onImportProgress(progress),
    });

    this.destroyRef.onDestroy(() => {
      this.transferEvents.disconnect();
      this.exportReload.cancel();
      this.importReload.cancel();
    });
  }

  private onExportProgress(progress: ExportJobProgressEvent): void {
    if (this.isRunningStatus(progress.status)) {
      this.exportReload.schedule();

      return;
    }

    this.exportReload.now();
  }

  private onImportProgress(progress: ImportSessionProgressEvent): void {
    if (progress.stage === ImportStage.committing) {
      this.importReload.schedule();

      return;
    }

    this.importReload.now();
  }

  private isRunningStatus(status: ExportJobStatus): boolean {
    return (
      status === ExportJobStatus.pending || status === ExportJobStatus.running
    );
  }

  protected isRunning(job: ExportJobViewModel): boolean {
    return this.isRunningStatus(job.status);
  }

  protected isCancellable(job: ExportJobViewModel): boolean {
    return this.isRunning(job);
  }

  protected statusLabel(job: ExportJobViewModel): string {
    switch (job.status) {
      case ExportJobStatus.pending:
        return $localize`:An export that has not started yet:Queued`;
      case ExportJobStatus.running:
        return $localize`:An export that is being produced:Running`;
      case ExportJobStatus.succeeded:
        return $localize`:An export that finished and can be downloaded:Ready`;
      case ExportJobStatus.failed:
        return $localize`:An export that did not finish:Failed`;
      case ExportJobStatus.cancelled:
        return $localize`:An export a member stopped:Cancelled`;
      case ExportJobStatus.expired:
        return $localize`:An export whose file has been removed:Expired`;
    }
  }

  protected statusColor(
    job: ExportJobViewModel
  ): 'success' | 'warn' | 'info' | 'pending' | 'neutral' {
    switch (job.status) {
      case ExportJobStatus.succeeded:
        return 'success';
      case ExportJobStatus.failed:
        return 'warn';
      case ExportJobStatus.running:
        return 'info';
      case ExportJobStatus.pending:
        return 'pending';
      default:
        return 'neutral';
    }
  }

  protected isImportRunning(session: ImportSessionViewModel): boolean {
    return session.stage === ImportStage.committing;
  }

  protected canResume(session: ImportSessionViewModel): boolean {
    return ResumableStages.includes(session.stage);
  }

  protected canDeleteImport(session: ImportSessionViewModel): boolean {
    return session.stage !== ImportStage.committing;
  }

  protected stageLabel(session: ImportSessionViewModel): string {
    switch (session.stage) {
      case ImportStage.uploaded:
        return $localize`:An import whose file has been uploaded but not read:Uploaded`;
      case ImportStage.inspected:
        return $localize`:An import whose file has been read:Inspected`;
      case ImportStage.mapped:
        return $localize`:An import whose columns have been mapped:Mapped`;
      case ImportStage.previewed:
        return $localize`:An import that has been dry-run:Previewed`;
      case ImportStage.committing:
        return $localize`:An import that is being applied:Running`;
      case ImportStage.committed:
        return $localize`:An import that has been applied:Committed`;
      case ImportStage.failed:
        return $localize`:An import that did not finish:Failed`;
      case ImportStage.undone:
        return $localize`:An import that has been rolled back:Undone`;
      case ImportStage.abandoned:
        return $localize`:An import nobody finished configuring:Abandoned`;
    }
  }

  protected stageColor(
    session: ImportSessionViewModel
  ): 'success' | 'warn' | 'info' | 'pending' | 'neutral' {
    switch (session.stage) {
      case ImportStage.committed:
        return 'success';
      case ImportStage.failed:
        return 'warn';
      case ImportStage.committing:
        return 'info';
      case ImportStage.uploaded:
      case ImportStage.inspected:
      case ImportStage.mapped:
      case ImportStage.previewed:
        return 'pending';
      default:
        return 'neutral';
    }
  }

  protected countsLabel(session: ImportSessionViewModel): string {
    if (session.stage < ImportStage.previewed) {
      return '—';
    }

    return $localize`:Summary of how many rows an import created, updated and skipped:${session.created}:created: created · ${session.updated}:updated: updated · ${session.skipped}:skipped: skipped`;
  }

  // Export formats and import source kinds share the same numeric values, so
  // one mapping covers both tables.
  protected fileIconGroup(
    format: ExportFormat | ImportSourceKind
  ): FileTypeIconGroup {
    switch (format) {
      case ExportFormat.csv:
      case ExportFormat.tsv:
      case ExportFormat.xlsx:
        return 'spreadsheet';
      case ExportFormat.json:
      case ExportFormat.ndjson:
        return 'data';
      case ExportFormat.archive:
        return 'archive';
      default:
        return 'other';
    }
  }

  protected formatLabel(job: ExportJobViewModel): string {
    return ExportFormat[job.format].toUpperCase();
  }

  protected toDate(value: string): Date {
    return new Date(value);
  }

  protected download(job: ExportJobViewModel): void {
    this.http
      .get<ClientResponse<string>>(`api/export/jobs/${job.publicId}/download`)
      .subscribe({
        next: (response) => {
          const url = response.payload;

          if (!url) return;

          window.open(url, '_blank', 'noopener');
        },
      });
  }

  protected cancel(job: ExportJobViewModel): void {
    this.http
      .post<ClientResponse<ExportJobViewModel>>(
        `api/export/jobs/${job.publicId}/cancel`,
        {}
      )
      .subscribe(() => this.reload());
  }

  protected undo(session: ImportSessionViewModel): void {
    this.undoing.set(session.publicId);

    this.http
      .post<ClientResponse<ImportSessionViewModel>>(
        `api/import/sessions/${session.publicId}/undo`,
        {}
      )
      .subscribe({
        next: () => {
          this.undoing.set(null);
          this.reloadImports();
        },
        error: () => this.undoing.set(null),
      });
  }

  protected deleteExport(job: ExportJobViewModel): void {
    const name = job.name ?? job.fileName ?? job.recordType;

    this.confirmation
      .open({
        title: $localize`:Title of the dialog that confirms deleting an export:Delete this export?`,
        message: $localize`:Warns that deleting an export also removes its file:${name}:name: and its file are removed for good.`,
        acceptLabel: $localize`:Confirms deleting a record:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(first())
      .subscribe((confirmed) => {
        if (!confirmed) return;

        this.http
          .delete<ClientResponse>(`api/export/jobs/${job.publicId}`)
          .subscribe(() => this.reload());
      });
  }

  protected deleteImport(session: ImportSessionViewModel): void {
    const message = session.canUndo
      ? $localize`:Warns that deleting a committed import gives up undo:${session.originalName}:name: is removed and this import can no longer be undone.`
      : $localize`:Warns that deleting an import also removes its file:${session.originalName}:name: and its uploaded file are removed for good.`;

    this.confirmation
      .open({
        title: $localize`:Title of the dialog that confirms deleting an import:Delete this import?`,
        message,
        acceptLabel: $localize`:Confirms deleting a record:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(first())
      .subscribe((confirmed) => {
        if (!confirmed) return;

        this.http
          .delete<ClientResponse>(`api/import/sessions/${session.publicId}`)
          .subscribe(() => this.reloadImports());
      });
  }

  private reload(): void {
    this.reloadToken.update((token) => token + 1);
  }

  private reloadImports(): void {
    this.importReloadToken.update((token) => token + 1);
  }
}
