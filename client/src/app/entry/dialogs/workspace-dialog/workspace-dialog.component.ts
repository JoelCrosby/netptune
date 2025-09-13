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
  FormControl,
  FormGroup,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { DialogRef, DIALOG_DATA } from '@angular/cdk/dialog';
import { AddWorkspaceRequest } from '@core/models/requests/add-workspace-request';
import { UpdateWorkspaceRequest } from '@core/models/requests/update-workspace-request';
import { Workspace } from '@core/models/workspace';
import * as Actions from '@core/store/workspaces/workspaces.actions';
import { WorkspacesService } from '@core/store/workspaces/workspaces.service';
import { colorDictionary } from '@core/util/colors/colors';
import { toUrlSlug } from '@core/util/strings';
import { Store } from '@ngrx/store';
import {
  animationFrameScheduler,
  combineLatest,
  Observable,
  Subject,
} from 'rxjs';
import {
  debounceTime,
  map,
  observeOn,
  takeUntil,
  tap,
  withLatestFrom,
} from 'rxjs/operators';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { MatButton } from '@angular/material/button';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-workspace-dialog',
  templateUrl: './workspace-dialog.component.html',
  styleUrls: ['./workspace-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    FormInputComponent,
    FormTextAreaComponent,
    ColorSelectComponent,
    DialogActionsDirective,
    MatButton,
    DialogCloseDirective,
    AsyncPipe,
  ],
})
export class WorkspaceDialogComponent implements OnInit, OnDestroy {
  isUniqueLoadingSubject$ = new Subject<boolean>();
  showIdentifierCheckSubject$ = new Subject<boolean>();
  identifierIcon$!: Observable<string | null>;

  showIdentifierCheck$ = this.showIdentifierCheckSubject$.pipe(
    withLatestFrom(this.isUniqueLoadingSubject$),
    map(([check, loading]) => !loading && check),
    debounceTime(640)
  );

  isUniqueLoading$ = this.isUniqueLoadingSubject$.pipe(debounceTime(640));

  onDestroy$ = new Subject<void>();

  formGroup = new FormGroup(
    {
      name: new FormControl('', {
        validators: [Validators.required],
        updateOn: 'change',
        nonNullable: true,
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

  colors = colorDictionary();

  get name() {
    return this.formGroup.controls.name;
  }

  get identifier() {
    return this.formGroup.controls.identifier;
  }

  get description() {
    return this.formGroup.controls.description;
  }

  get color() {
    return this.formGroup.controls.color;
  }

  get selectedColor() {
    return this.color.value;
  }

  get isEditMode() {
    return !!this.data;
  }

  constructor(
    private store: Store,
    private cd: ChangeDetectorRef,
    private workspaceServcie: WorkspacesService,
    public dialogRef: DialogRef<WorkspaceDialogComponent>,
    @Optional() @Inject(DIALOG_DATA) public data: Workspace
  ) {}

  ngOnInit() {
    this.identifierIcon$ = combineLatest([
      this.isUniqueLoading$.pipe(),
      this.identifier.statusChanges,
    ]).pipe(
      map(([loading]) => {
        if (loading) return null;

        if (this.identifier?.valid) {
          return 'check';
        }

        return '';
      })
    );

    if (this.data && this.data.slug) {
      const workspace = this.data as Required<Workspace>;

      this.name.setValue(workspace.name, { emitEvent: false });
      this.identifier.setValue(workspace.slug, { emitEvent: false });
      this.description.setValue(workspace.description, { emitEvent: false });
      this.color.setValue(workspace.metaInfo?.color ?? null, {
        emitEvent: false,
      });

      this.identifier.disable({ emitEvent: false });
    } else {
      this.name.valueChanges
        .pipe(
          takeUntil(this.onDestroy$),
          observeOn(animationFrameScheduler),
          tap((value: string | undefined) => {
            if (!value) {
              this.identifier.setValue('');
              this.showIdentifierCheckSubject$.next(false);
              return;
            }

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
    return this.workspaceServcie.isSlugUnique(control.value as string).pipe(
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

    if (this.isEditMode) {
      const request: UpdateWorkspaceRequest = {
        name: this.name.value,
        slug: this.identifier.value as string,
        description: this.description.value as string,
        metaInfo: {
          color: this.selectedColor as string,
        },
      };

      this.store.dispatch(Actions.editWorkspace({ request }));
    } else {
      const request: AddWorkspaceRequest = {
        name: this.name.value,
        slug: this.identifier.value as string,
        description: this.description.value as string,
        metaInfo: {
          color: this.selectedColor as string,
        },
      };

      this.store.dispatch(Actions.createWorkspace({ request }));
    }

    this.dialogRef.close();
  }

  getColorLabel(value: string) {
    const obj = this.colors.find((color) => color.color === value);
    return obj && obj.name;
  }
}
