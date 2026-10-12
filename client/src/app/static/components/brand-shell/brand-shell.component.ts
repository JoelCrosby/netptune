import { Component } from '@angular/core';
import { BrandLogoComponent } from '../brand-logo.component';
import { BuildNumberComponent } from '../build-number/build-number.component';

@Component({
  selector: 'app-brand-shell',
  imports: [BrandLogoComponent, BuildNumberComponent],
  host: { class: 'bg-background flex h-dvh flex-col' },
  template: `
    <header
      class="border-border flex h-15 shrink-0 items-center justify-between gap-4 border-b px-4 sm:px-6">
      <div class="flex items-center gap-3">
        <app-brand-logo size="small" />
        <span class="text-base font-semibold">{{ brandName }}</span>
      </div>

      <ng-content select="[shellActions]" />
    </header>

    <div class="relative min-h-0 flex-1 overflow-hidden">
      <ng-content select="[shellBackdrop]" />

      <main class="relative h-full overflow-y-auto">
        <div
          class="flex min-h-full items-center justify-center px-4 py-10 md:justify-start md:px-10 lg:pl-55">
          <ng-content />
        </div>
      </main>
    </div>

    <footer
      class="text-foreground/70 flex h-11 shrink-0 items-center px-4 sm:px-6">
      <app-build-number appearance="inline" />
    </footer>
  `,
})
export class BrandShellComponent {
  protected readonly brandName = 'Netptune';
}
