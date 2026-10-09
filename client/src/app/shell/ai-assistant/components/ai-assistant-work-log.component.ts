import { Component, computed, input, signal } from '@angular/core';
import { AiChatEntry, AiToolStep } from '@core/models/ai-chat-entry';
import { AiTokenUsage } from '@core/models/ai-conversation';
import { formatTokens, totalTokens } from '@core/util/ai-usage';
import { formatElapsed } from '@core/util/duration';
import { numberFormat } from '@core/util/locale';
import {
  LucideCheck,
  LucideChevronDown,
  LucideSparkle,
  LucideSquare,
} from '@lucide/angular';
import { SpinnerIconComponent } from '@static/components/spinner/spinner-icon.component';

type AiStepState = 'done' | 'running' | 'cancelled' | 'unknown';

interface AiStepView {
  key: string;
  name: string;
  state: AiStepState;
  text: string;
  time: string;
}

// Below a second there is nothing worth reporting.
const MINIMUM_REPORTED_DURATION = 1000;

const stepSeconds = numberFormat({
  style: 'unit',
  unit: 'second',
  unitDisplay: 'narrow',
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
});

@Component({
  selector: 'app-ai-assistant-work-log',
  host: { class: 'block' },
  imports: [
    LucideCheck,
    LucideChevronDown,
    LucideSparkle,
    LucideSquare,
    SpinnerIconComponent,
  ],
  template: `
    @if (isVisible()) {
      <div
        class="flex flex-col overflow-hidden rounded-[10px] border"
        [class]="isLive() ? 'border-primary/35' : 'border-foreground/7'">
        @if (isLive()) {
          <div
            class="bg-foreground/3 text-foreground/70 flex h-8.5 items-center gap-2 px-2.5 text-xs"
            role="status">
            <app-spinner-icon class="h-3.5 w-3.5" />
            <span class="min-w-0 flex-1 truncate">{{ liveLabel() }}</span>
            @if (liveMeta(); as meta) {
              <span class="font-avatar text-muted text-[10.5px] tabular-nums">
                {{ meta }}
              </span>
            }
          </div>
        } @else {
          <button
            type="button"
            class="bg-foreground/3 text-muted hover:text-foreground flex h-8.5 items-center gap-2 px-2.5 text-left text-xs transition-colors disabled:cursor-default disabled:hover:text-current"
            [disabled]="steps().length === 0"
            [attr.aria-expanded]="steps().length > 0 ? isExpanded() : null"
            (click)="isExpanded.set(!isExpanded())">
            @if (entry().stopped) {
              <svg lucideSquare class="h-2.5 w-2.5 shrink-0 fill-current"></svg>
            } @else {
              <svg
                lucideSparkle
                class="text-primary h-3.5 w-3.5 shrink-0"></svg>
            }
            <span class="min-w-0 flex-1 truncate">{{ summary() }}</span>
            @if (steps().length > 0) {
              <svg
                lucideChevronDown
                class="h-3.5 w-3.5 shrink-0 transition-transform"
                [class.rotate-180]="isExpanded()"></svg>
            }
          </button>
        }

        @if (showSteps()) {
          <ul
            class="border-foreground/7 flex flex-col border-t px-2.5 pt-1.5 pb-2">
            @for (step of steps(); track step.key) {
              <li
                class="grid h-6.5 grid-cols-[14px_auto_minmax(0,1fr)_auto] items-center gap-2 text-xs">
                <span class="flex h-3 w-3 items-center justify-center">
                  @switch (step.state) {
                    @case ('done') {
                      <svg
                        lucideCheck
                        class="text-change-added h-3 w-3"
                        strokeWidth="2.6"></svg>
                    }
                    @case ('running') {
                      <app-spinner-icon class="h-3 w-3" />
                    }
                    @case ('cancelled') {
                      <svg
                        lucideSquare
                        class="text-muted h-2.5 w-2.5 fill-current"></svg>
                    }
                    @default {
                      <span
                        class="bg-foreground/25 h-1 w-1 rounded-full"></span>
                    }
                  }
                </span>
                <span
                  class="font-avatar text-foreground/70 truncate text-[11px]">
                  {{ step.name }}
                </span>
                <span
                  class="truncate"
                  [class]="
                    step.state === 'running'
                      ? 'text-foreground/70'
                      : 'text-muted'
                  ">
                  {{ step.text }}
                </span>
                <span class="font-avatar text-muted text-[10.5px] tabular-nums">
                  {{ step.time }}
                </span>
              </li>
            }
          </ul>
        }
      </div>
    }
  `,
})
export class AiAssistantWorkLogComponent {
  readonly entry = input.required<AiChatEntry>();
  readonly isLive = input(false);
  readonly isThinking = input(false);
  readonly elapsedMs = input(0);
  readonly usage = input<AiTokenUsage | null>(null);

  protected readonly isExpanded = signal(false);

  protected readonly steps = computed<AiStepView[]>(() => {
    const entry = this.entry();
    const isLive = this.isLive();
    const timed = entry.steps;

    if (timed && timed.length > 0) {
      return timed.map((step, index) => {
        const state = stepState(step, isLive);
        const durationMs = step.durationMs;

        return {
          key: `${index}-${step.name}`,
          name: step.name,
          state,
          text: stepText(state),
          time:
            durationMs === null ? '' : stepSeconds.format(durationMs / 1000),
        };
      });
    }

    return entry.tools.map((name, index) => {
      return {
        key: `${index}-${name}`,
        name,
        state: 'unknown',
        text: '',
        time: '',
      };
    });
  });

  private readonly runningStep = computed(() => {
    return this.steps().find((step) => step.state === 'running') ?? null;
  });

  private readonly durationMs = computed(() => this.entry().durationMs ?? 0);

  protected readonly isVisible = computed(() => {
    const entry = this.entry();
    const isLive = this.isLive();

    if (isLive) {
      return true;
    }

    const hasSteps = this.steps().length > 0;
    const isWorthReporting =
      !entry.failed && this.durationMs() >= MINIMUM_REPORTED_DURATION;

    return hasSteps || isWorthReporting || !!entry.stopped;
  });

  protected readonly showSteps = computed(() => {
    const hasSteps = this.steps().length > 0;

    return hasSteps && (this.isLive() || this.isExpanded());
  });

  protected readonly liveLabel = computed(() => {
    const running = this.runningStep();

    if (running) {
      return $localize`:Assistant progress while a tool runs, naming the tool:Running ${running.name}:tool:…`;
    }

    if (this.isThinking()) {
      return $localize`:Shown while the assistant is preparing its reply:Thinking…`;
    }

    return $localize`:Shown while the assistant writes out its reply:Writing the reply…`;
  });

  // A count that keeps moving is what says the turn is alive. Tokens only land
  // as the model finishes each call, so the clock carries the wait until then.
  protected readonly liveMeta = computed(() => {
    const elapsedMs = this.elapsedMs();
    const hasStarted = elapsedMs >= MINIMUM_REPORTED_DURATION;

    if (!hasStarted) {
      return null;
    }

    const elapsed = formatElapsed(elapsedMs);
    const usage = this.usage() ?? undefined;
    const hasTokens = totalTokens(usage) > 0;

    if (!hasTokens) {
      return elapsed;
    }

    const tokens = formatTokens(usage);

    return $localize`:Assistant progress while a reply is being prepared, for example "8s · 12.4k tok":${elapsed}:elapsed: · ${tokens}:tokens: tok`;
  });

  protected readonly summary = computed(() => {
    const entry = this.entry();
    const duration = formatElapsed(this.durationMs());
    const count = this.steps().length;
    const hasDuration = this.durationMs() >= MINIMUM_REPORTED_DURATION;

    if (entry.stopped) {
      return $localize`:Header of the work log of a reply the user stopped:Stopped after ${duration}:duration:`;
    }

    if (count === 0) {
      return $localize`:Shown above a finished reply, saying how long the assistant worked on it:Thought for ${duration}:duration:`;
    }

    const calls =
      count === 1
        ? $localize`:Number of tools the assistant used, singular:1 tool call`
        : $localize`:Number of tools the assistant used, plural:${count}:count: tool calls`;

    if (!hasDuration) {
      return calls;
    }

    return $localize`:Header of the work log of a finished reply, for example "Worked for 15s · 3 tool calls":Worked for ${duration}:duration: · ${calls}:calls:`;
  });
}

function stepState(step: AiToolStep, isLive: boolean): AiStepState {
  if (step.durationMs !== null) {
    return 'done';
  }

  return isLive ? 'running' : 'cancelled';
}

function stepText(state: AiStepState): string {
  if (state === 'running') {
    return $localize`:Shown beside a tool the assistant is still running:Running…`;
  }

  if (state === 'cancelled') {
    return $localize`:Shown beside a tool that was stopped before it finished:Cancelled`;
  }

  return '';
}
