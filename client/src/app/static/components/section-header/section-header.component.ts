import { booleanAttribute, Component, computed, input } from '@angular/core';

export type SectionHeaderAppearance = 'default' | 'inline' | 'eyebrow';

@Component({
  selector: 'app-section-header',
  host: { class: 'block' },
  template: `
    <header
      class="flex items-start justify-between gap-3"
      [class]="headerClass()">
      <div [class]="textClass()">
        <h3 [class]="headingClass()">{{ heading() }}</h3>
        <ng-content select="[sectionHeaderBadge]" />

        @if (description()) {
          <p [class]="descriptionClass()">{{ description() }}</p>
        }
      </div>

      <div [class]="actionsClass()">
        <ng-content select="[sectionHeaderActions]" />
      </div>
    </header>
  `,
})
export class SectionHeaderComponent {
  readonly heading = input.required<string>();
  readonly description = input('');
  readonly appearance = input<SectionHeaderAppearance>('default');
  readonly wrap = input(false, { transform: booleanAttribute });

  private readonly isInline = computed(() => this.appearance() === 'inline');

  protected readonly headerClass = computed(() => {
    const spacing = this.appearance() === 'default' ? 'mb-4' : 'mb-3';

    return this.wrap() ? `${spacing} flex-wrap` : spacing;
  });

  protected readonly actionsClass = computed(() => {
    return this.wrap() ? 'min-w-0 empty:hidden' : 'shrink-0 empty:hidden';
  });

  protected readonly textClass = computed(() => {
    return this.isInline()
      ? 'flex min-w-0 flex-wrap items-baseline gap-x-2.5 gap-y-0.5'
      : 'min-w-0';
  });

  protected readonly headingClass = computed(() => {
    if (this.appearance() === 'eyebrow') {
      return 'text-foreground/60 text-xs font-semibold tracking-[.06em] uppercase';
    }

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
