import { Component, computed, input, output, signal } from '@angular/core';
import {
  brandingImageAccept,
  brandingImageError,
  brandingImageMaxBytes,
} from '@core/util/branding';
import { formatBytes } from '@core/util/bytes';
import { colorHex } from '@core/util/colors/colors';
import { LucideImage } from '@lucide/angular';
import { DropOverlayComponent } from '@static/components/drop-overlay.component';
import { LogoPickerComponent } from '@static/components/logo-picker/logo-picker.component';
import { FileDropDirective } from '@static/directives/file-drop.directive';

const overlayButtonClass =
  'inline-flex h-[30px] cursor-pointer items-center gap-1.5 rounded-md border border-white/15 bg-black/60 px-3 text-xs font-semibold text-white/85 backdrop-blur-md hover:bg-black/80 hover:text-white disabled:cursor-not-allowed disabled:opacity-50';

export type BrandingImageKind = 'logo' | 'background';

// A live header preview where the logo and background are edited in place: click the
// logo or drop images onto it, and drop or pick a background for the banner. Files are
// validated here; the parent only receives usable images.
@Component({
  selector: 'app-branding-preview',
  imports: [
    DropOverlayComponent,
    FileDropDirective,
    LogoPickerComponent,
    LucideImage,
  ],
  host: { class: 'flex flex-col gap-2' },
  template: `
    <div
      class="relative h-[148px] overflow-hidden rounded-xl border border-white/10 bg-neutral-900 bg-cover bg-center text-white"
      [style.background-image]="backgroundImage()"
      appFileDrop
      #backgroundDrop="appFileDrop"
      [fileDropDisabled]="disabled()"
      (filesDropped)="select('background', $event[0])">
      <div class="absolute top-2.5 right-2.5 flex gap-1.5">
        @if (backgroundUrl()) {
          <button
            type="button"
            [class]="overlayButtonClass"
            [disabled]="disabled()"
            (click)="removed.emit('background')">
            <span i18n="Button that removes a background image">Remove</span>
          </button>
        }
        <button
          type="button"
          [class]="overlayButtonClass"
          [disabled]="disabled()"
          (click)="backgroundPicker.click()">
          <svg lucideImage class="h-3.5 w-3.5" aria-hidden="true"></svg>
          @if (backgroundUrl()) {
            <span i18n="Button that replaces a background image">
              Change background
            </span>
          } @else {
            <span i18n="Button that adds a background image">
              Add background
            </span>
          }
        </button>
      </div>

      <div class="absolute right-4 bottom-3.5 left-4 flex items-end gap-3.5">
        <app-logo-picker
          #logoPicker
          class="shrink-0"
          [name]="title()"
          [color]="color()"
          [imageUrl]="logoUrl()"
          [disabled]="disabled()"
          (fileSelected)="
            backgroundDrop.dragging.set(false); select('logo', $event)
          " />

        <div
          class="flex min-w-0 flex-1 flex-col gap-0.5 pb-1 [text-shadow:0_1px_8px_rgba(0,0,0,.6)]">
          <span class="truncate text-xl font-semibold">
            {{ displayTitle() }}
          </span>
          @if (subtitle()) {
            <span class="truncate font-mono text-xs text-white/60">
              {{ subtitle() }}
            </span>
          }
        </div>
      </div>

      @if (backgroundDrop.dragging() && !logoPicker.dragging()) {
        <app-drop-overlay
          i18n-label="Shown while an image is dragged over a branding preview"
          label="Drop to set background" />
      }
    </div>

    <div class="text-muted flex justify-between gap-3 text-xs">
      @if (logoUrl()) {
        <button
          type="button"
          class="hover:text-foreground cursor-pointer underline-offset-2 hover:underline disabled:cursor-not-allowed"
          [disabled]="disabled()"
          (click)="removed.emit('logo')"
          i18n="Button that removes a logo">
          Remove logo
        </button>
      } @else {
        <span i18n="Hint under a branding preview explaining how to set images">
          Click the logo or drop images onto the preview.
        </span>
      }
      <span
        i18n="
          Accepted image formats under a branding preview. SIZE is a formatted
          byte limit such as 10 MiB
        ">
        PNG, JPEG, WebP, GIF or AVIF · {{ maxBytesLabel }} max
      </span>
    </div>

    @if (error()) {
      <p class="text-destructive text-sm" aria-live="polite">{{ error() }}</p>
    }

    <input
      #backgroundPicker
      class="sr-only"
      type="file"
      tabindex="-1"
      [attr.accept]="accept"
      [disabled]="disabled()"
      (change)="onBackgroundInput($event)" />
  `,
})
export class BrandingPreviewComponent {
  readonly title = input('');
  readonly subtitle = input<string | null>(null);
  readonly color = input<string | null>(null);
  readonly logoUrl = input<string | null>(null);
  readonly backgroundUrl = input<string | null>(null);
  readonly disabled = input(false);

  readonly fileSelected = output<{ kind: BrandingImageKind; file: File }>();
  readonly removed = output<BrandingImageKind>();

  protected readonly overlayButtonClass = overlayButtonClass;
  protected readonly accept = brandingImageAccept;
  protected readonly maxBytesLabel = formatBytes(brandingImageMaxBytes);
  protected readonly error = signal('');

  protected readonly displayTitle = computed(() => {
    const title = this.title().trim();

    return title || $localize`:Preview title when no name is entered:Untitled`;
  });

  // Without an image the banner takes a wash of the colour.
  protected readonly backgroundImage = computed(() => {
    const url = this.backgroundUrl();

    if (url) {
      return `linear-gradient(180deg, rgba(0,0,0,0) 30%, rgba(0,0,0,.7) 100%), url("${url}")`;
    }

    const hex = colorHex(this.color());

    return `radial-gradient(ellipse 80% 140% at 0% 100%, ${hex}55 0%, transparent 65%), radial-gradient(ellipse 60% 120% at 100% 0%, ${hex}22 0%, transparent 70%)`;
  });

  protected onBackgroundInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    input.value = '';

    if (!file) return;

    this.select('background', file);
  }

  protected select(kind: BrandingImageKind, file: File | undefined) {
    if (!file) return;

    const error = brandingImageError(file);

    this.error.set(error);

    if (error) return;

    this.fileSelected.emit({ kind, file });
  }
}
