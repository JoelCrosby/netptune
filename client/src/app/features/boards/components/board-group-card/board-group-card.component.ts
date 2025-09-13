import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Selected } from '@core/models/selected';
import {
  AssigneeViewModel,
  BoardViewTask,
} from '@core/models/view-models/board-view';
import { CardComponent } from '@static/components/card/card.component';

import { MatIcon } from '@angular/material/icon';
import { AvatarComponent } from '@static/components/avatar/avatar.component';

@Component({
  selector: 'app-board-group-card',
  templateUrl: './board-group-card.component.html',
  styleUrls: ['./board-group-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent, MatIcon, AvatarComponent],
})
export class BoardGroupCardComponent {
  readonly task = input.required<Selected<BoardViewTask>>();
  readonly groupId = input.required<number>();

  trackByTag(_: number, tag: string) {
    return tag;
  }

  trackById(_: number, item: AssigneeViewModel) {
    return item.id;
  }
}
