import { Component, input } from '@angular/core';
import { type LucideIconInput } from '@lucide/angular';
import { IconTileComponent } from '../icon-tile.component';

@Component({
  selector: 'app-chart-card',
  imports: [IconTileComponent],
  host: { class: 'block h-full' },
  template: `
    <section
      class="border-border bg-card flex h-full min-h-24 flex-col overflow-hidden rounded-lg border shadow-sm">
      <header
        class="border-border flex shrink-0 flex-wrap items-center justify-between gap-x-4 gap-y-2 border-b px-6 py-5">
        <div class="flex min-w-0 items-center gap-3">
          <app-icon-tile [icon]="icon()" />

          <div class="min-w-0">
            <h3 class="font-overpass truncate text-base font-semibold">
              {{ title() }}
            </h3>
            @if (description()) {
              <p class="text-muted truncate text-sm">{{ description() }}</p>
            }
          </div>
        </div>

        <div class="flex shrink-0 items-center gap-3 empty:hidden">
          <ng-content select="[chartCardActions]" />
        </div>
      </header>

      <div class="flex flex-1 flex-col justify-center px-6 py-5">
        <ng-content />
      </div>
    </section>
  `,
})
export class ChartCardComponent {
  readonly icon = input.required<LucideIconInput>();
  readonly title = input.required<string>();
  readonly description = input('');
}
