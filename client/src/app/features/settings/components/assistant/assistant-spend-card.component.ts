import { Component, computed, inject, input, output } from '@angular/core';
import { AiSpend, AiSpendMember } from '@core/models/ai-spend';
import { DialogService } from '@core/services/dialog.service';
import { formatCurrency, formatTokens } from '@core/util/ai-usage';
import { dateTimeFormat } from '@core/util/locale';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { first } from 'rxjs';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import {
  AssistantSpendCapDialogComponent,
  AssistantSpendCapDialogData,
} from './assistant-spend-cap-dialog.component';

interface SpendBar {
  day: string;
  height: string;
  isPeak: boolean;
  label: string;
}

interface SpendMemberRow {
  userId: string;
  initials: string;
  name: string;
  conversations: string;
  tokens: string;
  cost: string;
}

const dayFormat = dateTimeFormat({ day: 'numeric', month: 'short' });
const resetFormat = dateTimeFormat({ day: 'numeric', month: 'long' });

@Component({
  selector: 'app-assistant-spend-card',
  imports: [PanelComponent, PanelHeaderComponent, StrokedButtonComponent],
  host: { class: 'block' },
  template: `
    <section app-panel surface="card">
      <app-panel-header
        density="comfortable"
        i18n-heading="Heading of the assistant spend card"
        heading="Spend this month">
        <p panelHeading class="text-muted mt-1 text-sm">
          <span i18n="Explains how assistant cost is worked out">
            Estimated from published model rates.
          </span>
          {{ resetsLabel() }}
        </p>

        <div panelHeaderActions>
          @if (canEditCap()) {
            <button
              app-stroked-button
              color="neutral"
              type="button"
              class="h-8 shrink-0 px-3 text-xs"
              (click)="editCap()">
              @if (spend()?.cap) {
                <span i18n="Button that changes the assistant spend cap"
                  >Edit cap</span
                >
              } @else {
                <span i18n="Button that sets an assistant spend cap"
                  >Set a cap</span
                >
              }
            </button>
          }
        </div>
      </app-panel-header>

      <div
        class="border-border flex flex-col gap-4 px-6 py-5"
        [class.border-b]="members().length">
        <div class="flex flex-wrap items-baseline gap-x-3 gap-y-1">
          <span
            class="font-overpass text-3xl font-semibold tabular-nums"
            [class.text-warn]="isNearCap()">
            {{ monthToDateLabel() }}
          </span>
          <span class="text-muted text-sm">{{ capLabel() }}</span>
        </div>

        @if (capPercent(); as percent) {
          <div class="bg-foreground/10 h-2 overflow-hidden rounded-full">
            <div
              class="h-full rounded-full"
              [class]="isNearCap() ? 'bg-warn' : 'bg-primary'"
              [style.width]="percent"></div>
          </div>
        }

        @if (hasActivity()) {
          <div class="flex h-16 items-end gap-1" role="presentation">
            @for (bar of bars(); track bar.day) {
              <span
                class="block max-w-10 flex-1 rounded-t-sm"
                [class]="barClass(bar)"
                [style.height]="bar.height"
                [title]="bar.label"></span>
            }
          </div>

          <div class="flex flex-wrap items-center justify-between gap-2">
            <span class="text-muted text-xs">{{ peakLabel() }}</span>
            <span class="text-muted text-xs">
              <span i18n="Label for the projected end-of-month assistant spend">
                Projected month end
              </span>
              <span
                class="ml-1 font-semibold tabular-nums"
                [class]="projectionClass()">
                {{ projectionLabel() }}
              </span>
            </span>
          </div>
        }

        <p class="text-muted text-sm">{{ summaryLabel() }}</p>
      </div>

      @if (members().length) {
        <div class="py-1">
          <div
            class="text-muted grid grid-cols-[minmax(0,1fr)_6rem_5rem_5rem] gap-4 px-6 py-2 text-xs font-semibold tracking-wide uppercase">
            <span i18n="Column heading for a workspace member">Member</span>
            <span i18n="Column heading counting assistant conversations"
              >Chats</span
            >
            <span i18n="Column heading counting assistant tokens">Tokens</span>
            <span class="text-right" i18n="Column heading for assistant cost"
              >Cost</span
            >
          </div>

          @for (member of members(); track member.userId) {
            <div
              class="border-border/40 grid grid-cols-[minmax(0,1fr)_6rem_5rem_5rem] items-center gap-4 border-t px-6 py-2.5">
              <div class="flex min-w-0 items-center gap-2.5">
                <span
                  class="bg-foreground/10 text-muted flex h-6 w-6 shrink-0 items-center justify-center rounded-full font-mono text-[10px]"
                  aria-hidden="true">
                  {{ member.initials }}
                </span>
                <span class="truncate text-sm">{{ member.name }}</span>
              </div>
              <span class="text-muted text-sm tabular-nums">
                {{ member.conversations }}
              </span>
              <span class="text-muted text-sm tabular-nums">
                {{ member.tokens }}
              </span>
              <span class="text-right text-sm font-semibold tabular-nums">
                {{ member.cost }}
              </span>
            </div>
          }
        </div>
      }
    </section>
  `,
})
export class AssistantSpendCardComponent {
  readonly spend = input.required<AiSpend | null>();
  readonly canEditCap = input.required<boolean>();

  readonly capChanged = output();

  private readonly dialog = inject(DialogService);

  protected readonly hasActivity = computed(() => {
    const spend = this.spend();

    return !!spend && (spend.monthToDate > 0 || spend.members.length > 0);
  });

  protected readonly monthToDateLabel = computed(() => {
    return formatCurrency(this.spend()?.monthToDate ?? 0);
  });

  protected readonly capPercent = computed(() => {
    const spend = this.spend();
    const cap = spend?.cap;

    if (!spend || !cap) {
      return null;
    }

    return `${Math.min(100, (spend.monthToDate / cap) * 100)}%`;
  });

  protected readonly isNearCap = computed(() => {
    const spend = this.spend();
    const cap = spend?.cap;

    if (!spend || !cap) {
      return false;
    }

    return spend.monthToDate / cap >= 0.9;
  });

  protected readonly capLabel = computed(() => {
    const spend = this.spend();
    const cap = spend?.cap;

    if (!spend || !cap) {
      return $localize`:Says the workspace has no assistant spend cap:No cap set`;
    }

    const percent = Math.round((spend.monthToDate / cap) * 100);

    return $localize`:Compares assistant spend with the cap:of ${formatCurrency(cap)}:cap: cap · ${percent}:percent:%`;
  });

  protected readonly projectionLabel = computed(() => {
    return formatCurrency(this.spend()?.projected ?? 0);
  });

  protected readonly projectionClass = computed(() => {
    const spend = this.spend();
    const cap = spend?.cap;
    const overshoots = !!spend && !!cap && spend.projected > cap;

    return overshoots ? 'text-warn' : '';
  });

  protected readonly resetsLabel = computed(() => {
    const spend = this.spend();

    if (!spend) {
      return '';
    }

    const resets = resetFormat.format(new Date(spend.periodEnd));

    return $localize`:Says when the assistant spend period restarts:Resets ${resets}:date:.`;
  });

  protected readonly summaryLabel = computed(() => {
    const spend = this.spend();

    if (!spend) {
      return '';
    }

    const tokens = formatTokens(spend.usage);

    return $localize`:Summarises assistant use for the month:${spend.conversations}:conversations: conversations · ${tokens}:tokens: tokens. Nothing is charged by Netptune — this is your provider bill.`;
  });

  protected readonly bars = computed<SpendBar[]>(() => {
    const daily = this.spend()?.daily ?? [];
    const peak = Math.max(...daily.map((day) => day.cost), 0);

    return daily.map((day) => {
      const height = peak > 0 ? Math.max(2, (day.cost / peak) * 100) : 2;
      const date = dayFormat.format(new Date(day.day));

      return {
        day: day.day,
        height: `${height}%`,
        isPeak: peak > 0 && day.cost === peak,
        label: `${date} · ${formatCurrency(day.cost)}`,
      };
    });
  });

  protected readonly peakLabel = computed(() => {
    const bars = this.bars();
    const peak = bars.find((bar) => bar.isPeak);

    if (!peak) {
      return $localize`:Caption for the daily assistant spend chart:Daily estimate this month`;
    }

    return $localize`:Caption naming the busiest day of assistant spend:Daily estimate this month · peak ${peak.label}:peak:`;
  });

  protected readonly members = computed<SpendMemberRow[]>(() => {
    const members = this.spend()?.members ?? [];

    return members.map((member) => this.toMemberRow(member));
  });

  protected barClass(bar: SpendBar): string {
    if (this.isNearCap()) {
      return bar.isPeak ? 'bg-warn' : 'bg-warn/35';
    }

    return bar.isPeak ? 'bg-primary' : 'bg-primary/35';
  }

  editCap() {
    const spend = this.spend();
    const data: AssistantSpendCapDialogData = {
      cap: spend?.cap ?? null,
      monthToDate: spend?.monthToDate ?? 0,
    };

    this.dialog
      .open<boolean, AssistantSpendCapDialogData>(
        AssistantSpendCapDialogComponent,
        { width: '480px', data }
      )
      .closed.pipe(first())
      .subscribe((changed) => {
        if (changed) this.capChanged.emit();
      });
  }

  private toMemberRow(member: AiSpendMember): SpendMemberRow {
    return {
      userId: member.userId,
      initials: initialsOf(member.userDisplayName),
      name: member.userDisplayName,
      conversations: `${member.conversations}`,
      tokens: formatTokens(member.usage),
      cost: formatCurrency(member.usage.cost),
    };
  }
}

function initialsOf(displayName: string): string {
  const parts = displayName.trim().split(/\s+/).filter(Boolean);

  if (parts.length === 0) {
    return '?';
  }

  const first = parts[0][0];
  const last = parts.length > 1 ? parts[parts.length - 1][0] : '';

  return `${first}${last}`.toUpperCase();
}
