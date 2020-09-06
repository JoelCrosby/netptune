import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';

@Component({
  selector: 'app-project-list-item',
  templateUrl: './project-list-item.component.html',
  styleUrls: ['./project-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectListItemComponent {
  @Input() project: ProjectViewModel;
}
