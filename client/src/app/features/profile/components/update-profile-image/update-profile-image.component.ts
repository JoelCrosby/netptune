import { Component, input, output, signal } from '@angular/core';
import {
  brandingImageAccept,
  brandingImageError,
  brandingImageMaxBytes,
} from '@core/util/branding';
import { formatBytes } from '@core/util/bytes';
import {
  LucideCircleUserRound,
  LucideTrash2,
  LucideUpload,
} from '@lucide/angular';
import { AvatarPickerComponent } from '@static/components/avatar-picker/avatar-picker.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { MenuSeparatorComponent } from '@static/components/dropdown-menu/menu-separator.component';
import { DropOverlayComponent } from '@static/components/drop-overlay.component';
import { FormErrorComponent } from '@static/components/form-error/form-error.component';
import { MediaRowComponent } from '@static/components/media-row.component';
import { FileDropDirective } from '@static/directives/file-drop.directive';

@Component({
  selector: 'app-update-profile-image',
  imports: [
    AvatarPickerComponent,
    DropOverlayComponent,
    FileDropDirective,
    FormErrorComponent,
    LucideCircleUserRound,
    LucideTrash2,
    LucideUpload,
    MediaRowComponent,
    MenuItemComponent,
    MenuSeparatorComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    <app-media-row
      appFileDrop
      #drop="appFileDrop"
      i18n-heading="Heading of the profile picture row"
      heading="Profile picture"
      [fileDropDisabled]="uploading()"
      (filesDropped)="handleFile($event[0])">
      <app-avatar-picker
        mediaRowMedia
        [imageUrl]="pictureUrl()"
        [name]="name()"
        [busy]="uploading()"
        i18n-pickLabel="Accessible label of the profile picture button"
        pickLabel="Change profile picture"
        i18n-actionsLabel="Accessible label of the profile picture actions menu"
        actionsLabel="Picture actions"
        i18n-overlayLabel="Overlay on the profile picture that opens the picker"
        overlayLabel="Change"
        (pick)="picker.click()">
        <ng-container avatarPickerActions>
          <button app-menu-item (click)="picker.click()">
            <svg lucideUpload class="h-4 w-4"></svg>
            <span i18n="Menu item that opens the profile picture picker">
              Upload a photo
            </span>
          </button>
          <button app-menu-item (click)="gravatarClicked.emit()">
            <svg lucideCircleUserRound class="h-4 w-4"></svg>
            <span i18n="Menu item that sets the profile picture from Gravatar">
              Use my Gravatar
            </span>
          </button>
          @if (pictureUrl()) {
            <app-menu-separator />
            <button app-menu-item color="warn" (click)="removeClicked.emit()">
              <svg lucideTrash2 class="h-4 w-4"></svg>
              <span i18n="Menu item that removes the profile picture">
                Remove picture
              </span>
            </button>
          }
        </ng-container>
      </app-avatar-picker>

      <span
        i18n="
          Hint beside the profile picture. SIZE is a formatted byte limit such
          as 10 MiB
        ">
        Click the picture to replace it, or drag an image onto this row. PNG,
        JPEG, WebP, GIF or AVIF, up to {{ maxBytesLabel }}.
      </span>

      @if (error()) {
        <app-form-error aria-live="polite">{{ error() }}</app-form-error>
      }

      <button
        mediaRowActions
        app-stroked-button
        type="button"
        [disabled]="uploading()"
        (click)="picker.click()">
        <svg lucideUpload class="mr-2 h-4 w-4"></svg>
        <span i18n="Button that opens the profile picture picker">Upload</span>
      </button>

      @if (drop.dragging()) {
        <app-drop-overlay
          mediaRowOverlay
          i18n-label="
            Shown while an image is dragged over the profile picture row
          "
          label="Drop to upload" />
      }

      <input
        #picker
        mediaRowOverlay
        class="sr-only"
        type="file"
        tabindex="-1"
        aria-hidden="true"
        [attr.accept]="accept"
        (change)="onInput($event)" />
    </app-media-row>
  `,
})
export class UpdateProfileImageComponent {
  readonly pictureUrl = input<string | null | undefined>(null);
  readonly name = input<string | null | undefined>(null);
  readonly uploading = input(false);

  readonly fileSelected = output<File>();
  readonly gravatarClicked = output();
  readonly removeClicked = output();

  protected readonly accept = brandingImageAccept;
  protected readonly maxBytesLabel = formatBytes(brandingImageMaxBytes);
  protected readonly error = signal('');

  protected onInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    input.value = '';

    if (!file) return;

    this.handleFile(file);
  }

  protected handleFile(file: File) {
    const error = brandingImageError(file);

    this.error.set(error);

    if (error) return;

    this.fileSelected.emit(file);
  }
}
