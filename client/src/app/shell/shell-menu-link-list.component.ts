import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ShellService } from './shell.service';

@Component({
  selector: 'app-shell-menu-link-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block w-full' },
  template: `
    <div
      class="flex flex-col items-center"
      [class.items-start]="shell.sideNavExpanded()">
      <ng-content />
    </div>
  `,
})
export class ShellMenuLinkListComponent {
  shell = inject(ShellService);
}
