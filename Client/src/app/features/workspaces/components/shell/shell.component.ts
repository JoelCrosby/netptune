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

@Component({
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent implements OnInit {
  @ViewChild(MatSidenav) sideNav: MatSidenav;

  authenticated$: Observable<boolean>;

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
  fixedInViewport$ = of(true);

  constructor(private store: Store) {}

  ngOnInit() {
    this.authenticated$ = this.store.select(
      AuthSelectors.selectIsAuthenticated
    );
  }

  onSidenavClosedStart() {
    this.store.dispatch(toggleSideMenu());
  }
}
