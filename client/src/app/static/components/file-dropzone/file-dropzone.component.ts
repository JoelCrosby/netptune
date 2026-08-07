import { Component, input, output, signal } from '@angular/core';
import { formatBytes } from '@core/util/bytes';
import { LucideUpload } from '@lucide/angular';

const defaultMaxFileSize = 50 * 1024 * 1024;

@Component({
  selector: 'app-file-dropzone',
  imports: [LucideUpload],
  host: { class: 'block' },
  template: `
    <div
      class="border-border bg-card/40 rounded border border-dashed p-4 text-center"
      [class.border-primary]="dragging()"
      (dragover)="onDragOver($event)"
      (dragleave)="dragging.set(false)"
      (drop)="onDrop($event)">
      <input
        #picker
        class="sr-only"
        type="file"
        multiple
        [attr.accept]="acceptTypes() || null"
        [disabled]="disabled()"
        (change)="onInput($event)" />
      <button
        type="button"
        class="text-primary inline-flex items-center gap-2 rounded px-3 py-2 hover:underline disabled:opacity-50"
        [disabled]="disabled()"
        (click)="picker.click()">
        <svg lucideUpload class="h-4 w-4"></svg>
        <span i18n="Button that opens the file picker">Choose files</span>
      </button>
      <p class="text-muted text-xs">
        <span
          i18n="
            Hint under the file picker. SIZE is a formatted byte limit such as
            50 MB
          ">
          or drag and drop ·
          {{
            formatBytes(maxBytes()) // i18n(ph="SIZE")
          }}
          maximum per file
        </span>
      </p>
      @if (remainingBytes() !== undefined) {
        <p class="text-muted mt-1 text-xs">
          <span
            i18n="
              Remaining upload allowance. SIZE is a formatted byte count such as
              1.2 MB
            ">
            {{
              formatBytes(remainingBytes()!) // i18n(ph="SIZE")
            }}
            remaining
          </span>
        </p>
      }
      <p class="text-destructive mt-2 text-sm" aria-live="polite">
        {{ error() }}
      </p>
    </div>
  `,
})
export class FileDropzoneComponent {
  protected readonly formatBytes = formatBytes;

  readonly disabled = input(false);
  /** Comma-separated extension list for the picker, e.g. ".csv,.xlsx". */
  readonly acceptTypes = input('');
  readonly maxBytes = input(defaultMaxFileSize);
  readonly remainingBytes = input<number>();
  readonly filesSelected = output<File[]>();
  readonly dragging = signal(false);
  readonly error = signal('');

  onDragOver(event: DragEvent) {
    event.preventDefault();

    if (!this.disabled()) this.dragging.set(true);
  }

  onDrop(event: DragEvent) {
    event.preventDefault();

    this.dragging.set(false);

    if (!this.disabled()) {
      this.accept(Array.from(event.dataTransfer?.files ?? []));
    }
  }

  onInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.accept(Array.from(input.files ?? []));
    input.value = '';
  }

  private accept(files: File[]) {
    const limit = this.maxBytes();
    const valid = files.filter((file) => file.size > 0 && file.size <= limit);
    const rejected = files.length - valid.length;
    this.error.set(
      rejected
        ? `${rejected} file(s) were empty or exceeded ${formatBytes(limit)}.`
        : ''
    );

    if (valid.length) {
      this.filesSelected.emit(valid);
    }
  }
}
