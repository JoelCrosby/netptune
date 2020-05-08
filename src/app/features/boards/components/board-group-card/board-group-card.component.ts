import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';

@Component({
  selector: 'app-board-group-card',
  templateUrl: './board-group-card.component.html',
  styleUrls: ['./board-group-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupCardComponent {
  @Input() task: TaskViewModel;
}
