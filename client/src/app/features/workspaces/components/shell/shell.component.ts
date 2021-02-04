import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  ViewChild,
} from '@angular/core';
import { MatSidenav } from '@angular/material/sidenav';
import {
  selectSideMenuMode,
  selectSideMenuOpen,
} from '@core/store/layout/layout.selectors';
import * as AuthSelectors from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { Observable, of } from 'rxjs';
import { toggleSideMenu } from '@core/store/layout/layout.actions';
import {
  selectAllWorkspaces,
  selectCurrentWorkspaceIdentifier,
} from '@core/store/workspaces/workspaces.selectors';
import { Router } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { LocalStorageService } from '@core/local-storage/local-storage.service';

@Component({
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent implements OnInit {
  @ViewChild(MatSidenav) sideNav: MatSidenav;

  authenticated$: Observable<boolean>;
  sideNavExpanded = true;

  links = [
    { label: 'Projects', value: ['./projects'], icon: 'assessment' },
    { label: 'Tasks', value: ['./tasks'], icon: 'check_box' },
    { label: 'Boards', value: ['./boards'], icon: 'table_chart' },
    { label: 'Users', value: ['./users'], icon: 'supervised_user_circle' },
  ];

  bottomLinks = [
    { label: 'Settings', value: ['./settings'], icon: 'settings_applications' },
  ];

  sideMenuOpen$ = this.store.select(selectSideMenuOpen);
  sideMenuMode$ = this.store.select(selectSideMenuMode);
  user$ = this.store.select(AuthSelectors.selectCurrentUser);
  workspaces$ = this.store.select(selectAllWorkspaces);
  workspaceId$ = this.store.select(selectCurrentWorkspaceIdentifier);
  fixedInViewport$ = of(true);

  constructor(
    private store: Store,
    private router: Router,
    private storage: LocalStorageService
  ) {
    this.sideNavExpanded = this.storage.getItem('side-nav-expanded') ?? true;
  }

  ngOnInit() {
    this.authenticated$ = this.store.select(
      AuthSelectors.selectIsAuthenticated
    );
  }

  onSidenavClosedStart() {
    this.store.dispatch(toggleSideMenu());
  }

  onToggleExpandClicked() {
    this.sideNavExpanded = !this.sideNavExpanded;
    this.storage.setItem('side-nav-expanded', this.sideNavExpanded);
  }

  onWorkspaceChange(workspace: Workspace) {
    if (!workspace) {
      throw new Error('onWorkspaceChange workspace is null');
    }

    this.router.navigate(['/', workspace.slug]);
  }
}
