import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideArrowLeft } from '@lucide/angular';

@Component({
  selector: 'app-page-header-back-link',
  template: `
    @if (backLink()) {
      <a
        class="text-foreground/70 hover:text-foreground inline-flex cursor-pointer items-center text-sm font-medium tracking-[0.225px] transition"
        [routerLink]="backLink()">
        <svg
          lucideArrowLeft
          class="mr-[0.4rem] h-4 w-4"
          aria-hidden="true"></svg>
        <span> {{ backLabel() || 'Go back' }} </span>
      </a>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideArrowLeft],
})
export class PageHeaderBackLinkComponent {
  readonly backLink = input<string[] | number[] | null>();
  readonly backLabel = input<string | null>();
}
