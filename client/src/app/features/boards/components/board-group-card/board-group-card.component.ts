import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { Selected } from '@core/models/selected';
import { BoardViewTask } from '@core/models/view-models/board-view';

@Component({
  selector: 'app-board-group-card',
  templateUrl: './board-group-card.component.html',
  styleUrls: ['./board-group-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupCardComponent {
  @Input() task!: Selected<BoardViewTask>;
  @Input() groupId!: number;

  trackByTag(_: number, tag: string) {
    return tag;
  }
}
