import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  templateUrl: './project-detail-view.component.html',
  styleUrls: ['./project-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectDetailViewComponent {}
