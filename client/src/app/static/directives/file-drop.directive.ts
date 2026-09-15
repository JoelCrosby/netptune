import { Directive, input, output, signal } from '@angular/core';

// Turns the host into a drop target for files from the desktop. `dragging` is exposed
// through the template reference so the host can style itself while a drag is over it.
@Directive({
  selector: '[appFileDrop]',
  exportAs: 'appFileDrop',
  host: {
    '(dragover)': 'onDragOver($event)',
    '(dragleave)': 'onDragLeave($event)',
    '(drop)': 'onDrop($event)',
  },
})
export class FileDropDirective {
  readonly fileDropDisabled = input(false);

  readonly filesDropped = output<File[]>();

  readonly dragging = signal(false);

  protected onDragOver(event: DragEvent) {
    event.preventDefault();
    this.dragging.set(!this.fileDropDisabled());
  }

  // dragleave also fires when the pointer crosses onto a child of the host.
  protected onDragLeave(event: DragEvent) {
    const host = event.currentTarget as HTMLElement;
    const related = event.relatedTarget as Node | null;

    if (related && host.contains(related)) return;

    this.dragging.set(false);
  }

  protected onDrop(event: DragEvent) {
    event.preventDefault();
    this.dragging.set(false);

    if (this.fileDropDisabled()) return;

    const files = Array.from(event.dataTransfer?.files ?? []);

    if (!files.length) return;

    this.filesDropped.emit(files);
  }
}
