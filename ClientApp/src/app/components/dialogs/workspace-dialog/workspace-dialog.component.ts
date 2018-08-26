import { Component, OnInit, Optional, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Workspace } from '../../../models/workspace';
import { WorkspaceService } from '../../../services/workspace/workspace.service';

@Component({
  selector: 'app-workspace-dialog',
  templateUrl: './workspace-dialog.component.html',
  styleUrls: ['./workspace-dialog.component.scss']
})
export class WorkspaceDialogComponent implements OnInit {

  public workspace: Workspace;

  public selectedName: string;
  public selectedDescription: string;

  constructor(
    public dialogRef: MatDialogRef<WorkspaceDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Workspace) { }

  ngOnInit() {

  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    const workspaceResult = new Workspace();

    if (this.workspace) {
      workspaceResult.workspaceId = this.workspace.workspaceId;
    }

    workspaceResult.name = this.selectedName;
    workspaceResult.description = this.selectedDescription;

    return workspaceResult;
  }

}
