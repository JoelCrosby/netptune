import { Component, computed, input } from '@angular/core';
import { ProgressBarComponent } from '../progress-bar/progress-bar.component';

// One file on its way up. Project an action, such as a retry, to show beside a
// failure.
@Component({
  selector: 'app-upload-progress',
  imports: [ProgressBarComponent],
  host: { class: 'bg-card block rounded p-2 text-sm' },
  template: `
    <div class="flex items-center justify-between gap-2">
      <span class="truncate">{{ name() }}</span>
      @if (error(); as error) {
        <span class="text-destructive ml-auto">{{ error }}</span>
        <ng-content />
      } @else {
        <span>{{ progress() }}%</span>
      }
    </div>
    <app-progress-bar
      class="mt-1"
      [value]="progress()"
      [ariaLabel]="name()"
      [valueText]="valueText()" />
  `,
})
export class UploadProgressComponent {
  readonly name = input.required<string>();
  readonly progress = input(0);
  readonly error = input<string | null | undefined>(null);

  protected readonly valueText = computed(() => {
    return this.error() ?? `${this.progress()}%`;
  });
}
