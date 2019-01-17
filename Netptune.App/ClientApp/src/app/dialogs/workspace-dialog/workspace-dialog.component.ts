import { Component, Inject, OnInit, Optional } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Workspace } from '../../models/workspace';
import { FormControl, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-workspace-dialog',
  templateUrl: './workspace-dialog.component.html',
  styleUrls: ['./workspace-dialog.component.scss']
})
export class WorkspaceDialogComponent implements OnInit {

  public workspace: Workspace;

  public workspaceFromGroup = new FormGroup({
    nameFormControl: new FormControl('', [
      Validators.required,
    ]),
    discriptionFormControl: new FormControl('', [
    ])
  });

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
      workspaceResult.id = this.workspace.id;
    }

    workspaceResult.name = this.workspaceFromGroup.controls['nameFormControl'].value;
    workspaceResult.description = this.workspaceFromGroup.controls['discriptionFormControl'].value;

    return workspaceResult;
  }

}
