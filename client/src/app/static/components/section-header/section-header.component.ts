import { Component, computed, input } from '@angular/core';

export type SectionHeaderAppearance = 'default' | 'inline';

@Component({
  selector: 'app-section-header',
  host: { class: 'block' },
  template: `
    <header
      class="flex items-start justify-between gap-3"
      [class]="headerClass()">
      <div [class]="textClass()">
        <h3 [class]="headingClass()">{{ heading() }}</h3>

        @if (description()) {
          <p [class]="descriptionClass()">{{ description() }}</p>
        }
      </div>

      <div class="shrink-0 empty:hidden">
        <ng-content select="[sectionHeaderActions]" />
      </div>
    </header>
  `,
})
export class SectionHeaderComponent {
  readonly heading = input.required<string>();
  readonly description = input('');
  readonly appearance = input<SectionHeaderAppearance>('default');

  private readonly isInline = computed(() => this.appearance() === 'inline');

  protected readonly headerClass = computed(() => {
    return this.isInline() ? 'mb-3' : 'mb-4';
  });

  protected readonly textClass = computed(() => {
    return this.isInline()
      ? 'flex min-w-0 flex-wrap items-baseline gap-x-2.5 gap-y-0.5'
      : 'min-w-0';
  });

  protected readonly headingClass = computed(() => {
    return this.isInline()
      ? 'text-base font-semibold'
      : 'font-overpass text-[1.4rem] font-normal';
  });

  protected readonly descriptionClass = computed(() => {
    return this.isInline()
      ? 'text-foreground/60 text-[13px]'
      : 'text-muted mt-1 text-sm';
  });
}
