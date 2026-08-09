import { A11yModule } from '@angular/cdk/a11y';
import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import {
  AfterViewInit,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import {
  apply,
  debounce,
  disabled,
  form,
  FormField,
  submit,
} from '@angular/forms/signals';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { BoardGroupCommandsService } from '@core/services/board-group-commands.service';
import { BoardComposerService } from '@core/services/board-composer.service';
import { BoardViewService } from '@core/services/board-view.service';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { DocumentService } from '@static/services/document.service';
import { sprintResource } from '@core/resources/sprint.resource';
import { SprintFilterService } from '@core/services/sprint-filter.service';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

@Component({
  selector: 'app-board-group-task-inline',
  imports: [
    TooltipDirective,
    SpinnerComponent,
    FormField,
    A11yModule,
    CdkTextareaAutosize,
  ],
  template: `
    <div
      class="border-border bg-card overflow-hidden rounded-sm border-2 p-[0.4rem]"
      [class.opacity-60]="loading()"
      [class.border-primary]="!loading()"
      #taskInlineContainer>
      <textarea
        class="text-foreground bg-card w-full resize-none border-0 font-[inherit] text-sm tracking-[0.1px] outline-none"
        #textarea
        cdkTextareaAutosize
        cdkAutosizeMinRows="2"
        cdkAutosizeMaxRows="8"
        [formField]="taskForm.name"
        (keydown.enter)="onSubmit($event)"
        (keydown.escape)="onEscape()"
        [cdkTrapFocusAutoCapture]="true"
        [cdkTrapFocus]="true"
        i18n-placeholder="
          Placeholder in the inline box for adding a task to a board group
        "
        placeholder="What do you need to get done?"></textarea>

      <div>
        @if (message(); as message) {
          <div
            class="bg-primary h-6 w-6 rounded-full text-center leading-6 text-white"
            [appTooltip]="message">
            !
          </div>
        }

        @if (selectedSprint()) {
          <div
            class="bg-primary/12 inline-flex h-6 rounded-sm px-2 text-center text-xs leading-6 text-white">
            {{ selectedSprint()?.name }}
          </div>
        }

        @if (loading()) {
          <app-spinner diameter="1.4rem"></app-spinner>
        }
      </div>
    </div>
  `,
})
export class BoardGroupTaskInlineComponent implements AfterViewInit {
  private document = inject(DocumentService);
  private boardView = inject(BoardViewService);
  private composer = inject(BoardComposerService);
  private boardCommands = inject(BoardGroupCommandsService);
  private elementRef = inject(ElementRef);
  private inputElementRef =
    viewChild<ElementRef<HTMLTextAreaElement>>('textarea');

  readonly boardGroupId = input.required<number>();
  readonly canceled = output();

  currentWorkspace = inject(CurrentWorkspaceService).workspace;
  currentProjectId = computed(() => this.boardView.board()?.projectId);
  currentUser = inject(SessionService).currentUser;
  message = this.composer.warning;
  content = this.composer.content;
  isInlineDirty = this.composer.isDirty;
  private readonly sprintFilter = inject(SprintFilterService);
  private readonly sprintsResource = sprintResource([]);
  private readonly sprints = this.sprintsResource.value;

  selectedSprint = computed(() => {
    const sprintId = this.sprintFilter.sprintId();

    return this.sprints().find((sprint) => sprint.id === sprintId);
  });

  taskFormModel = signal({
    name: this.content() ?? '',
  });

  taskForm = form(this.taskFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Task summary is required.":Task summary`,
        maxLength: 256,
      })
    );
    disabled(schema.name, { when: () => !this.isEditActive() });
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
      this.composer.setContent(content);
    });

    effect(() => {
      const isInlineDirty = this.isInlineDirty();

      if (isInlineDirty) {
        this.loading.set(false);
        this.taskForm.name().value.set('');
        this.composer.setIsDirty(false);
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

    submit(this.taskForm, async () => {
      const name =
        this.inputElementRef()?.nativeElement.value ??
        this.taskForm.name().value();

      const task: AddProjectTaskRequest = {
        name: name.trim(),
        projectId,
        assigneeId: user.userId,
        boardGroupId: this.boardGroupId(),
      };

      this.boardCommands.createTask(task);
      this.loading.set(true);
    });
  }

  onEscape() {
    this.canceled.emit();
    this.isEditActive.set(false);
    this.loading.set(false);
  }
}
