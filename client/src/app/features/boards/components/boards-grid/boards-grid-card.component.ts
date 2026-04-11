import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { LucideChartColumnBig } from '@lucide/angular';
import { CardHeaderImageComponent } from '@static/components/card/card-header-image.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';

@Component({
  selector: 'app-boards-grid-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardHeaderImageComponent,
    LucideChartColumnBig,
    CardHeaderComponent,
    CardTitleComponent,
    CardSubtitleComponent,
  ],
  template: ` <div
    class="bg-card border-border flex min-h-38 min-w-72 overflow-hidden rounded-sm border p-6">
    <app-card-header-image
      [style.background-color]="board().metaInfo.color || 'inherit'">
      <svg lucideChartColumnBig></svg>
    </app-card-header-image>
    <app-card-header>
      <app-card-title>{{ board().name }}</app-card-title>
      <app-card-subtitle>{{ board().identifier }}</app-card-subtitle>
    </app-card-header>
  </div>`,
})
export class BoardsGridCardComponent {
  board = input.required<BoardViewModel>();
}
