import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    .loader {
      background: linear-gradient(
        to right,
        var(--primary) 10%,
        rgba(129, 55, 241, 0) 42%
      );
      animation: load 1.4s infinite linear;
    }

    .loader::before {
      border-radius: 100% 0 0;
      content: '';
      background: var(--primary);
    }

    .loader::after {
      content: '';
      background: var(--card);
    }

    @keyframes load {
      0% {
        transform: rotate(0deg);
      }
      100% {
        transform: rotate(360deg);
      }
    }
  `,
  template: `<div
    class="loader relative translate-z-0 rounded-full before:absolute before:top-0 before:left-0 before:h-1/2 before:w-1/2 after:absolute after:inset-0 after:m-auto after:h-3/4 after:w-3/4 after:rounded-full"
    [style]="{ height: diameter(), width: diameter() }"></div>`,
})
export class SpinnerComponent {
  readonly diameter = input('2rem');
}
