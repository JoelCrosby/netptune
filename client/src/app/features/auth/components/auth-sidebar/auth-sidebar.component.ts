import { Component } from '@angular/core';
import { BrandLogoComponent } from '@static/components/brand-logo.component';
import { BuildNumberComponent } from '@static/components/build-number/build-number.component';

@Component({
  selector: 'app-auth-sidebar',
  imports: [BrandLogoComponent, BuildNumberComponent],
  host: {
    class:
      'bg-side-bar border-side-bar-border relative flex flex-col overflow-hidden border-r p-10 text-white',
  },
  template: `
    <div class="auth-planet-stage" aria-hidden="true">
      <div class="auth-planet-glow"></div>
      <div class="auth-ring auth-ring--back">
        <div class="auth-ring-band"></div>
        <div class="auth-ring-band auth-ring-band--inner"></div>
      </div>
      <div class="auth-planet">
        <div class="auth-planet-surface"></div>
        <div class="auth-planet-shade"></div>
        <div class="auth-planet-rim"></div>
      </div>
      <div class="auth-ring auth-ring--front">
        <div class="auth-ring-band"></div>
        <div class="auth-ring-band auth-ring-band--inner"></div>
      </div>
    </div>

    <div class="relative z-1 flex items-center gap-3.5">
      <app-brand-logo size="small" />
      <span class="text-[15px] font-semibold tracking-[.225px]">
        {{ brandName }}
      </span>
    </div>

    <app-build-number class="relative z-1 mt-auto" appearance="inline" />
  `,
  styles: [
    `
      .auth-planet-stage {
        --planet-size: 360px;
        --ring-size: 860px;

        position: absolute;
        left: 50%;
        top: 62%;
        width: 1080px;
        height: 1080px;
        transform: translate(-50%, -50%);
        perspective: 1400px;
        pointer-events: none;
      }

      .auth-planet-glow,
      .auth-planet,
      .auth-ring {
        position: absolute;
        left: 50%;
        top: 50%;
      }

      .auth-planet-glow {
        width: 900px;
        height: 900px;
        transform: translate(-50%, -50%);
        border-radius: 50%;
        background: radial-gradient(
          circle,
          rgb(139 92 246 / 0.28) 0%,
          rgb(139 92 246 / 0.1) 34%,
          rgb(139 92 246 / 0) 60%
        );
        filter: blur(24px);
        animation: auth-planet-glow 9s ease-in-out infinite;
      }

      .auth-ring {
        width: var(--ring-size);
        height: var(--ring-size);
        transform: translate(-50%, -50%) rotate(-16deg) rotateX(74deg);
      }

      .auth-ring--back {
        clip-path: polygon(0 0, 100% 0, 100% 50%, 0 50%);
      }

      .auth-ring--front {
        clip-path: polygon(0 50%, 100% 50%, 100% 100%, 0 100%);
      }

      .auth-ring-band {
        position: absolute;
        inset: 0;
        border-radius: 50%;
        background: conic-gradient(
          from 200deg,
          rgb(167 139 250 / 0) 0deg,
          rgb(233 213 255 / 0.75) 55deg,
          rgb(167 139 250 / 0.18) 130deg,
          rgb(125 211 252 / 0.65) 225deg,
          rgb(217 70 239 / 0.4) 300deg,
          rgb(167 139 250 / 0) 360deg
        );
        mask-image: radial-gradient(
          circle,
          transparent 0 47.2%,
          black 47.9% 49.3%,
          transparent 50%
        );
        animation: auth-ring-spin 54s linear infinite;
      }

      .auth-ring-band--inner {
        inset: 42px;
        background: conic-gradient(
          from 20deg,
          rgb(167 139 250 / 0) 0deg,
          rgb(196 181 253 / 0.4) 70deg,
          rgb(167 139 250 / 0.08) 150deg,
          rgb(233 213 255 / 0.35) 260deg,
          rgb(167 139 250 / 0) 360deg
        );
        mask-image: radial-gradient(
          circle,
          transparent 0 47.8%,
          black 48.4% 49.6%,
          transparent 50%
        );
        animation-duration: 78s;
        animation-direction: reverse;
      }

      .auth-planet {
        width: var(--planet-size);
        height: var(--planet-size);
        transform: translate(-50%, -50%);
        border-radius: 50%;
        overflow: hidden;
        background: radial-gradient(
          circle at 32% 26%,
          #8b6ce0 0%,
          #5b32ab 30%,
          #331768 58%,
          #170a33 80%,
          #08040f 100%
        );
        box-shadow:
          0 0 90px rgb(139 92 246 / 0.35),
          inset 0 0 70px rgb(0 0 0 / 0.75);
      }

      .auth-planet-surface {
        position: absolute;
        top: -10%;
        left: 0;
        width: 200%;
        height: 120%;
        opacity: 0.55;
        background-image:
          repeating-linear-gradient(
            90deg,
            rgb(255 255 255 / 0.09) 0 1px,
            rgb(255 255 255 / 0) 1px 26px
          ),
          repeating-linear-gradient(
            90deg,
            rgb(196 181 253 / 0.16) 0 3px,
            rgb(196 181 253 / 0) 3px 78px
          ),
          repeating-linear-gradient(
            0deg,
            rgb(0 0 0 / 0.22) 0 2px,
            rgb(0 0 0 / 0) 2px 34px
          );
        animation: auth-planet-spin 48s linear infinite;
      }

      .auth-planet-shade {
        position: absolute;
        inset: 0;
        border-radius: 50%;
        background:
          radial-gradient(
            circle at 30% 24%,
            rgb(255 255 255 / 0.32) 0%,
            rgb(255 255 255 / 0.05) 22%,
            rgb(255 255 255 / 0) 42%
          ),
          radial-gradient(
            circle at 76% 78%,
            rgb(4 2 10 / 0) 12%,
            rgb(4 2 10 / 0.72) 62%,
            rgb(4 2 10 / 0.95) 100%
          );
      }

      .auth-planet-rim {
        position: absolute;
        inset: 0;
        border-radius: 50%;
        box-shadow:
          inset 6px 8px 34px rgb(196 181 253 / 0.22),
          inset -18px -22px 60px rgb(0 0 0 / 0.85),
          inset 0 0 0 1.5px rgb(206 190 255 / 0.5),
          inset 0 0 26px rgb(167 139 250 / 0.3);
      }

      @keyframes auth-planet-spin {
        from {
          transform: translateX(0);
        }
        to {
          transform: translateX(-50%);
        }
      }

      @keyframes auth-ring-spin {
        from {
          transform: rotate(0deg);
        }
        to {
          transform: rotate(360deg);
        }
      }

      @keyframes auth-planet-glow {
        0%,
        100% {
          opacity: 0.75;
        }
        50% {
          opacity: 1;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .auth-planet-surface,
        .auth-ring-band,
        .auth-planet-glow {
          animation: none;
        }
      }
    `,
  ],
})
export class AuthSidebarComponent {
  protected readonly brandName = 'Netptune';
}
