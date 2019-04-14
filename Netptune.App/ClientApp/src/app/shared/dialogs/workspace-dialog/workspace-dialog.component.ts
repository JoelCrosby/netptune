import { Component, Inject, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Workspace } from '@app/core/models/workspace';
import { AppState } from '@app/core/core.state';
import { Store } from '@ngrx/store';
import { ActionCreateWorkspaces } from '@app/features/workspaces/store/workspaces.actions';

@Component({
  selector: 'app-workspace-dialog',
  templateUrl: './workspace-dialog.component.html',
  styleUrls: ['./workspace-dialog.component.scss'],
})
export class WorkspaceDialogComponent implements OnInit {
  public workspace: Workspace;

  public workspaceFromGroup = new FormGroup({
    nameFormControl: new FormControl('', [Validators.required]),
    discriptionFormControl: new FormControl(),
  });

  constructor(
    private store: Store<AppState>,
    public dialogRef: MatDialogRef<WorkspaceDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Workspace
  ) {}

  ngOnInit() {}

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

    this.store.dispatch(new ActionCreateWorkspaces(workspaceResult));

    this.dialogRef.close();
  }
}
