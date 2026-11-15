import { booleanAttribute, Component, input } from '@angular/core';
import {
  BadgeColor,
  BadgeComponent,
} from '@static/components/badge/badge.component';

@Component({
  selector: 'app-labelled-card',
  imports: [BadgeComponent],
  host: { class: 'block' },
  template: `
    <article
      class="border-foreground/8 rounded-lg border px-3.5 py-3"
      [class.opacity-70]="muted()">
      <div class="flex flex-wrap items-center gap-2">
        <app-badge [color]="labelColor()" shape="rounded">
          {{ label() }}
        </app-badge>

        @if (status(); as status) {
          <span class="text-muted text-[13px]">{{ status }}</span>
        }
      </div>

      <h4 class="mt-2.5 text-[14.5px] font-semibold">{{ title() }}</h4>

      @if (body(); as body) {
        <p class="text-foreground/75 mt-1.5 text-[14px]/[1.5]">{{ body }}</p>
      }

      @if (quote(); as quote) {
        <blockquote
          class="border-foreground/15 text-muted mt-2.5 border-l-2 pl-2.5 text-[13px]/[1.5] italic">
          {{ quote }}
        </blockquote>
      }

      <ng-content />

      <div class="mt-3.5 flex flex-wrap items-center gap-1 empty:hidden">
        <ng-content select="[cardActions]" />
      </div>
    </article>
  `,
})
export class LabelledCardComponent {
  readonly label = input.required<string>();
  readonly labelColor = input<BadgeColor>('neutral');
  readonly title = input.required<string>();
  readonly body = input<string | null>(null);
  readonly quote = input<string | null>(null);
  readonly status = input<string | null>(null);
  readonly muted = input(false, { transform: booleanAttribute });
}
