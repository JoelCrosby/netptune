import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { uploadProfilePicture } from '@profile/store/profile.actions';
import { Store } from '@ngrx/store';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-select-profile-image-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title>Change Profile Picture</app-dialog-title>

    <div class="flex flex-col items-center gap-4">
      @if (previewUrl()) {
        <img
          [src]="previewUrl()"
          alt="Preview"
          class="h-[180px] w-[180px] rounded-full object-cover" />
      } @else {
        <div
          class="flex h-[180px] w-[180px] items-center justify-center rounded-full bg-[var(--background-two)] text-[var(--text-two)]">
          No image selected
        </div>
      }

      <input
        #fileInput
        type="file"
        accept="image/*"
        class="hidden"
        (change)="onFileSelected($event)" />

      <button app-stroked-button (click)="fileInput.click()">
        Select Image
      </button>
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button (click)="dialogRef.close()">Cancel</button>
      <button
        app-flat-button
        [disabled]="!selectedFile()"
        (click)="onUpload()">
        Upload
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

    this.store.dispatch(uploadProfilePicture({ data: formData }));
    this.dialogRef.close();
  }
}
