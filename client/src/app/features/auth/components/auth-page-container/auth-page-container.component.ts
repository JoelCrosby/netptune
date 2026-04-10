import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-auth-page-container',
  template: `<ng-content />`,
  host: {
    class:
      'h-screen    flex flex-col items-center justify-center grid place-items-center',
  },
  styles: [
    `
      :host::before {
        position: absolute;
        left: 0;
        bottom: 0;
        content: '';
        width: 100%;
        height: 50vh;
        background-size: 20px 20px;
        background-color: rgba(var(--background-rgb), 0.6);
        background-image: radial-gradient(
          rgba(var(--foreground-rgb), 0.2) 1px,
          var(--background) 1px
        );
        z-index: 0;
        transform: rotateX(20deg);
      }

      @media only screen and (max-width: 600px) {
        :host {
          font-size: 18px;
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthPageContainerComponent {}
