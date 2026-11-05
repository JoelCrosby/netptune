import { booleanAttribute, Component, input } from '@angular/core';
import { BrandLogoComponent } from '@static/components/brand-logo.component';

@Component({
  selector: 'app-flow-page',
  imports: [BrandLogoComponent],
  host: { class: 'bg-background flex h-dvh flex-col' },
  template: `
    <header
      class="border-border flex shrink-0 items-center gap-4 border-b px-7 pt-5 pb-4.5 max-md:flex-wrap max-md:gap-3 max-md:px-3 max-md:pt-3 max-md:pb-3">
      <app-brand-logo class="shrink-0" size="small" />

      <div class="min-w-0 flex-1">
        <p class="text-foreground/50 mb-0.5 text-[13px] font-medium">
          {{ eyebrow() }}
        </p>
        <h1
          class="truncate text-2xl/[30px] font-bold tracking-[-0.3px]"
          [class.text-muted]="titleMuted()">
          {{ title() }}
        </h1>
      </div>

      <div
        class="flex shrink-0 items-center gap-2 max-md:w-full max-md:*:flex-1">
        <ng-content select="[flowPageActions]" />
      </div>
    </header>

    <div
      class="flex min-h-0 flex-1 flex-col md:grid md:grid-cols-[280px_minmax(0,1fr)] lg:grid-cols-[380px_minmax(0,1fr)]">
      <nav
        class="border-border bg-foreground/2 flex flex-col overflow-y-auto border-r px-7 pt-6 pb-8 max-md:hidden md:px-4 lg:px-7"
        [attr.aria-label]="railLabel()">
        <ng-content select="[flowPageRail]" />
      </nav>

      <section class="bg-card flex min-h-0 flex-1 flex-col">
        <div class="min-h-0 flex-1 overflow-y-auto px-10 pt-7 pb-9 max-md:px-3">
          <div class="flex max-w-150 flex-col">
            <ng-content select="[flowPageNotice]" />

            <p class="text-foreground/45 mb-2 text-xs font-semibold">
              {{ position() }}
            </p>

            <div class="mb-5.5">
              <h2 class="mb-1 text-lg font-bold">{{ heading() }}</h2>
              <p
                class="text-foreground/55 text-[13px] leading-normal text-pretty">
                {{ description() }}
              </p>
            </div>

            <ng-content />
          </div>
        </div>

        <div
          class="border-border flex shrink-0 items-center gap-2.5 border-t px-10 py-3 max-md:px-3">
          <ng-content select="[flowPageFooter]" />
        </div>
      </section>
    </div>
  `,
})
export class FlowPageComponent {
  readonly eyebrow = input.required<string>();
  readonly title = input.required<string>();
  readonly titleMuted = input(false, { transform: booleanAttribute });
  readonly railLabel = input.required<string>();
  readonly position = input.required<string>();
  readonly heading = input.required<string>();
  readonly description = input.required<string>();
}
