import { A11yModule } from '@angular/cdk/a11y';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import {
  debounce,
  disabled,
  form,
  FormField,
  maxLength,
  required,
} from '@angular/forms/signals';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import {
  createProjectTask,
  setInlineTaskContent,
  setIsInlineDirty,
} from '@boards/store/groups/board-groups.actions';
import {
  selectBoardProjectId,
  selectCreateBoardGroupTaskMessage,
  selectInlineTaskContent,
  selectIsInlineDirty,
} from '@boards/store/groups/board-groups.selectors';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { DocumentService } from '@static/services/document.service';

@Component({
  selector: 'app-board-group-task-inline',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TooltipDirective, SpinnerComponent, FormField, A11yModule],
  template: `<div
    class="border-border bg-card overflow-hidden rounded-sm border-2 p-[0.4rem]"
    [class.opacity-60]="loading()"
    [class.border-primary]="!loading()"
    #taskInlineContainer>
    <textarea
      class="text-foreground bg-card w-full resize-none border-0 [font-family:inherit] text-sm tracking-[0.1px] outline-none"
      #textarea
      [formField]="taskForm.name"
      (keydown.enter)="onSubmit($event)"
      [cdkTrapFocusAutoCapture]="true"
      [cdkTrapFocus]="true"
      placeholder="What do you need to get done?">
    </textarea>
    <div class="h-[1.6rem] p-[0.4rem]">
      @if (message(); as message) {
        <div
          class="bg-primary h-6 w-6 rounded-full text-center leading-6 text-white"
          [appTooltip]="message">
          !
        </div>
      }

      @if (loading()) {
        <app-spinner diameter="1.4rem"> </app-spinner>
      }
    </div>
  </div> `,
})
export class BoardGroupTaskInlineComponent implements AfterViewInit {
  private document = inject(DocumentService);
  private store = inject(Store);
  private elementRef = inject(ElementRef);
  private inputElementRef = viewChild<ElementRef>('textarea');

  readonly boardGroupId = input.required<number>();
  readonly canceled = output();

  currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);
  currentProjectId = this.store.selectSignal(selectBoardProjectId);
  currentUser = this.store.selectSignal(selectCurrentUser);
  message = this.store.selectSignal(selectCreateBoardGroupTaskMessage);
  content = this.store.selectSignal(selectInlineTaskContent);
  isInlineDirty = this.store.selectSignal(selectIsInlineDirty);

  taskFormModel = signal({
    name: this.content() ?? '',
  });

  taskForm = form(this.taskFormModel, (schema) => {
    required(schema.name);
    maxLength(schema.name, 256);
    disabled(schema.name, () => !this.isEditActive());
    debounce(schema.name, 240);
  });

  isEditActive = signal(false);
  loading = signal(false);

  constructor() {
    effect(() => {
      const el = this.document.documentClicked();
      untracked(() => this.handleDocumentClick(el));
    });

    effect(() => {
      const content = this.taskForm.name().value();
      this.store.dispatch(setInlineTaskContent({ content }));
    });

    effect(() => {
      const isInlineDirty = this.isInlineDirty();

      if (isInlineDirty) {
        this.loading.set(false);
        this.taskForm.name().value.set('');
        this.store.dispatch(setIsInlineDirty({ isDirty: false }));
      }
    });
  }

  handleDocumentClick(target: EventTarget) {
    const isEditActive = this.isEditActive();
    const clickedInside = this.elementRef.nativeElement.contains(target);

    if (isEditActive && !clickedInside) {
      this.canceled.emit();
      this.isEditActive.set(false);
      this.loading.set(false);
    } else {
      this.isEditActive.set(true);
      this.focusInput();
    }
  }

  focusInput() {
    const textarea = this.inputElementRef();

    if (textarea) {
      textarea?.nativeElement.focus();
    }
  }

  ngAfterViewInit() {
    this.inputElementRef()?.nativeElement.focus();
  }

  onSubmit(event?: Event) {
    event?.preventDefault();

    const user = this.currentUser();
    const projectId = this.currentProjectId();

    if (!projectId || !user) return;

    const name = this.taskForm.name().value();

    const task: AddProjectTaskRequest = {
      name: name.trim(),
      projectId,
      assigneeId: user.userId,
      boardGroupId: this.boardGroupId(),
    };

    this.store.dispatch(createProjectTask({ task }));

    this.loading.set(true);
  }
}
