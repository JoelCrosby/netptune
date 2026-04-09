import {
  ChangeDetectionStrategy,
  Component,
  inject,
  output,
} from '@angular/core';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { ShellMenuLinkListComponent } from './shell-menu-link-list.component';
import { ShellMenuLinkComponent } from './shell-menu-link.component';
import { ShellSidebarCollapseComponent } from './shell-sidebar-collapse.component';
import { ShellService } from './shell.service';
import { WorkspaceSelectComponent } from './workspace-select/workspace-select.component';

@Component({
  selector: 'app-shell-sidebar',
  template: `
    <div
      class="border-side-bar-border fixed top-[0] flex h-screen w-[72px] flex-col justify-between border-r bg-neutral-950 [transition:width_.2s_ease-in-out]"
      [class.w-[248px]]="shell.sideNavExpanded()">
      <app-shell-menu-link-list>
        <app-workspace-select
          idKey="id"
          labelKey="name"
          (selectChange)="onWorkspaceChange($event)">
        </app-workspace-select>
        @for (link of links; track link.value) {
          <app-shell-menu-link [link]="link" />
        }
      </app-shell-menu-link-list>

      <div class="flex-[1]"></div>

      <app-shell-menu-link-list>
        @for (link of bottomLinks; track link.value) {
          <app-shell-menu-link [link]="link" />
        }
        @if (user(); as user) {
          <app-shell-menu-link
            [link]="{ label: 'Profile', value: ['./profile'] }">
            <app-avatar
              class="app-menu-link-profile"
              [name]="user.displayName"
              [imageUrl]="user.pictureUrl"
              [size]="24"
              [border]="true"
              [tooltip]="false" />
          </app-shell-menu-link>
        }
      </app-shell-menu-link-list>
      <app-shell-sidebar-collapse />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    WorkspaceSelectComponent,
    AvatarComponent,
    ShellMenuLinkComponent,
    ShellMenuLinkListComponent,
    ShellSidebarCollapseComponent,
  ],
})
export class ShellSidebarComponent {
  private store = inject(Store);

  shell = inject(ShellService);

  links = [
    { label: 'Projects', value: ['./projects'], icon: 'assessment' },
    { label: 'Tasks', value: ['./tasks'], icon: 'check_box' },
    { label: 'Boards', value: ['./boards'], icon: 'table_chart' },
    { label: 'Users', value: ['./users'], icon: 'supervised_user_circle' },
  ];

  bottomLinks = [
    { label: 'Settings', value: ['./settings'], icon: 'settings_applications' },
  ];

  user = this.store.selectSignal(selectCurrentUser);

  workspaceChange = output<Workspace>();

  onWorkspaceChange(workspace: Workspace) {
    this.workspaceChange.emit(workspace);
  }
}
