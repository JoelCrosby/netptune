import { Component, input } from '@angular/core';
import { type LucideIconInput } from '@lucide/angular';
import { IconCircleComponent } from './icon-circle.component';

@Component({
  selector: 'app-panel-header',
  imports: [IconCircleComponent],
  template: `
    <header
      class="border-border bg-foreground/3 flex flex-wrap items-center justify-between gap-3 border-b px-4 py-3">
      <div class="flex min-w-0 items-center gap-3">
        <app-icon-circle [icon]="icon()" />
        <div class="min-w-0">
          <h2 class="text-sm font-medium">{{ heading() }}</h2>
          @if (description()) {
            <p class="text-foreground/60 truncate text-xs">
              {{ description() }}
            </p>
          }
        </div>
      </div>

      <div class="shrink-0 empty:hidden">
        <ng-content select="[panelHeaderActions]" />
      </div>
    </header>
  `,
  styles: ``,
})
export class PanelHeaderComponent {
  readonly heading = input.required<string>();
  readonly description = input('');
  readonly icon = input.required<LucideIconInput>();
}
