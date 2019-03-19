import { Component, OnInit } from '@angular/core';
import { dropIn } from '@app/core/animations/animations';
import { Workspace } from '@app/core/models/workspace';

@Component({
  selector: 'app-workspaces',
  templateUrl: './workspaces.index.component.html',
  styleUrls: ['./workspaces.index.component.scss'],
  animations: [dropIn],
})
export class WorkspacesComponent implements OnInit {
  workspaces$ = [];

  constructor() {}

  ngOnInit() {}

  trackById(index: number, workspace: Workspace) {
    return workspace.id;
  }
}
