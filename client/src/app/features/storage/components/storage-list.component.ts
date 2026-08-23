import { DatePipe } from '@angular/common';
import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import {
  WorkspaceFileContentTypeGroup,
  WorkspaceFileFilter,
  WorkspaceFilePurpose,
  WorkspaceFileViewModel,
} from '@core/models/view-models/workspace-file-view-model';
import { WorkspaceFilesService } from '@core/services/workspace-files.service';
import {
  LucideDownload,
  LucideExternalLink,
  LucideFiles,
  LucideTrash2,
} from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { ButtonLinkComponent } from '@static/components/button/button-link.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import {
  DatatableDataSource,
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { finalize } from 'rxjs';
import { FileTypeIconComponent } from '@static/components/file-type-icon/file-type-icon.component';
import { FileSizePipe } from '@static/pipes/file-size.pipe';
import {
  StorageFiltersComponent,
  StorageSort,
} from './storage-filters.component';

const contentTypeGroups: readonly WorkspaceFileContentTypeGroup[] = [
  'image',
  'document',
  'archive',
  'other',
];

@Component({
  selector: 'app-storage-list',
  imports: [
    AvatarComponent,
    BadgeComponent,
    ButtonLinkComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    DatePipe,
    EmptyStateComponent,
    FileSizePipe,
    FileTypeIconComponent,
    IconButtonComponent,
    LucideDownload,
    LucideExternalLink,
    LucideFiles,
    LucideTrash2,
    RouterLink,
    StorageFiltersComponent,
    TaskScopeIdComponent,
    TooltipDirective,
  ],
  template: `
    <app-storage-filters
      [filter]="filter()"
      (queryChange)="setQuery($event)"
      (purposeChange)="setPurpose($event)"
      (contentTypeChange)="setContentTypeGroup($event)"
      (sortChange)="setSort($event)"
      (resetFilters)="clearFilters()" />

    <app-datatable
      i18n-errorMessage="Shown when the file list fails to load"
      errorMessage="Files could not be loaded."
      containerClass="overflow-x-auto rounded-lg shadow-sm"
      tableClass="min-w-200 table-fixed"
      headerClass="bg-card-header text-muted uppercase"
      rowClass="group"
      emptyCellClass="py-12"
      i18n-itemLabel="Plural noun for files, used in the selection summary"
      itemLabel="files"
      [data]="data"
      [sort]="sort()"
      (sortChange)="onSortChange($event)">
      <ng-template appDatatableCell="name" let-file>
        <div class="flex min-w-0 items-center gap-3">
          <app-file-type-icon [group]="file.contentTypeGroup" />

          <div class="min-w-0">
            <a
              class="hover:text-primary block truncate font-medium transition-colors hover:underline"
              [href]="file.contentUrl"
              target="_blank"
              rel="noopener">
              {{ file.originalName }}
            </a>
            <span class="text-muted block truncate text-xs">
              {{ file.contentType }}
            </span>
          </div>
        </div>
      </ng-template>

      <ng-template appDatatableCell="origin" let-file>
        @if (file.taskSystemId) {
          <a
            class="hover:text-primary flex items-center gap-2 transition-colors hover:underline"
            [routerLink]="['../tasks', file.taskSystemId]">
            <app-task-scope-id [id]="file.taskSystemId" />
            <span class="truncate">{{ file.taskName }}</span>
          </a>
        } @else {
          <app-badge
            color="neutral"
            i18n="File origin: embedded in a description or comment">
            Inline media
          </app-badge>
        }
      </ng-template>

      <ng-template appDatatableCell="uploader" let-file>
        @if (file.uploadedByDisplayName) {
          <div class="flex min-w-0 items-center gap-2">
            <app-avatar
              [name]="file.uploadedByDisplayName"
              [imageUrl]="file.uploadedByPictureUrl"
              [isServiceAccount]="file.uploadedByIsServiceAccount ?? false" />
            <span class="truncate">{{ file.uploadedByDisplayName }}</span>
          </div>
        } @else {
          <span
            class="text-muted"
            i18n="Shown in place of a value that is not known">
            Unknown
          </span>
        }
      </ng-template>

      <ng-template appDatatableCell="createdAt" let-file>
        <span
          class="text-muted tabular-nums"
          [appTooltip]="file.createdAt | date: 'medium'">
          {{ file.createdAt | date: 'mediumDate' }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="sizeBytes" let-file>
        <span class="block text-right font-medium tabular-nums">
          {{ file.sizeBytes | fileSize }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="actions" let-file>
        <div
          class="text-muted flex justify-end gap-0.5 opacity-70 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
          <a
            app-button-link
            class="h-8 min-h-8 w-8 px-0"
            [href]="file.contentUrl"
            target="_blank"
            rel="noopener"
            i18n-aria-label="Accessible label for the button that opens a file"
            aria-label="Open file">
            <svg lucideExternalLink class="h-4 w-4"></svg>
          </a>
          <a
            app-button-link
            class="h-8 min-h-8 w-8 px-0"
            [href]="file.contentUrl"
            i18n-aria-label="
              Accessible label for the button that downloads a file
            "
            aria-label="Download file">
            <svg lucideDownload class="h-4 w-4"></svg>
          </a>
          @if (file.canDelete) {
            <button
              app-icon-button
              color="warn"
              class="h-8 w-8"
              type="button"
              [disabled]="deletingId() === file.id"
              (click)="deleteFile(file.id)"
              i18n-aria-label="
                Accessible label for the button that deletes a file
              "
              aria-label="Delete file">
              <svg lucideTrash2 class="h-4 w-4"></svg>
            </button>
          }
        </div>
      </ng-template>

      <ng-template appDatatableEmpty>
        <app-empty-state
          compact
          i18n-title="Empty state for the file list"
          title="No files match the current filters."
          i18n-description="Advice on the empty file list"
          description="Try changing or clearing the filters.">
          <svg emptyStateIcon lucideFiles class="h-8 w-8"></svg>
        </app-empty-state>
      </ng-template>
    </app-datatable>
  `,
})
export class StorageListComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly workspaceFiles = inject(WorkspaceFilesService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly datatable = viewChild.required(
    DatatableComponent<WorkspaceFileViewModel>
  );

  private readonly query = signal(
    this.route.snapshot.queryParamMap.get('query') ?? undefined
  );
  private readonly purpose = signal(this.initialPurpose());
  private readonly contentTypeGroup = signal(this.initialContentTypeGroup());
  readonly sort = signal<DatatableSort | null>(this.initialSort());
  readonly deletingId = signal<number | null>(null);

  readonly filter = computed<WorkspaceFileFilter>(() => ({
    query: this.query(),
    purpose: this.purpose(),
    contentTypeGroup: this.contentTypeGroup(),
    sortBy: this.storageSort(),
    sortDirection: this.sort()?.sortDirection,
  }));

  private readonly resourceParams = computed<Params>(() => ({
    query: this.query(),
    purpose: this.purpose(),
    contentTypeGroup: this.contentTypeGroup(),
  }));

  readonly data: DatatableDataSource<WorkspaceFileViewModel> = {
    key: 'workspace-storage-files',
    columns: [
      {
        id: 'name',
        header: 'File',
        sortable: true,
        widthClass: 'w-80',
      },
      { id: 'origin', header: 'Origin', widthClass: 'w-56' },
      { id: 'uploader', header: 'Uploader', widthClass: 'w-48' },
      {
        id: 'createdAt',
        header: 'Uploaded',
        sortable: true,
        widthClass: 'w-36',
      },
      {
        id: 'sizeBytes',
        header: 'Size',
        sortable: true,
        align: 'end',
        widthClass: 'w-28',
      },
      {
        id: 'actions',
        header: '',
        align: 'end',
        ariaLabel: 'Actions',
        widthClass: 'w-32',
      },
    ],
    resource: {
      url: 'api/storage/files',
      params: this.resourceParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, file: WorkspaceFileViewModel) => file.id,
  };

  readonly fileDeleted = output();

  constructor() {
    effect(() => {
      const filter = this.filter();
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: filter,
        replaceUrl: true,
      });
    });
  }

  setQuery(query: string) {
    this.query.set(query || undefined);
    this.datatable().goToPage(1);
  }

  setPurpose(purpose: WorkspaceFilePurpose | undefined) {
    this.purpose.set(purpose);
    this.datatable().goToPage(1);
  }

  setContentTypeGroup(group: WorkspaceFileContentTypeGroup | undefined) {
    this.contentTypeGroup.set(group);
    this.datatable().goToPage(1);
  }

  clearFilters() {
    this.query.set(undefined);
    this.purpose.set(undefined);
    this.contentTypeGroup.set(undefined);
    this.datatable().goToPage(1);
  }

  setSort(sortBy: StorageSort) {
    this.onSortChange({
      sortBy,
      sortDirection: this.sort()?.sortDirection ?? 'desc',
    });
  }

  onSortChange(sort: DatatableSort | null) {
    this.sort.set(sort);
    this.datatable().goToPage(1);
  }

  deleteFile(id: number) {
    this.deletingId.set(id);
    this.workspaceFiles
      .deleteFile(id)
      .pipe(
        finalize(() => this.deletingId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.datatable().reload();
          this.fileDeleted.emit();
        },
        error: () => undefined,
      });
  }

  private initialPurpose(): WorkspaceFilePurpose | undefined {
    const purpose = this.route.snapshot.queryParamMap.get('purpose');
    return purpose === null
      ? undefined
      : (Number(purpose) as WorkspaceFilePurpose);
  }

  private initialContentTypeGroup(): WorkspaceFileContentTypeGroup | undefined {
    const group = this.route.snapshot.queryParamMap.get('contentTypeGroup');
    const isKnownGroup = contentTypeGroups.includes(
      group as WorkspaceFileContentTypeGroup
    );

    return isKnownGroup ? (group as WorkspaceFileContentTypeGroup) : undefined;
  }

  private initialSort(): DatatableSort {
    const params = this.route.snapshot.queryParamMap;
    return {
      sortBy: params.get('sortBy') ?? 'createdAt',
      sortDirection: params.get('sortDirection') === 'asc' ? 'asc' : 'desc',
    };
  }

  private storageSort(): StorageSort | undefined {
    const sortBy = this.sort()?.sortBy;
    return sortBy === 'createdAt' || sortBy === 'name' || sortBy === 'sizeBytes'
      ? sortBy
      : undefined;
  }
}
