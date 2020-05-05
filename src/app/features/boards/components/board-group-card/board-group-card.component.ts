import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import {
  Component,
  OnInit,
  ChangeDetectionStrategy,
  Input,
} from '@angular/core';

@Component({
  selector: 'app-board-group-card',
  templateUrl: './board-group-card.component.html',
  styleUrls: ['./board-group-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupCardComponent {
  @Input() task: TaskViewModel;
}
