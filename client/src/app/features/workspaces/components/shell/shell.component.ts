import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatSidenav } from '@angular/material/sidenav';
import { MatTooltip } from '@angular/material/tooltip';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import {
  selectCurrentUser,
  selectIsAuthenticated,
} from '@core/auth/store/auth.selectors';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { Workspace } from '@core/models/workspace';
import { toggleSideMenu } from '@core/store/layout/layout.actions';
import {
  selectSideMenuMode,
  selectSideMenuOpen,
} from '@core/store/layout/layout.selectors';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { WorkspaceSelectComponent } from '../workspace-select/workspace-select.component';

@Component({
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    WorkspaceSelectComponent,
    RouterLinkActive,
    RouterLink,
    MatTooltip,
    MatIcon,
    AvatarComponent,
    RouterOutlet,
  ],
})
export class ShellComponent {
  private store = inject(Store);
  private router = inject(Router);
  private storage = inject(LocalStorageService);

  readonly sideNav = viewChild.required(MatSidenav);

  authenticated = this.store.selectSignal(selectIsAuthenticated);
  sideNavExpanded = signal(this.storage.getItem('side-nav-expanded') ?? true);

  links = [
    { label: 'Projects', value: ['./projects'], icon: 'assessment' },
    { label: 'Tasks', value: ['./tasks'], icon: 'check_box' },
    { label: 'Boards', value: ['./boards'], icon: 'table_chart' },
    { label: 'Users', value: ['./users'], icon: 'supervised_user_circle' },
  ];

  bottomLinks = [
    { label: 'Settings', value: ['./settings'], icon: 'settings_applications' },
  ];

  sideMenuOpen = this.store.selectSignal(selectSideMenuOpen);
  sideMenuMode = this.store.selectSignal(selectSideMenuMode);
  user = this.store.selectSignal(selectCurrentUser);

  onSidenavClosedStart() {
    this.store.dispatch(toggleSideMenu());
  }

  onToggleExpandClicked() {
    this.sideNavExpanded.set(!this.sideNavExpanded());
    this.storage.setItem('side-nav-expanded', this.sideNavExpanded());
  }

  onWorkspaceChange(workspace: Workspace) {
    if (!workspace) {
      throw new Error('onWorkspaceChange workspace is null');
    }

    void this.router.navigate(['/', workspace.slug]);
  }
}
