import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { AppState } from '@core/core.state';
import { TaskStatus } from '@core/enums/project-task-status';
import { Project } from '@core/models/project';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { Workspace } from '@core/models/workspace';
import { SelectCurrentProject } from '@core/state/core.selectors';
import { SelectCurrentWorkspace } from '@core/workspaces/workspaces.selectors';
import { select, Store } from '@ngrx/store';
import {
  combineLatest,
  fromEvent,
  Observable,
  Subject,
  Subscription,
} from 'rxjs';
import {
  first,
  shareReplay,
  takeUntil,
  tap,
  throttleTime,
} from 'rxjs/operators';
import { selectInlineEditActive } from '../../store/tasks.selectors';
import {
  createProjectTask,
  setInlineEditActive,
} from './../../store/tasks.actions';

@Component({
  selector: 'app-task-inline',
  templateUrl: './task-inline.component.html',
  styleUrls: ['./task-inline.component.scss'],
})
export class TaskInlineComponent implements OnInit, OnDestroy {
  @Input() status: TaskStatus = TaskStatus.New;
  @Input() siblings: TaskViewModel[];

  outSideClickListener$: Observable<Event>;

  editActive = false;

  outsideClickSubscription: Subscription;

  currentWorkspace$: Observable<Workspace>;
  currentProject$: Observable<Project>;
  inlineEditActive$: Observable<boolean>;

  taskGroup = new FormGroup({
    taskName: new FormControl(),
  });

  onDestroy$ = new Subject();

  get taskName() {
    return this.taskGroup.get('taskName');
  }

  @ViewChild('taskInlineContainer') containerElementRef: ElementRef;
  @ViewChild('taskInlineForm') formElementRef: ElementRef;
  @ViewChild('taskInput') inputElementRef: ElementRef;

  constructor(private store: Store<AppState>, private cd: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.currentWorkspace$ = this.store.pipe(select(SelectCurrentWorkspace));
    this.currentProject$ = this.store.pipe(select(SelectCurrentProject));
    this.inlineEditActive$ = this.store.pipe(
      select(selectInlineEditActive),
      shareReplay()
    );

    this.outSideClickListener$ = fromEvent(document, 'mousedown', {
      passive: true,
    }).pipe(
      takeUntil(this.onDestroy$),
      throttleTime(200),
      tap(event => {
        if (
          this.editActive &&
          !this.containerElementRef.nativeElement.contains(event.target) &&
          !this.formElementRef.nativeElement.contains(event.target)
        ) {
          this.editActive = false;
          this.store.dispatch(setInlineEditActive({ active: false }));
          this.outsideClickSubscription.unsubscribe();
          this.cd.detectChanges();
        }
      })
    );
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  addTaskClicked() {
    this.editActive = true;
    this.store.dispatch(setInlineEditActive({ active: true }));
    this.outsideClickSubscription = this.outSideClickListener$.subscribe();
    this.cd.detectChanges();
    this.inputElementRef.nativeElement.focus();
  }

  onSubmit() {
    combineLatest([this.currentWorkspace$, this.currentProject$])
      .pipe(first())
      .subscribe({
        next: ([workspace, project]) => {
          const lastSibling =
            this.siblings && this.siblings[this.siblings.length - 1];

          const order = lastSibling && lastSibling.sortOrder + 1;

          const task: AddProjectTaskRequest = {
            name: this.taskName.value,
            workspace: workspace.slug,
            projectId: project.id,
            status: this.status,
            sortOrder: order || 0,
          };

          this.store.dispatch(createProjectTask({ task }));

          this.taskGroup.reset();
        },
      });
  }
}
