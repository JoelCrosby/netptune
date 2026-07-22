import { Component, computed, inject, input } from '@angular/core';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { colorBackgroundClass } from '@core/util/colors/colors';
import { LucideChartColumnBig } from '@lucide/angular';
import { CardHeaderImageComponent } from '@static/components/card/card-header-image.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { FromNowPipe } from '@static/pipes/from-now.pipe';

@Component({
  selector: 'app-boards-grid-card',
  providers: [FromNowPipe],
  imports: [
    CardHeaderImageComponent,
    LucideChartColumnBig,
    CardHeaderComponent,
    CardTitleComponent,
  ],
  template: ` <div
    class="bg-card-header border-border flex min-h-38 min-w-72 flex-col overflow-hidden rounded border">
    <div class="flex p-6">
      <app-card-header-image
        [class]="colorBackgroundClass(board().metaInfo.color)">
        <svg lucideChartColumnBig></svg>
      </app-card-header-image>
      <app-card-header>
        <app-card-title>{{ board().name }}</app-card-title>
      </app-card-header>
    </div>
    <div
      class="bg-card border-border mt-auto flex items-center justify-stretch gap-4 rounded border-t px-4 py-3 text-sm">
      @for (stat of stats(); track $index) {
        <div class="flex w-full flex-col items-baseline justify-center gap-1">
          <span
            class="text-muted/60 flex items-center gap-1.5 text-xs tracking-wider uppercase">
            {{ stat.label }}
          </span>
          <span
            class="text-foreground flex items-center gap-1.5 font-semibold tracking-wide">
            {{ stat.value }}
          </span>
        </div>
      }
    </div>
  </div>`,
})
export class BoardsGridCardComponent {
  readonly colorBackgroundClass = colorBackgroundClass;
  board = input.required<BoardViewModel>();
  fromNow = inject(FromNowPipe);

  stats = computed(() => {
    const data = this.board();

    return [
      {
        label: 'Tasks',
        value: data.taskCount,
      },
      {
        label: 'Modified',
        value: this.fromNow.transform(data.lastUpdated),
      },
    ];
  });
}
