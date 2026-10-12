import { Component } from '@angular/core';
import { BrandLogoComponent } from '@static/components/brand-logo.component';
import { BuildNumberComponent } from '@static/components/build-number/build-number.component';
import { PlanetBackdropComponent } from '@static/components/planet-backdrop/planet-backdrop.component';

@Component({
  selector: 'app-auth-sidebar',
  imports: [BrandLogoComponent, BuildNumberComponent, PlanetBackdropComponent],
  host: {
    class:
      'bg-side-bar border-side-bar-border relative flex flex-col overflow-hidden border-r p-10 text-white',
  },
  template: `
    <app-planet-backdrop />

    <div class="relative z-1 flex items-center gap-3.5">
      <app-brand-logo size="small" />
      <span class="text-[15px] font-semibold tracking-[.225px]">
        {{ brandName }}
      </span>
    </div>

    <app-build-number class="relative z-1 mt-auto" appearance="inline" />
  `,
})
export class AuthSidebarComponent {
  protected readonly brandName = 'Netptune';
}
