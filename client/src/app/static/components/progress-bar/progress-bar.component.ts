import {
  ChangeDetectionStrategy,
  Component,
  HostBinding,
  computed,
  input,
} from '@angular/core';

export type ProgressBarMode = 'determinate' | 'indeterminate' | 'buffer';

@Component({
  selector: 'app-progress-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    @keyframes indeterminate-primary {
      0% { left: -35%; right: 100%; }
      60% { left: 100%; right: -90%; }
      100% { left: 100%; right: -90%; }
    }

    @keyframes indeterminate-secondary {
      0% { left: -200%; right: 100%; }
      60% { left: 107%; right: -8%; }
      100% { left: 107%; right: -8%; }
    }

    @keyframes buffer-dots {
      0% { background-position: 0 center; }
      100% { background-position: 10px center; }
    }

    .bar-primary-indeterminate {
      animation: indeterminate-primary 2s infinite linear;
    }

    .bar-secondary-indeterminate {
      animation: indeterminate-secondary 2s infinite linear;
    }

    .buffer-bg {
      animation: buffer-dots 250ms infinite linear;
      background-image: radial-gradient(circle, var(--progress-bar-track) 0%, var(--progress-bar-track) 16%, transparent 42%);
      background-size: 10px 100%;
    }
  `,
  template: `
    <div
      class="relative h-full w-full overflow-hidden rounded-full"
      [style]="{ '--progress-bar-track': 'oklch(var(--primary) / 0.3)' }"
      role="progressbar"
      [attr.aria-valuenow]="mode() !== 'indeterminate' ? clampedValue() : null"
      aria-valuemin="0"
      aria-valuemax="100">

      <!-- Track background (buffer dots or solid) -->
      @if (mode() === 'buffer') {
        <div class="buffer-bg absolute inset-0"></div>
        <div
          class="absolute inset-y-0 left-0 rounded-full bg-primary/30 transition-[width] duration-300"
          [style.width]="clampedBufferValue() + '%'"></div>
      } @else {
        <div class="absolute inset-0 rounded-full bg-primary/20"></div>
      }

      <!-- Primary bar -->
      @if (mode() === 'indeterminate') {
        <div class="bar-primary-indeterminate absolute inset-y-0 rounded-full bg-primary"></div>
        <div class="bar-secondary-indeterminate absolute inset-y-0 rounded-full bg-primary"></div>
      } @else {
        <div
          class="absolute inset-y-0 left-0 rounded-full bg-primary transition-[width] duration-300"
          [style.width]="clampedValue() + '%'"></div>
      }
    </div>
  `,
})
export class ProgressBarComponent {
  readonly mode = input<ProgressBarMode>('determinate');
  readonly value = input(0);
  readonly bufferValue = input(0);

  readonly clampedValue = computed(() => Math.min(100, Math.max(0, this.value())));
  readonly clampedBufferValue = computed(() =>
    Math.min(100, Math.max(0, this.bufferValue())),
  );

  @HostBinding('class') readonly hostClass =
    'block h-1 w-full overflow-hidden rounded-full';
}
