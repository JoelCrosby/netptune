import { HttpEventType } from '@angular/common/http';
import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { WorkspaceFileViewModel } from '@core/models/view-models/workspace-file-view-model';
import {
  catchError,
  from,
  map,
  mergeMap,
  of,
  Subscription,
} from 'rxjs';
import { WorkspaceFilesService } from './workspace-files.service';

export interface TaskFileUploadItem {
  id: number;
  file: File;
  name: string;
  progress: number;
  error?: string;
}

export interface UploadedTaskFile {
  systemId: string;
  file: WorkspaceFileViewModel;
}

@Injectable()
export class TaskFileUploadService {
  private readonly workspaceFiles = inject(WorkspaceFilesService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly uploadItems = signal<TaskFileUploadItem[]>([]);
  private readonly completedFiles = signal<UploadedTaskFile[]>([]);
  private uploadSubscription?: Subscription;
  private nextUploadId = 0;

  readonly uploads = this.uploadItems.asReadonly();
  readonly uploadedFiles = this.completedFiles.asReadonly();
  readonly uploading = computed(() => {
    return this.uploadItems().some(
      (item) => item.progress < 100 && !item.error
    );
  });

  upload(systemId: string, files: File[]) {
    const uploads = files.map((file) => ({
      id: ++this.nextUploadId,
      file,
      name: file.name,
      progress: 0,
    }));

    this.uploadItems.set(uploads);
    this.startUploads(systemId, uploads);
  }

  retry(systemId: string, upload: TaskFileUploadItem) {
    const retry = { ...upload, progress: 0, error: undefined };

    this.updateUpload(upload.id, retry);
    this.startUploads(systemId, [retry]);
  }

  cancel() {
    this.uploadSubscription?.unsubscribe();
    this.uploadSubscription = undefined;
    this.uploadItems.update((items) =>
      items.map((item) =>
        item.progress < 100 && !item.error
          ? { ...item, error: 'Cancelled' }
          : item
      )
    );
  }

  removeCompletedFile(fileId: number) {
    this.completedFiles.update((items) =>
      items.filter((item) => item.file.id !== fileId)
    );
  }

  private startUploads(systemId: string, uploads: TaskFileUploadItem[]) {
    this.uploadSubscription = from(uploads)
      .pipe(
        mergeMap(
          (upload) =>
            this.workspaceFiles.uploadTaskFile(systemId, upload.file).pipe(
              map((event) => ({ upload, event, error: '' })),
              catchError(() =>
                of({ upload, event: null, error: 'Upload failed' })
              )
            ),
          3
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ upload, event, error }) => {
          if (error) {
            this.updateUpload(upload.id, { error });

            return;
          }

          if (!event) {
            return;
          }

          if (event.type === HttpEventType.UploadProgress) {
            const progress = Math.round(
              (event.loaded * 100) / (event.total || event.loaded)
            );
            this.updateUpload(upload.id, { progress });
          } else if (event.type === HttpEventType.Response) {
            const result = event.body?.payload;

            this.updateUpload(upload.id, {
              progress: 100,
              error: result?.isSuccess ? undefined : result?.error,
            });

            if (result?.file) {
              const file = result.file;

              this.completedFiles.update((items) => [
                { systemId, file },
                ...items,
              ]);
            }
          }
        },
        complete: () => {
          this.uploadSubscription = undefined;
        },
      });
  }

  private updateUpload(id: number, patch: Partial<TaskFileUploadItem>) {
    this.uploadItems.update((items) =>
      items.map((item) => (item.id === id ? { ...item, ...patch } : item))
    );
  }
}
