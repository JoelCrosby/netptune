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
          class="flex max-h-96 min-h-0 overflow-y-auto"
          [class]="busy() ? 'flex-col-reverse' : 'flex-col'">
          <ol class="flex min-w-0 flex-col gap-2.5">
            @for (item of items(); track $index) {
              @if (item.kind === 'note') {
                <li
                  class="border-border bg-foreground/3 text-foreground/80 rounded-lg border px-3 py-2.5 text-[13.5px]/[1.5] whitespace-pre-line">
                  {{ item.text }}
                </li>
              } @else {
                <li
                  class="flex min-w-0 items-center gap-2.5 text-[13px]"
                  [class]="stepClass(item.state)">
                  <span
                    class="flex h-5 w-5 shrink-0 items-center justify-center rounded-full ring-1"
                    [class]="markerClass(item.state)">
                    @switch (item.state) {
                      @case ('running') {
                        <app-spinner-icon class="h-3 w-3" />
                      }
                      @case ('done') {
                        <svg
                          lucideCheck
                          class="h-3 w-3"
                          strokeWidth="2.6"
                          aria-hidden="true"></svg>
                      }
                      @case ('failed') {
                        <svg
                          lucideX
                          class="h-3 w-3"
                          strokeWidth="2.6"
                          aria-hidden="true"></svg>
                      }
                    }
                  </span>
                  <span class="font-avatar min-w-0 truncate">
                    {{ item.text }}
                  </span>
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
        <div
          class="border-border bg-foreground/3 flex min-w-0 flex-col gap-1.5 rounded-lg border px-3 py-2.5">
          @if (currentLabel()) {
            <span class="text-muted text-xs font-semibold">
              {{ currentLabel() }}
            </span>
          }
          <p
            class="text-foreground/70 max-h-44 min-w-0 overflow-y-auto text-[13.5px]/[1.5] whitespace-pre-line italic">
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

  protected stepClass(state: StepLogItem['state']): string {
    return state === 'failed' ? 'text-warn' : 'text-muted';
  }

  protected markerClass(state: StepLogItem['state']): string {
    return state === 'failed' ? 'ring-warn/35 bg-warn/6' : 'ring-border';
  }
}
