import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { Selected } from '@core/models/selected';
import {
  AssigneeViewModel,
  BoardViewTask,
} from '@core/models/view-models/board-view';
import { CardComponent } from '../../../../static/components/card/card.component';
import { NgFor, NgIf } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { AvatarComponent } from '../../../../static/components/avatar/avatar.component';

@Component({
    selector: 'app-board-group-card',
    templateUrl: './board-group-card.component.html',
    styleUrls: ['./board-group-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CardComponent, NgFor, NgIf, MatIcon, AvatarComponent]
})
export class BoardGroupCardComponent {
  @Input() task!: Selected<BoardViewTask>;
  @Input() groupId!: number;

  trackByTag(_: number, tag: string) {
    return tag;
  }

  trackById(_: number, item: AssigneeViewModel) {
    return item.id;
  }
}
