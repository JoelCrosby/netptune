import { Component, input } from '@angular/core';
import { LucideCheck, LucideX } from '@lucide/angular';
import { DisclosureComponent } from '../disclosure.component';
import { SpinnerIconComponent } from '../spinner/spinner-icon.component';

export interface StepLogItem {
  kind: 'note' | 'step';
  text: string;
  state: 'running' | 'done' | 'failed';
}

// A run in flight and a run already finished read the same way here, so the two
// cannot drift apart the way two components did.
@Component({
  selector: 'app-step-log',
  imports: [LucideCheck, LucideX, SpinnerIconComponent, DisclosureComponent],
  host: { class: 'contents' },
  template: `
    <app-disclosure [label]="summary()" [meta]="meta()" [busy]="busy()">
      @if (items().length) {
        <div
          class="flex max-h-72 min-h-0 overflow-y-auto"
          [class]="busy() ? 'flex-col-reverse' : 'flex-col'">
          <ol class="flex min-w-0 flex-col gap-1">
            @for (item of items(); track $index) {
              @if (item.kind === 'note') {
                <li class="text-foreground/80 text-sm whitespace-pre-line">
                  {{ item.text }}
                </li>
              } @else {
                <li
                  class="font-avatar flex min-w-0 items-center gap-1.5 text-[11.5px]"
                  [class]="item.state === 'failed' ? 'text-warn' : 'text-muted'">
                  @switch (item.state) {
                    @case ('running') {
                      <app-spinner-icon class="h-3.5 w-3.5 shrink-0" />
                    }
                    @case ('done') {
                      <svg
                        lucideCheck
                        class="h-3.5 w-3.5 shrink-0"
                        aria-hidden="true"></svg>
                    }
                    @case ('failed') {
                      <svg
                        lucideX
                        class="h-3.5 w-3.5 shrink-0"
                        aria-hidden="true"></svg>
                    }
                  }
                  <span class="truncate">{{ item.text }}</span>
                  @if (item.state === 'failed') {
                    <span
                      class="shrink-0"
                      i18n="Marks a step in a list of steps that failed"
                      >(failed)</span
                    >
                  }
                </li>
              }
            }
          </ol>
        </div>
      }

      @if (current()) {
        <div class="flex min-w-0 flex-col gap-0.5">
          @if (currentLabel()) {
            <span class="text-foreground/70 text-xs font-medium">
              {{ currentLabel() }}
            </span>
          }
          <p
            class="text-muted max-h-40 min-w-0 overflow-y-auto text-xs whitespace-pre-line italic">
            {{ current() }}
          </p>
        </div>
      }
    </app-disclosure>
  `,
})
export class StepLogComponent {
  readonly summary = input.required<string>();
  readonly items = input<readonly StepLogItem[]>([]);
  readonly meta = input('');
  readonly busy = input(false);
  readonly current = input('');
  readonly currentLabel = input('');
}
