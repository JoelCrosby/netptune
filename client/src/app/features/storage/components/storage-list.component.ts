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
import { FileSizePipe } from '../pipes/file-size.pipe';
import {
  StorageFiltersComponent,
  StorageSort,
} from './storage-filters.component';

@Component({
  selector: 'app-storage-list',
  imports: [
    AvatarComponent,
    ButtonLinkComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    DatePipe,
    EmptyStateComponent,
    FileSizePipe,
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
      (sortChange)="setSort($event)" />

    <app-datatable
      containerClass="overflow-x-auto"
      tableClass="min-w-190 table-fixed"
      emptyCellClass="py-12"
      itemLabel="files"
      [data]="data"
      [sort]="sort()"
      (sortChange)="onSortChange($event)">
      <ng-template appDatatableCell="name" let-file>
        <a
          class="block truncate font-medium hover:underline"
          [href]="file.contentUrl"
          target="_blank"
          rel="noopener">
          {{ file.originalName }}
        </a>
        <span class="text-muted text-xs">{{ file.contentType }}</span>
      </ng-template>

      <ng-template appDatatableCell="origin" let-file>
        @if (file.taskSystemId) {
          <a
            class="flex items-center gap-2 hover:underline"
            [routerLink]="['../tasks', file.taskSystemId]">
            <app-task-scope-id [id]="file.taskSystemId" />
            <span class="truncate">{{ file.taskName }}</span>
          </a>
        } @else {
          <span class="text-muted">Inline media</span>
        }
      </ng-template>

      <ng-template appDatatableCell="uploader" let-file>
        @if (file.uploadedByDisplayName) {
          <div class="flex items-center gap-2">
            <app-avatar
              [name]="file.uploadedByDisplayName"
              [imageUrl]="file.uploadedByPictureUrl"
              [isServiceAccount]="file.uploadedByIsServiceAccount ?? false" />
            <span class="truncate">{{ file.uploadedByDisplayName }}</span>
          </div>
        } @else {
          <span class="text-muted">Unknown</span>
        }
      </ng-template>

      <ng-template appDatatableCell="createdAt" let-file>
        <span [appTooltip]="file.createdAt | date: 'medium'">
          {{ file.createdAt | date: 'mediumDate' }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="sizeBytes" let-file>
        <span class="block text-right tabular-nums">
          {{ file.sizeBytes | fileSize }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="actions" let-file>
        <div class="flex justify-end gap-1">
          <a
            app-button-link
            class="h-8 min-h-8 w-8 px-0"
            [href]="file.contentUrl"
            target="_blank"
            rel="noopener"
            aria-label="Open file">
            <svg lucideExternalLink class="h-4 w-4"></svg>
          </a>
          <a
            app-button-link
            class="h-8 min-h-8 w-8 px-0"
            [href]="file.contentUrl"
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
              aria-label="Delete file">
              <svg lucideTrash2 class="h-4 w-4"></svg>
            </button>
          }
        </div>
      </ng-template>

      <app-empty-state
        appDatatableEmpty
        compact
        title="No files match the current filters."
        description="Try changing or clearing the filters.">
        <svg emptyStateIcon lucideFiles class="h-8 w-8"></svg>
      </app-empty-state>
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
  readonly sort = signal<DatatableSort | null>(this.initialSort());
  readonly deletingId = signal<number | null>(null);

  readonly filter = computed<WorkspaceFileFilter>(() => ({
    query: this.query(),
    purpose: this.purpose(),
    sortBy: this.storageSort(),
    sortDirection: this.sort()?.sortDirection,
  }));

  private readonly resourceParams = computed<Params>(() => ({
    query: this.query(),
    purpose: this.purpose(),
  }));

  readonly data: DatatableDataSource<WorkspaceFileViewModel> = {
    key: 'workspace-storage-files',
    columns: [
      {
        id: 'name',
        header: 'File',
        sortable: true,
        widthClass: 'w-72',
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
