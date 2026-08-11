import { Component, computed, inject, input, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSONS } from '@core/auth/permissions';
import { WorkspaceFileViewModel } from '@core/models/view-models/workspace-file-view-model';
import { taskFilesResource } from '@core/resources/workspace-file.resource';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import {
  TaskFileUploadItem,
  TaskFileUploadService,
} from '@core/services/task-file-upload.service';
import { WorkspaceFilesService } from '@core/services/workspace-files.service';
import {
  LucideDownload,
  LucideRotateCcw,
  LucideTrash2,
  LucideX,
} from '@lucide/angular';
import { FileDropzoneComponent } from '@static/components/file-dropzone/file-dropzone.component';
import { FileTypeIconComponent } from '@static/components/file-type-icon/file-type-icon.component';
import { FileSizePipe } from '@static/pipes/file-size.pipe';

@Component({
  selector: 'app-task-detail-files',
  imports: [
    FileDropzoneComponent,
    FileSizePipe,
    FileTypeIconComponent,
    LucideDownload,
    LucideRotateCcw,
    LucideTrash2,
    LucideX,
  ],
  providers: [TaskFileUploadService],
  template: `
    <section class="mt-6" aria-labelledby="task-files-heading">
      <div class="mb-2 flex items-center justify-between">
        <h3 id="task-files-heading" class="font-medium">
          <span i18n="Section heading for files attached to a task">Files</span>
        </h3>
        @if (uploading()) {
          <button
            type="button"
            class="text-muted inline-flex items-center gap-1 text-xs hover:underline"
            (click)="cancelUploads()">
            <svg lucideX class="h-3 w-3"></svg>
            <span i18n="Button that stops in-progress file uploads">
              Cancel uploads
            </span>
          </button>
        } @else if (loading()) {
          <span
            class="text-muted text-xs"
            i18n="Shown while a task's files are loading">
            Loading…
          </span>
        }
      </div>

      @if (canUpload()) {
        <app-file-dropzone
          [disabled]="uploading()"
          [maxBytes]="maxUploadBytes()"
          (filesSelected)="upload($event)" />
      }

      <div class="mt-3 space-y-2" aria-live="polite">
        @for (upload of uploads(); track upload.id) {
          <div class="bg-card rounded p-2 text-sm">
            <div class="flex items-center justify-between gap-2">
              <span class="truncate">{{ upload.name }}</span>
              @if (upload.error) {
                <span class="text-destructive ml-auto">{{ upload.error }}</span>
                <button
                  type="button"
                  class="text-primary inline-flex items-center gap-1 hover:underline disabled:opacity-50"
                  [disabled]="uploading()"
                  (click)="retry(upload)">
                  <svg lucideRotateCcw class="h-3 w-3"></svg>
                  <span i18n="Button that retries a failed file upload">
                    Retry
                  </span>
                </button>
              } @else {
                <span>{{ upload.progress }}%</span>
              }
            </div>
            <div class="bg-muted mt-1 h-1 overflow-hidden rounded">
              <div
                class="bg-primary h-full"
                [style.width.%]="upload.progress"></div>
            </div>
          </div>
        }
        @for (file of files(); track file.id) {
          <div class="border-border flex items-center gap-3 rounded border p-2">
            <app-file-type-icon size="small" [group]="file.contentTypeGroup" />
            <div class="min-w-0 flex-1">
              <a
                class="block truncate font-medium hover:underline"
                [href]="file.contentUrl"
                target="_blank"
                rel="noopener">
                {{ file.originalName }}
              </a>
              <span class="text-muted text-xs">
                {{ file.sizeBytes | fileSize }} ·
                {{ file.uploadedByDisplayName || 'Unknown user' }}
              </span>
            </div>
            <a
              class="rounded p-2"
              [href]="file.contentUrl"
              i18n-aria-label="
                Accessible label for the button that downloads an attached file
              "
              aria-label="Download file">
              <svg lucideDownload class="h-4 w-4"></svg>
            </a>
            @if (file.canDelete) {
              <button
                type="button"
                class="text-destructive rounded p-2"
                (click)="remove(file)"
                i18n-aria-label="
                  Accessible label for the button that deletes an attached file
                "
                aria-label="Delete file">
                <svg lucideTrash2 class="h-4 w-4"></svg>
              </button>
            }
          </div>
        } @empty {
          @if (!loading()) {
            <p class="text-muted py-3 text-sm">
              <span i18n="Empty state when a task has no attached files">
                No files have been added to this task.
              </span>
            </p>
          }
        }
        @if (error()) {
          <p class="text-destructive text-sm">{{ error() }}</p>
        }
      </div>
    </section>
  `,
})
export class TaskDetailFilesComponent {
  readonly systemId = input.required<string>();
  private readonly filesResource = taskFilesResource(this.systemId);
  private readonly uploadService = inject(TaskFileUploadService);
  private readonly service = inject(WorkspaceFilesService);
  private readonly mutationError = signal('');

  readonly maxUploadBytes = inject(CurrentWorkspaceService).maxUploadBytes;
  readonly uploads = this.uploadService.uploads;
  readonly uploading = this.uploadService.uploading;
  readonly loading = this.filesResource.isLoading;

  readonly files = computed(() => {
    const fetched = this.filesResource.value()?.payload ?? [];
    const uploaded = this.uploadService
      .uploadedFiles()
      .filter((item) => item.systemId === this.systemId())
      .map((item) => item.file);
    const uploadedIds = new Set(uploaded.map((file) => file.id));

    return [
      ...uploaded,
      ...fetched.filter((file) => !uploadedIds.has(file.id)),
    ];
  });

  readonly error = computed(() => {
    if (this.mutationError()) {
      return this.mutationError();
    }

    return this.filesResource.error() ? 'Files could not be loaded.' : '';
  });

  readonly canUpload = hasPermission(PERMISSONS.files.upload);

  upload(files: File[]) {
    this.uploadService.upload(this.systemId(), files);
  }

  retry(upload: TaskFileUploadItem) {
    this.uploadService.retry(this.systemId(), upload);
  }

  cancelUploads() {
    this.uploadService.cancel();
  }

  async remove(file: WorkspaceFileViewModel) {
    this.mutationError.set('');

    try {
      await this.service.deleteTaskFile(this.systemId(), file.id);
      this.uploadService.removeCompletedFile(file.id);
      this.filesResource.value.update((response) => {
        if (!response) {
          return response;
        }

        return {
          ...response,
          payload: response.payload?.filter((item) => item.id !== file.id),
        };
      });
    } catch {
      this.mutationError.set(`Could not delete ${file.originalName}.`);
    }
  }
}
