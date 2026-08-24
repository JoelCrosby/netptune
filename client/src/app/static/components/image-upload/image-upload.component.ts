import { Component, computed, input, output, signal } from '@angular/core';
import {
  brandingImageAccept,
  brandingImageMaxBytes,
  isBrandingImageType,
} from '@core/util/branding';
import { formatBytes } from '@core/util/bytes';
import { LucideImage, LucideTrash2, LucideUpload } from '@lucide/angular';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

export type ImageUploadShape = 'square' | 'circle' | 'wide';

@Component({
  selector: 'app-image-upload',
  imports: [
    LucideImage,
    LucideTrash2,
    LucideUpload,
    SpinnerComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    <div class="flex flex-wrap items-start gap-5">
      <div
        class="border-border bg-secondary-background relative shrink-0 overflow-hidden border"
        [class]="frameClass()"
        [class.border-primary]="dragging()"
        [class.border-dashed]="!imageUrl()"
        (dragover)="onDragOver($event)"
        (dragleave)="dragging.set(false)"
        (drop)="onDrop($event)">
        @if (imageUrl(); as url) {
          <img
            [src]="url"
            [alt]="alt()"
            class="h-full w-full object-cover"
            [class.object-contain]="shape() !== 'wide'" />
        } @else {
          <div
            class="text-muted flex h-full w-full items-center justify-center">
            <svg lucideImage class="h-6 w-6"></svg>
          </div>
        }

        @if (uploading()) {
          <div
            class="bg-card/70 absolute inset-0 flex items-center justify-center">
            <app-spinner diameter="24px" />
          </div>
        }
      </div>

      <div class="min-w-0 flex-1">
        <input
          #picker
          class="sr-only"
          type="file"
          [attr.accept]="accept"
          [disabled]="isBusy()"
          (change)="onInput($event)" />

        <div class="flex flex-wrap items-center gap-2">
          <button
            app-stroked-button
            type="button"
            [disabled]="isBusy()"
            (click)="picker.click()">
            <svg lucideUpload class="mr-2 h-4 w-4"></svg>
            @if (imageUrl()) {
              <span i18n="Button that replaces an already uploaded image">
                Replace
              </span>
            } @else {
              <span i18n="Button that opens the image picker"
                >Upload image</span
              >
            }
          </button>

          @if (imageUrl()) {
            <button
              app-stroked-button
              color="warn"
              type="button"
              [disabled]="isBusy()"
              (click)="removed.emit()">
              <svg lucideTrash2 class="mr-2 h-4 w-4"></svg>
              <span i18n="Button that removes an uploaded image">Remove</span>
            </button>
          }
        </div>

        <p class="text-muted mt-2 text-xs">
          <span
            i18n="
              Hint under an image picker. SIZE is a formatted byte limit such as
              10 MiB
            ">
            PNG, JPEG, WebP, GIF or AVIF · drag and drop or choose a file ·
            {{ maxBytesLabel }} maximum
          </span>
        </p>

        @if (error()) {
          <p class="text-destructive mt-2 text-sm" aria-live="polite">
            {{ error() }}
          </p>
        }
      </div>
    </div>
  `,
})
export class ImageUploadComponent {
  readonly imageUrl = input<string | null>(null);
  readonly alt = input('');
  readonly shape = input<ImageUploadShape>('square');
  readonly disabled = input(false);
  readonly uploading = input(false);

  readonly fileSelected = output<File>();
  readonly removed = output();

  protected readonly accept = brandingImageAccept;
  protected readonly maxBytesLabel = formatBytes(brandingImageMaxBytes);
  protected readonly dragging = signal(false);
  protected readonly error = signal('');

  protected readonly isBusy = computed(() => {
    return this.disabled() || this.uploading();
  });

  protected readonly frameClass = computed(() => {
    switch (this.shape()) {
      case 'circle':
        return 'h-24 w-24 rounded-full';
      case 'wide':
        return 'h-28 w-56 rounded-lg';
      default:
        return 'h-24 w-24 rounded-lg';
    }
  });

  protected onDragOver(event: DragEvent) {
    event.preventDefault();
    this.dragging.set(!this.isBusy());
  }

  protected onDrop(event: DragEvent) {
    event.preventDefault();
    this.dragging.set(false);

    if (this.isBusy()) return;

    const file = event.dataTransfer?.files?.[0];

    if (!file) return;

    this.handleFile(file);
  }

  protected onInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    input.value = '';

    if (!file) return;

    this.handleFile(file);
  }

  private handleFile(file: File) {
    const isSupportedType = isBrandingImageType(file);

    if (!isSupportedType) {
      this.error.set(
        $localize`:Validation error when a chosen file is not a supported image:Choose a PNG, JPEG, WebP, GIF or AVIF image.`
      );

      return;
    }

    if (file.size > brandingImageMaxBytes) {
      this.error.set(
        $localize`:Validation error when a chosen image is too large. SIZE is a formatted byte limit:The image must be smaller than ${this.maxBytesLabel}:SIZE:.`
      );

      return;
    }

    this.error.set('');
    this.fileSelected.emit(file);
  }
}
