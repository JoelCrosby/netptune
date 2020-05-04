import { Component, Inject, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { colorDictionary } from '@app/core/colors/colors';
import { createWorkspace } from '@app/core/workspaces/workspaces.actions';
import { AppState } from '@core/core.state';
import { Workspace } from '@core/models/workspace';
import { editWorkspace } from '@core/workspaces/workspaces.actions';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-workspace-dialog',
  templateUrl: './workspace-dialog.component.html',
  styleUrls: ['./workspace-dialog.component.scss'],
})
export class WorkspaceDialogComponent implements OnInit {
  workspaceFromGroup = new FormGroup({
    name: new FormControl('', [Validators.required]),
    description: new FormControl(),
    color: new FormControl(''),
  });

  colors = colorDictionary();

  get name() {
    return this.workspaceFromGroup.get('name');
  }

  get description() {
    return this.workspaceFromGroup.get('description');
  }

  get color() {
    return this.workspaceFromGroup.get('color');
  }

  get selectedColor() {
    return this.color.value;
  }

  get isEditMode() {
    return !!this.data;
  }

  constructor(
    private store: Store<AppState>,
    public dialogRef: MatDialogRef<WorkspaceDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Workspace
  ) {}

  ngOnInit() {
    if (this.data) {
      const workspace = this.data;

      this.name.setValue(workspace.name);
      this.description.setValue(workspace.description);
      this.color.setValue(workspace.metaInfo.color);
    }
  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    const workspace: Workspace = {
      ...this.data,
      name: this.name.value,
      description: this.description.value,
      metaInfo: {
        color: this.selectedColor,
      },
      users: [],
      projects: [],
    };

    if (this.isEditMode) {
      this.store.dispatch(editWorkspace({ workspace }));
    } else {
      this.store.dispatch(createWorkspace({ workspace }));
    }

    this.dialogRef.close();
  }

  getColorLabel(value: string) {
    const obj = this.colors.find((color) => color.color === value);
    return obj && obj.name;
  }
}
