import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-embed-frame',
  imports: [RouterLink],
  host: { '[class]': 'hostClass()', contenteditable: 'false' },
  template: `
    <div
      class="bg-card-header border-border text-foreground/60 flex h-9 items-center gap-2 border-b px-3 text-xs">
      <span
        class="bg-change-added h-1.5 w-1.5 shrink-0 rounded-full"
        aria-hidden="true"></span>
      <span class="text-foreground/70 font-semibold">{{ badge() }}</span>
      <span class="min-w-0 truncate">{{ heading() }}</span>
      <span class="flex-1"></span>
      @if (link(); as link) {
        <a
          class="hover:text-foreground shrink-0 hover:underline"
          [routerLink]="link">
          {{ linkLabel() }} ↗
        </a>
      }
    </div>
    <div class="bg-card flex flex-col text-[13.5px] leading-snug">
      <ng-content />
    </div>
  `,
})
export class EmbedFrameComponent {
  readonly badge = input.required<string>();
  readonly heading = input('');
  readonly link = input<string[] | null>(null);
  readonly linkLabel = input('');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'border-border my-1 block overflow-hidden rounded-lg border',
      this.class()
    );
  });
}
