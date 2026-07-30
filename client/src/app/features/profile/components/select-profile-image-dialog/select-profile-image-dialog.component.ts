import { DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { uploadProfilePicture } from '@app/core/store/profile/profile.actions';
import { Store } from '@ngrx/store';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-select-profile-image-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title i18n="Title of the change-profile-picture dialog">
      Change Profile Picture
    </app-dialog-title>

    <div class="flex flex-col items-center gap-4">
      @if (previewUrl()) {
        <img
          [src]="previewUrl()"
          i18n-alt="Alt text for the chosen profile picture preview"
          alt="Preview"
          class="h-[180px] w-[180px] rounded-full object-cover" />
      } @else {
        <div
          class="bg-secondary-background text-muted flex h-[180px] w-[180px] items-center justify-center rounded-full">
          <span i18n="Shown before a profile picture has been chosen">
            No image selected
          </span>
        </div>
      }

      <input
        #fileInput
        type="file"
        accept="image/*"
        class="hidden"
        (change)="onFileSelected($event)" />

      <button app-stroked-button (click)="fileInput.click()">
        <span i18n="Button that opens the file picker for a profile picture">
          Select Image
        </span>
      </button>
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button (click)="dialogRef.close()">
        <span i18n="Dismisses a dialog without acting">Cancel</span>
      </button>
      <button app-flat-button [disabled]="!selectedFile()" (click)="onUpload()">
        <span i18n="Button that uploads the chosen profile picture">
          Upload
        </span>
      </button>
    </div>
  `,
})
export class SelectProfileImageDialogComponent {
  private store = inject(Store);
  dialogRef = inject(DialogRef);

  previewUrl = signal<string | null>(null);
  selectedFile = signal<File | null>(null);

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) return;

    this.selectedFile.set(file);

    const reader = new FileReader();
    reader.onload = (e) => this.previewUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  onUpload() {
    const file = this.selectedFile();
    if (!file) return;

    const formData = new FormData();
    formData.append('image', file, file.name);

    this.store.dispatch(uploadProfilePicture.init({ data: formData }));
    this.dialogRef.close();
  }
}
