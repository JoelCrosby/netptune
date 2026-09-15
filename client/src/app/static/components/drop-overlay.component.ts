import { Component, input } from '@angular/core';

// Covers a relatively positioned `appFileDrop` host while files are dragged over it.
@Component({
  selector: 'app-drop-overlay',
  host: {
    class:
      'border-primary bg-card text-primary pointer-events-none absolute inset-0 flex items-center justify-center rounded-[inherit] border-2 border-dashed text-sm font-semibold',
  },
  template: `{{ label() }}`,
})
export class DropOverlayComponent {
  readonly label = input.required<string>();
}
