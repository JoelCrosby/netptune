import { booleanAttribute, Component, input } from '@angular/core';
import { type LucideIconInput } from '@lucide/angular';
import { IconTileComponent } from '../icon-tile.component';
import { type Tone } from '../tones';

@Component({
  selector: 'app-chart-card',
  imports: [IconTileComponent],
  host: { class: 'block h-full' },
  template: `
    <section
      class="border-border bg-card flex h-full min-h-24 flex-col rounded-lg border shadow-sm"
      [class.overflow-hidden]="clipContent()">
      <header
        class="border-border flex shrink-0 flex-wrap items-center justify-between gap-x-4 gap-y-2 border-b px-6 py-5"
        [class.bg-card-header]="headerFill()">
        <div class="flex min-w-0 items-center gap-3">
          <app-icon-tile [icon]="icon()" [tone]="tone()" />

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

      <div
        class="flex flex-1 flex-col"
        [class.justify-center]="!flush()"
        [class.px-6]="!flush()"
        [class.py-5]="!flush()">
        <ng-content />
      </div>

      <footer
        class="border-border flex shrink-0 items-center gap-3 border-t px-4 py-2 empty:hidden"
        [class.hidden]="!footer()">
        <ng-content select="[chartCardFooter]" />
      </footer>
    </section>
  `,
})
export class ChartCardComponent {
  readonly icon = input.required<LucideIconInput>();
  readonly title = input.required<string>();
  readonly description = input('');
  readonly clipContent = input(true);
  readonly flush = input(false, { transform: booleanAttribute });
  readonly tone = input<Tone>('purple');
  readonly headerFill = input(false, { transform: booleanAttribute });
  readonly footer = input(false, { transform: booleanAttribute });
}
