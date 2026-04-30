import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  inject,
  provideAppInitializer,
  provideZonelessChangeDetection,
} from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter, withViewTransitions } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideRouterStore } from '@ngrx/router-store';
import { provideStore } from '@ngrx/store';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { AuthEffects } from './app/core/store/auth/auth.effects';
import { metaReducers, reducers } from './app/core/core.state';
import { authInterceptor } from './app/core/http-interceptors/auth.interceptor';
import { CustomSerializer } from './app/core/router/custom-serializer';
import { NavigationService } from './app/core/services/navigation.service';
import { ActivityEffects } from './app/core/store/activity/activity.effects';
import { BoardsEffects } from './app/core/store/boards/boards.effects';
import { BoardGroupsEffects } from './app/core/store/groups/board-groups.effects';
import { LayoutEffects } from './app/core/store/layout/layout.effects';
import { MetaEffects } from './app/core/store/meta/meta.effects';
import { NotificationsEffects } from './app/core/store/notifications/notifications.effects';
import { ProjectsEffects } from './app/core/store/projects/projects.effects';
import { SettingsEffects } from './app/core/store/settings/settings.effects';
import { TagsEffects } from './app/core/store/tags/tags.effects';
import { ProjectTasksEffects } from './app/core/store/tasks/tasks.effects';
import { UsersEffects } from './app/core/store/users/users.effects';
import { WorkspacesEffects } from './app/core/store/workspaces/workspaces.effects';
import { ProfileEffects } from './app/core/store/profile/profile.effects';

bootstrapApplication(AppComponent, {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes, withViewTransitions()),
    provideStore(reducers, {
      metaReducers,
      runtimeChecks: {
        strictStateImmutability: true,
        strictActionImmutability: true,
        strictStateSerializability: false,
        strictActionSerializability: false,
        strictActionTypeUniqueness: true,
      },
    }),
    provideRouterStore({
      serializer: CustomSerializer,
    }),
    provideEffects([
      AuthEffects,
      MetaEffects,
      ActivityEffects,
      NotificationsEffects,
      LayoutEffects,
      SettingsEffects,
      WorkspacesEffects,
      ProjectsEffects,
      ProjectTasksEffects,
      UsersEffects,
      TagsEffects,
      BoardsEffects,
      ProfileEffects,
      BoardGroupsEffects,
    ]),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAppInitializer(() => {
      inject(NavigationService).listen();
    }),
  ],
});
