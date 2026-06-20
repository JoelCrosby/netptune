import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { TaskImportResult } from '@core/models/import/task-import-result';
import { importTasksSuccess } from '@core/store/tasks/tasks.actions';
import { ProjectTasksService } from '@core/store/tasks/tasks.service';
import { LucideCheckCircle, LucideTriangleAlert } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { FormFileUploadComponent } from '@static/components/form-file-upload/form-file-upload.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { firstValueFrom } from 'rxjs';

interface ImportTasksDialogData {
  boardIdentifier: string;
}

@Component({
  selector: 'app-import-tasks-dialog',
  imports: [
    DialogTitleComponent,
    DialogContentComponent,
    DialogActionsDirective,
    FormFileUploadComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    SpinnerComponent,
    LucideTriangleAlert,
    LucideCheckCircle,
  ],
  template: `
    <app-dialog-title>Import Tasks</app-dialog-title>

    <app-dialog-content>
      <div class="flex w-full min-w-100 flex-col gap-5 max-sm:min-w-0">
        <app-form-file-upload
          name="file"
          placeholder="Choose a CSV file"
          hint="CSV files only"
          accept=".csv,text/csv"
          [disabled]="loading()"
          [file]="selectedFile()"
          (fileSelected)="onFileSelected($event)" />

        @if (loading()) {
          <div class="flex items-center gap-3 text-sm">
            <app-spinner diameter="20px" />
            <span>Validating import file...</span>
          </div>
        }

        @if (response(); as result) {
          <section
            class="rounded border p-4"
            [class.border-green-500]="result.isSuccess"
            [class.bg-green-500/10]="result.isSuccess"
            [class.border-warn]="!result.isSuccess"
            [class.bg-warn/10]="!result.isSuccess">
            <div class="mb-3 flex items-center gap-2 font-medium">
              @if (result.isSuccess) {
                <svg lucideCheckCircle class="h-5 w-5 text-green-500"></svg>
                <span>Validation passed</span>
              } @else {
                <svg lucideTriangleAlert class="text-warn h-5 w-5"></svg>
                <span>Validation failed</span>
              }
            </div>

            <p class="text-sm">
              {{ validationMessage() }}
            </p>

            @if (missingHeaders().length) {
              <div class="mt-4">
                <h2 class="text-sm font-medium">Missing headers</h2>
                <ul class="mt-2 list-disc pl-5 text-sm">
                  @for (header of missingHeaders(); track header) {
                    <li>{{ header }}</li>
                  }
                </ul>
              </div>
            }

            @if (invalidHeaders().length) {
              <div class="mt-4">
                <h2 class="text-sm font-medium">Invalid headers</h2>
                <ul class="mt-2 list-disc pl-5 text-sm">
                  @for (header of invalidHeaders(); track header) {
                    <li>{{ header }}</li>
                  }
                </ul>
              </div>
            }

            @if (missingEmails().length) {
              <div class="mt-4">
                <h2 class="text-sm font-medium">Unknown email addresses</h2>
                <ul class="mt-2 list-disc pl-5 text-sm">
                  @for (email of missingEmails(); track email) {
                    <li>{{ email }}</li>
                  }
                </ul>
              </div>
            }
          </section>
        }
      </div>
    </app-dialog-content>

    <div app-dialog-actions align="end">
      <button app-stroked-button (click)="close()">Close</button>
      <button
        app-flat-button
        color="primary"
        [disabled]="!selectedFile() || loading() || response()?.isSuccess"
        (click)="importSelectedFile()">
        Import
      </button>
    </div>
  `,
})
export class ImportTasksDialogComponent {
  private readonly dialogRef =
    inject<DialogRef<void, ImportTasksDialogComponent>>(DialogRef);
  private readonly data = inject<ImportTasksDialogData>(DIALOG_DATA);
  private readonly taskService = inject(ProjectTasksService);
  private readonly store = inject(Store);

  readonly selectedFile = signal<File | null>(null);
  readonly loading = signal(false);
  readonly response = signal<ClientResponse<TaskImportResult> | null>(null);

  readonly missingHeaders = computed(
    () => this.response()?.payload?.headerValidationResult?.missingHeaders ?? []
  );
  readonly invalidHeaders = computed(
    () => this.response()?.payload?.headerValidationResult?.invalidHeaders ?? []
  );
  readonly missingEmails = computed(
    () => this.response()?.payload?.missingEmails ?? []
  );
  readonly validationMessage = computed(() => {
    const response = this.response();

    if (!response) return '';

    if (response.message) return response.message;

    return response.isSuccess
      ? 'No validation issues were found and the tasks were imported.'
      : 'Review the validation results, update the CSV, and try again.';
  });

  onFileSelected(file: File | null) {
    this.selectedFile.set(file);
    this.response.set(null);
  }

  async importSelectedFile() {
    const file = this.selectedFile();

    if (!file) return;

    this.loading.set(true);
    this.response.set(null);

    try {
      const response = await firstValueFrom(
        this.taskService.import(this.data.boardIdentifier, file)
      );

      this.response.set(response);

      if (response.isSuccess) {
        this.store.dispatch(importTasksSuccess());
      }
    } catch {
      this.response.set({
        isSuccess: false,
        message: 'Import failed before validation completed.',
      });
    } finally {
      this.loading.set(false);
    }
  }

  close() {
    this.dialogRef.close();
  }
}
