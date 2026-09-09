import { Component, computed, input, model } from '@angular/core';
import { LucideChevronDown } from '@lucide/angular';

@Component({
  selector: 'app-automation-setup-strip',
  imports: [LucideChevronDown],
  host: { class: 'border-border bg-card block rounded-lg border' },
  template: `
    <div class="flex items-center gap-4 px-4 py-3">
      <div
        class="flex min-w-0 flex-1 flex-wrap items-baseline gap-x-2.5 gap-y-1">
        <span class="text-[0.9375rem] font-semibold">{{ nameLabel() }}</span>

        <span class="text-foreground/55 text-[13px]">
          <span
            i18n="
              Names the service account an automation runs as. ACCOUNT is that
              account
            ">
            runs as
            {{
              runAs() // i18n(ph="ACCOUNT")
            }}
          </span>
        </span>

        <span class="text-foreground/25" aria-hidden="true">·</span>
        <span class="text-foreground/55 text-[13px]">{{ scope() }}</span>

        <span class="text-foreground/25" aria-hidden="true">·</span>
        <span class="text-foreground/55 text-[13px]">{{ enabledLabel() }}</span>
      </div>

      <button
        type="button"
        class="text-primary hover:bg-primary/8 focus-visible:ring-primary inline-flex h-8 shrink-0 items-center gap-1.5 rounded-lg px-3 text-[13px] font-semibold transition-colors focus-visible:ring-2 focus-visible:outline-none"
        [attr.aria-expanded]="open()"
        (click)="open.set(!open())">
        <span>{{ toggleLabel() }}</span>
        <svg
          lucideChevronDown
          class="h-3.5 w-3.5 transition-transform"
          [class.rotate-180]="open()"></svg>
      </button>
    </div>

    @if (open()) {
      <div class="border-border bg-foreground/2 border-t p-4">
        <ng-content />
      </div>
    }
  `,
})
export class AutomationSetupStripComponent {
  readonly name = input('');
  readonly runAs = input('');
  readonly scope = input('');
  readonly isEnabled = input(true);
  readonly open = model(false);

  protected readonly nameLabel = computed(() => {
    return (
      this.name().trim() ||
      $localize`:Stands in for an automation that has not been named yet:Untitled automation`
    );
  });

  protected readonly enabledLabel = computed(() => {
    return this.isEnabled()
      ? $localize`:Marks an automation that is switched on:Enabled`
      : $localize`:Marks an automation that is switched off:Disabled`;
  });

  protected readonly toggleLabel = computed(() => {
    return this.open()
      ? $localize`:Hides the automation setup fields:Hide setup`
      : $localize`:Reveals the automation setup fields:Edit setup`;
  });
}
