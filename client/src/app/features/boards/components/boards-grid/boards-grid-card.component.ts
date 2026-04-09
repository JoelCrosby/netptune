import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { CardHeaderImageComponent } from '@static/components/card/card-header-image.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';

@Component({
  selector: 'app-boards-grid-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardHeaderImageComponent,
    MatIcon,
    CardHeaderComponent,
    CardTitleComponent,
    CardSubtitleComponent,
  ],
  template: ` <div
    class="bg-card border-border flex min-h-38 min-w-72 overflow-hidden rounded-sm border p-6">
    <app-card-header-image
      [style.background-color]="board().metaInfo.color || 'inherit'">
      <mat-icon class="material-icons-outlined"> table_chart </mat-icon>
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
