import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Inject,
  OnDestroy,
  OnInit,
  Optional,
} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormControl,
  FormGroup,
  Validators,
} from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { WorkspacesService } from '@core/store/workspaces/workspaces.service';
import { Workspace } from '@core/models/workspace';
import * as Actions from '@core/store/workspaces/workspaces.actions';
import { colorDictionary } from '@core/util/colors/colors';
import { Store } from '@ngrx/store';
import { Subject } from 'rxjs';
import { debounceTime, map, tap } from 'rxjs/operators';

@Component({
  selector: 'app-workspace-dialog',
  templateUrl: './workspace-dialog.component.html',
  styleUrls: ['./workspace-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspaceDialogComponent implements OnInit, OnDestroy {
  isUniqueLoading$ = new Subject<boolean>();

  onDestroy$ = new Subject();

  formGroup: FormGroup;

  colors = colorDictionary();

  get name() {
    return this.formGroup.get('name');
  }

  get identifier() {
    return this.formGroup.get('identifier');
  }

  get description() {
    return this.formGroup.get('description');
  }

  get color() {
    return this.formGroup.get('color');
  }

  get selectedColor() {
    return this.color.value;
  }

  get isEditMode() {
    return !!this.data;
  }

  constructor(
    private store: Store,
    private fb: FormBuilder,
    private cd: ChangeDetectorRef,
    private workspaceServcie: WorkspacesService,
    public dialogRef: MatDialogRef<WorkspaceDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Workspace
  ) {}

  ngOnInit() {
    this.formGroup = this.fb.group(
      {
        name: new FormControl('', {
          validators: [Validators.required],
        }),
        identifier: new FormControl('', {
          validators: [Validators.required],
          asyncValidators: this.data ? null : this.validate.bind(this),
          updateOn: 'change',
        }),
        description: new FormControl(''),
        color: new FormControl(''),
      },
      { updateOn: 'blur' }
    );

    if (this.data) {
      const workspace = this.data;

      this.name.setValue(workspace.name, { emitEvent: false });
      this.identifier.setValue(workspace.slug, { emitEvent: false });
      this.description.setValue(workspace.description, { emitEvent: false });
      this.color.setValue(workspace.metaInfo.color, { emitEvent: false });

      this.identifier.disable({ emitEvent: false });
    }
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  validate(control: AbstractControl) {
    this.isUniqueLoading$.next(true);
    return this.workspaceServcie.isSlugUnique(control.value).pipe(
      debounceTime(140),
      map((val) => {
        this.isUniqueLoading$.next(false);
        if (val?.payload?.isUnique) {
          return null;
        } else {
          return { 'already-taken': true };
        }
      }),
      tap(() => this.cd.markForCheck())
    );
  }

  getResult() {
    if (this.formGroup.pending) {
      return;
    }

    if (this.formGroup.invalid) {
      this.formGroup.markAllAsTouched();
      return;
    }

    const workspace: Workspace = {
      ...this.data,
      name: this.name.value,
      slug: this.identifier.value,
      description: this.description.value,
      metaInfo: {
        color: this.selectedColor,
      },
      users: [],
      projects: [],
    };

    if (this.isEditMode) {
      this.store.dispatch(Actions.editWorkspace({ workspace }));
    } else {
      this.store.dispatch(Actions.createWorkspace({ workspace }));
    }

    this.dialogRef.close();
  }

  getColorLabel(value: string) {
    const obj = this.colors.find((color) => color.color === value);
    return obj && obj.name;
  }
}
