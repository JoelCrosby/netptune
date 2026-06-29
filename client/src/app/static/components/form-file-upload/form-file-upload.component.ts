import { Component, ElementRef, input, output, viewChild } from '@angular/core';
import { LucideFileUp } from '@lucide/angular';
import { FormControlLabelDirective } from '../form-control/form-control.directives';

@Component({
  selector: 'app-form-file-upload',
  imports: [LucideFileUp, FormControlLabelDirective],
  template: `
    <div class="nept-form-control mb-[1.4rem] w-[inherit]">
      @if (label()) {
        <label [for]="name()" appFormLabel>
          {{ label() }}
        </label>
      }

      <button
        type="button"
        class="border-border hover:bg-hover flex w-full cursor-pointer flex-col items-center justify-center gap-3 rounded border border-dashed px-6 py-8 text-center transition-colors disabled:pointer-events-none disabled:opacity-50"
        [disabled]="disabled()"
        (click)="openFilePicker()">
        <svg lucideFileUp class="text-primary h-8 w-8"></svg>
        <span class="text-sm font-medium">
          {{ file()?.name ?? placeholder() }}
        </span>
        @if (hint()) {
          <span class="text-foreground/60 text-xs">{{ hint() }}</span>
        }
      </button>

      <input
        #fileInput
        [id]="name()"
        [name]="name()"
        type="file"
        class="hidden"
        [attr.accept]="accept()"
        [disabled]="disabled()"
        (change)="onFileInputChanged($event)" />
    </div>
  `,
})
export class FormFileUploadComponent {
  readonly name = input('file');
  readonly label = input<string | null>(null);
  readonly placeholder = input('Choose a file');
  readonly hint = input<string | null>(null);
  readonly accept = input<string | null>(null);
  readonly disabled = input(false);
  readonly file = input<File | null>(null);

  readonly fileInput =
    viewChild.required<ElementRef<HTMLInputElement>>('fileInput');

  readonly fileSelected = output<File | null>();

  openFilePicker() {
    const input = this.fileInput().nativeElement;

    input.value = '';
    input.click();
  }

  onFileInputChanged(event: Event) {
    const input = event.target as HTMLInputElement;

    this.fileSelected.emit(input.files?.[0] ?? null);
  }
}
