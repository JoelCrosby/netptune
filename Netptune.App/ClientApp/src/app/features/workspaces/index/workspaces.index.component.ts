import { Component, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { dropIn } from '@app/core/animations/animations';
import { Workspace } from '@app/core/models/workspace';
import { Maybe } from '@app/core/types/nothing';

@Component({
  selector: 'app-workspaces',
  templateUrl: './workspaces.index.component.html',
  styleUrls: ['./workspaces.index.component.scss'],
  animations: [dropIn],
})
export class WorkspacesComponent implements OnInit {
  selectedWorkspace: Maybe<Workspace>;

  constructor(public snackBar: MatSnackBar, public dialog: MatDialog) {}

  ngOnInit() {}

  trackById(index: number, workspace: Workspace) {
    return workspace.id;
  }
}
