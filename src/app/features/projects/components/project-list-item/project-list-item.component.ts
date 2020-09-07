import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
} from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { deleteProject } from '@core/store/projects/projects.actions';
import { HeaderAction } from '@core/types/header-action';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-project-list-item',
  templateUrl: './project-list-item.component.html',
  styleUrls: ['./project-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectListItemComponent implements OnInit {
  @Input() project: ProjectViewModel;

  actions: HeaderAction[];

  constructor(private store: Store) {}

  ngOnInit() {
    this.actions = [
      {
        label: 'Go To Board',
        isLink: true,
        icon: 'assessment',
      },
      {
        label: 'Edit Project',
        click: () => this.onEditClicked(),
      },
    ];
  }

  onEditClicked() {}

  onDeleteClicked() {
    this.store.dispatch(deleteProject({ project: this.project }));
  }
}
