import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { Selected } from '@core/models/selected';
import { BoardViewTask } from '@core/models/view-models/board-view';
import { CardComponent } from '@static/components/card/card.component';

import { LucideCheck, LucideFlag } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-board-group-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    AvatarComponent,
    TaskScopeIdComponent,
    LucideFlag,
    LucideCheck,
    NgClass,
  ],
  template: `<app-card
    class="mb-[.3rem] flex cursor-pointer flex-col items-start overflow-hidden p-2! text-[14px] tracking-[.1px]"
    [ngClass]="flaggedClasses()"
    [class.selected]="task().selected">
    <div class="mb-0 leading-[1.4rem]">{{ task().name }}</div>

    <div class="mt-4 flex flex-row flex-wrap">
      @for (tag of task().tags; track tag) {
        <div
          class="bg-primary/10 my-[.2rem] mr-[.2rem] ml-0 rounded-[4px] px-[.4rem] py-[.2rem]">
          {{ tag }}
        </div>
      }
    </div>

    <div class="mt-2 flex w-full flex-row items-center justify-between">
      <div class="flex items-center gap-2">
        <app-task-scope-id [id]="task().systemId" />

        @if (task().status === 1) {
          <svg lucideCheck class="text-green-500">done</svg>
        }
      </div>

      <div class="flex items-center gap-4">
        @if (task().isFlagged) {
          <svg lucideFlag class="fill-red-500 text-red-500" size="20px"></svg>
        }
        @for (assignee of task().assignees; track assignee.id) {
          <app-avatar
            size="24"
            class="task-card-user-chip"
            [name]="assignee.displayName"
            [imageUrl]="assignee.pictureUrl">
          </app-avatar>
        }
      </div>
    </div>
  </app-card> `,
})
export class BoardGroupCardComponent {
  readonly task = input.required<Selected<BoardViewTask>>();
  readonly groupId = input.required<number>();

  flaggedClasses = computed(() => ({
    'bg-red-700/10 border-red-900/20': this.task().isFlagged,
  }));
}
