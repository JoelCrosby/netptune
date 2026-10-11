import { Routes } from '@angular/router';
import {
  LucideBell,
  LucideCalendarDays,
  LucideCalendarRange,
  LucideChartGantt,
  LucideChartNoAxesColumn,
  LucideChartSpline,
  LucideFileText,
  LucideHardDrive,
  LucideLayoutDashboard,
  LucideListFilter,
  LucidePin,
  LucideSettings,
  LucideShield,
  LucideSparkles,
  LucideSquareCheckBig,
  LucideTable2,
  LucideUserRound,
  LucideUsers,
  LucideWorkflow,
} from '@lucide/angular';
import { assistantGuard } from './core/auth/assistant.guard';
import { authGuard } from './core/auth/auth.guard';
import { workspaceGuard } from './core/auth/workspace.guard';
import { workspaceResovler } from './core/resolvers/workspace-resolver';

// prettier-ignore

export const routes: Routes = [
  {
    path: '',
    canActivate: [workspaceGuard],
    resolve: [workspaceResovler],
    loadComponent: () => import('./shell/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the workspace dashboard:Dashboard`, icon: LucideLayoutDashboard },
      },
      {
        path: 'projects',
        loadChildren: () => import('./features/projects/projects.routes').then((m) => m.routes),
        data: { title: $localize`:Page title for the project list:Projects`, icon: LucideChartNoAxesColumn },
      },
      {
        path: 'tasks',
        loadChildren: () => import('./features/project-tasks/project-tasks.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the task list:Tasks`, icon: LucideSquareCheckBig },
      },
      {
        path: 'queries',
        loadChildren: () => import('./features/queries/queries.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the saved query list:Queries`, icon: LucideListFilter },
      },
      {
        path: 'pinned',
        loadChildren: () => import('./features/pins/pins.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the pinned task list:Pinned`, icon: LucidePin },
      },
      {
        path: 'automations',
        loadChildren: () => import('./features/automations/automations.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the workspace automation rules:Automations`, icon: LucideWorkflow },
      },
      {
        path: 'boards',
        loadChildren: () => import('./features/boards/boards.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the kanban board list:Boards`, icon: LucideTable2 },
      },
      {
        path: 'sprints',
        loadChildren: () => import('./features/sprints/sprints.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the sprint list:Sprints`, icon: LucideCalendarRange },
      },
      {
        path: 'reports',
        loadChildren: () => import('./features/reporting/reporting.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the workspace reporting views:Reports`, icon: LucideChartSpline },
      },
      {
        path: 'pages',
        loadChildren: () => import('./features/pages/pages.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the workspace pages:Pages`, icon: LucideFileText },
      },
      {
        path: 'roadmap',
        loadChildren: () => import('./features/roadmap/roadmap.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the roadmap timeline:Roadmap`, icon: LucideChartGantt },
      },
      {
        path: 'calendar',
        loadChildren: () => import('./features/calendar/calendar.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: $localize`:Page title for the calendar view:Calendar`, icon: LucideCalendarDays },
      },
      {
        path: 'assistant',
        loadChildren: () => import('./features/assistant/assistant.routes').then((m) => m.routes),
        canActivate: [authGuard, assistantGuard],
        data: { title: $localize`:Page title for the assistant chat page:Assistant`, icon: LucideSparkles },
      },
      {
        path: 'notifications',
        loadChildren: () => import('./features/notifications/notifications.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for the notification list:Notifications`, icon: LucideBell },
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for the workspace member list:Users`, icon: LucideUsers },
      },
      {
        path: 'audit',
        loadChildren: () => import('./features/audit/audit.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for the workspace audit log:Audit Log`, icon: LucideShield },
      },
      {
        path: 'storage',
        loadChildren: () => import('./features/storage/storage.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for uploaded file storage:Storage`, icon: LucideHardDrive },
      },
      {
        path: 'settings',
        loadChildren: () => import('./features/settings/settings.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for workspace settings:Settings`, icon: LucideSettings },
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: $localize`:Page title for the signed-in user profile:Profile`, icon: LucideUserRound },
      },
    ],
  },
];
