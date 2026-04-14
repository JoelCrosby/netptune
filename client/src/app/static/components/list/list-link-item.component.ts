import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-list-link-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  host: { class: 'block' },
  template: `
    <a
      [routerLink]="link()"
      class="bg-card hover:bg-primary/20 active:bg-primary/10 mb-0.75 flex h-10 items-center overflow-hidden rounded-sm transition-colors duration-200 ease-in-out">
      <ng-content />
    </a>
  `,
})
export class ListLinkItemComponent {
  link = input.required<string | string[] | null | undefined>();
}
