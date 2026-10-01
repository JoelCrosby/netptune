import { Component, computed, input, output, viewChild } from '@angular/core';
import { brandingImageAccept } from '@core/util/branding';
import { colorHex } from '@core/util/colors/colors';
import { LucidePencil } from '@lucide/angular';
import { FileDropDirective } from '@static/directives/file-drop.directive';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-logo-picker',
  imports: [FileDropDirective, TooltipDirective, LucidePencil],
  host: { class: 'block w-fit' },
  template: `
    <button
      type="button"
      class="relative block h-16 w-16 cursor-pointer rounded-[15px] focus-visible:ring-2 focus-visible:ring-white focus-visible:outline-none disabled:cursor-not-allowed"
      [appTooltip]="label()"
      [attr.aria-label]="label()"
      [disabled]="disabled()"
      appFileDrop
      #drop="appFileDrop"
      [fileDropDisabled]="disabled()"
      (filesDropped)="fileSelected.emit($event[0])"
      (dragover)="$event.stopPropagation()"
      (drop)="$event.stopPropagation()"
      (click)="picker.click()">
      <span
        class="border-dialog-background flex h-16 w-16 items-center justify-center overflow-hidden rounded-[15px] border-2 bg-cover bg-center text-[26px] font-semibold text-white"
        [style.background-color]="hex()"
        [style.background-image]="image()"
        [style.box-shadow]="shadow()">
        @if (!imageUrl()) {
          {{ letter() }}
        }
      </span>
      <span
        class="border-dialog-background bg-primary text-primary-foreground absolute -right-1.5 -bottom-1.5 flex h-6 w-6 items-center justify-center rounded-full border-2">
        <svg lucidePencil class="h-3 w-3" aria-hidden="true"></svg>
      </span>
      @if (drop.dragging()) {
        <span
          class="border-primary absolute -inset-1 rounded-[18px] border-2 border-dashed bg-black/60"></span>
      }
    </button>

    <input
      #picker
      class="sr-only"
      type="file"
      tabindex="-1"
      [attr.accept]="accept"
      [disabled]="disabled()"
      (change)="onInput($event)" />
  `,
})
export class LogoPickerComponent {
  readonly name = input('');
  readonly color = input<string | null>(null);
  readonly imageUrl = input<string | null>(null);
  readonly disabled = input(false);
  readonly label = input(
    $localize`:Tooltip and accessible label of a logo picker:Change logo`
  );

  readonly fileSelected = output<File>();

  private readonly drop = viewChild.required(FileDropDirective);

  // True while files are dragged over the tile.
  readonly dragging = computed(() => this.drop().dragging());

  protected readonly accept = brandingImageAccept;

  protected readonly hex = computed(() => colorHex(this.color()));

  protected readonly letter = computed(() => {
    const name = this.name().trim();

    return name.charAt(0).toUpperCase() || '?';
  });

  protected readonly image = computed(() => {
    const url = this.imageUrl();

    return url ? `url("${url}")` : 'none';
  });

  protected readonly shadow = computed(() => {
    const hasImage = !!this.imageUrl();

    return hasImage
      ? '0 6px 18px rgba(0,0,0,.5)'
      : `0 6px 18px ${this.hex()}55`;
  });

  protected onInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    input.value = '';

    if (!file) return;

    this.fileSelected.emit(file);
  }
}
