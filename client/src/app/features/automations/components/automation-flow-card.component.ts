import { Component, input } from '@angular/core';
import { type LucideIconInput } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { panelSurfaceClass } from '@static/components/panel.component';

@Component({
  selector: 'app-automation-flow-card',
  imports: [BadgeComponent, IconTileComponent],
  host: {
    class: `${panelSurfaceClass} bg-card`,
  },
  template: `
    <header
      class="border-border flex flex-wrap items-center justify-between gap-x-4 gap-y-2 border-b px-4 py-3.5">
      <div class="flex min-w-0 flex-1 items-center gap-3">
        <app-icon-tile [icon]="icon()" />

        @if (heading(); as headingText) {
          <div class="min-w-0">
            <h3 class="font-overpass truncate text-base font-semibold">
              {{ headingText }}
            </h3>
            @if (description(); as descriptionText) {
              <p class="text-muted truncate text-sm">{{ descriptionText }}</p>
            }
          </div>
        }

        <div class="flex min-w-0 items-center gap-3 empty:hidden">
          <ng-content select="[flowCardHeader]" />
        </div>
      </div>

      <div class="flex shrink-0 items-center gap-2.5">
        <app-badge
          color="primary"
          class="text-[0.65rem] font-bold tracking-wider">
          {{ keyword() }}
        </app-badge>

        <ng-content select="[flowCardActions]" />
      </div>
    </header>

    <div class="flex min-w-0 flex-col gap-3.5 p-4">
      <ng-content />
    </div>
  `,
})
export class AutomationFlowCardComponent {
  readonly icon = input.required<LucideIconInput>();
  readonly keyword = input.required<string>();
  readonly heading = input<string | null>(null);
  readonly description = input<string | null>(null);
}
