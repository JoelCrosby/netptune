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
import { Workspace } from '@core/models/workspace';
import * as Actions from '@core/store/workspaces/workspaces.actions';
import { WorkspacesService } from '@core/store/workspaces/workspaces.service';
import { colorDictionary } from '@core/util/colors/colors';
import { Store } from '@ngrx/store';
import { Subject } from 'rxjs';
import {
  debounceTime,
  map,
  takeUntil,
  tap,
  withLatestFrom,
} from 'rxjs/operators';
import { toUrlSlug } from '../../../core/util/strings';

@Component({
  selector: 'app-workspace-dialog',
  templateUrl: './workspace-dialog.component.html',
  styleUrls: ['./workspace-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspaceDialogComponent implements OnInit, OnDestroy {
  isUniqueLoadingSubject$ = new Subject<boolean>();
  showIdentifierCheckSubject$ = new Subject<boolean>();

  showIdentifierCheck$ = this.showIdentifierCheckSubject$.pipe(
    withLatestFrom(this.isUniqueLoadingSubject$),
    map(([check, loading]) => !loading && check),
    debounceTime(640)
  );

  isUniqueLoading$ = this.isUniqueLoadingSubject$.pipe(debounceTime(640));

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
          updateOn: 'change',
        }),
        identifier: new FormControl('', {
          validators: [Validators.required],
          asyncValidators: this.data ? null : this.validate.bind(this),
          updateOn: 'change',
        }),
        description: new FormControl(''),
        color: new FormControl('#673AB7'),
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
    } else {
      this.name.valueChanges
        .pipe(
          takeUntil(this.onDestroy$),
          tap((value: string | undefined) => {
            if (!value) {
              this.identifier.setValue('');
              this.showIdentifierCheckSubject$.next(false);
              return;
            }

            console.log({ color: this.selectedColor });

            if (typeof value !== 'string') return;

            this.identifier.setValue(toUrlSlug(value));
          })
        )
        .subscribe();
    }
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  validate(control: AbstractControl) {
    this.isUniqueLoadingSubject$.next(true);
    return this.workspaceServcie.isSlugUnique(control.value).pipe(
      debounceTime(640),
      map((val) => {
        this.isUniqueLoadingSubject$.next(false);
        if (val?.payload?.isUnique) {
          this.showIdentifierCheckSubject$.next(true);
          return null;
        } else {
          this.showIdentifierCheckSubject$.next(false);
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
