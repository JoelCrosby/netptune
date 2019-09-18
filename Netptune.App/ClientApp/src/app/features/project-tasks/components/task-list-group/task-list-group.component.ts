import { Component, OnInit, Input } from '@angular/core';
import { ProjectTaskDto } from '@core/models/view-models/project-task-dto';
import { Observable } from 'rxjs';
import { fadeIn, dropIn } from '@core/animations/animations';

@Component({
  selector: 'app-task-list-group',
  templateUrl: './task-list-group.component.html',
  styleUrls: ['./task-list-group.component.scss'],
  animations: [fadeIn, dropIn],
})
export class TaskListGroupComponent implements OnInit {
  @Input() groupName: string;
  @Input() tasks: Observable<ProjectTaskDto[] | undefined>;
  @Input() header: string;
  @Input() emptyMessage: string;
  @Input() loaded: Observable<boolean>;

  constructor() {}

  ngOnInit() {}
}
