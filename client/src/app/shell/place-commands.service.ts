import { Injectable, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { AiPanelService } from '@core/services/ai-panel.service';
import {
  Command,
  CommandPlace,
  injectCommands,
} from '@core/services/command-registry.service';
import { PlaceNavigationService } from '@core/services/place-navigation.service';
import { SessionService } from '@core/services/session.service';

const REPORTS_SECTION = $localize`:Command palette section label for places inside the reports:Reports`;
const PAGES_SECTION = $localize`:Command palette section label for places inside workspace pages:Pages`;
const WORKSPACE_SETTINGS_SECTION = $localize`:Command palette section label for places inside workspace settings:Workspace settings`;
const PERSONAL_SETTINGS_SECTION = $localize`:Command palette section label for places inside personal settings:Personal settings`;

interface PlaceCommand extends Omit<Command, 'execute'> {
  place: CommandPlace;
  available: () => boolean;
}

@Injectable()
export class PlaceCommandsService {
  private navigation = inject(PlaceNavigationService);
  private panel = inject(AiPanelService);
  private authenticated = inject(SessionService).isAuthenticated;
  private isPublicViewer = inject(SessionService).isPublicViewer;

  private canReadReports = hasPermission(PERMISSIONS.reports.read);
  private canReadMembers = hasPermission(PERMISSIONS.members.read);
  private canReadSprints = hasPermission(PERMISSIONS.sprints.read);
  private canReadPages = hasPermission(PERMISSIONS.pages.read);
  private canCreatePages = hasPermission(PERMISSIONS.pages.create);
  private canReadWorkspace = hasPermission(PERMISSIONS.workspace.read);
  private canReadTags = hasPermission(PERMISSIONS.tags.read);
  private canReadStatuses = hasPermission(PERMISSIONS.statuses.read);
  private canReadRelationTypes = hasPermission(PERMISSIONS.relationTypes.read);
  private canReadServiceAccounts = hasPermission(
    PERMISSIONS.serviceAccounts.read
  );
  private canReadSourceControl = hasPermission(PERMISSIONS.sourceControl.read);
  private canReadAssistantConversations = hasPermission(
    PERMISSIONS.assistant.readAllConversations
  );
  private canExportData = hasPermission(PERMISSIONS.tasks.export);

  private readonly places: PlaceCommand[] = [
    {
      id: 'place.reports.throughput',
      label: $localize`:Command palette place for the throughput chart in the reports:Throughput`,
      group: 'navigation',
      icon: 'chart-spline',
      keywords: searchTerms(
        $localize`:Comma-separated search terms for the throughput report. Translate each term and keep the commas:flow,completed,done,charts`
      ),
      place: {
        path: 'reports',
        fragment: 'throughput',
        section: REPORTS_SECTION,
      },
      available: () => this.canReadReports(),
    },
    {
      id: 'place.reports.cycleTime',
      label: $localize`:Command palette place for the cycle-time chart in the reports:Cycle time`,
      group: 'navigation',
      icon: 'chart-spline',
      keywords: searchTerms(
        $localize`:Comma-separated search terms for the cycle-time report. Translate each term and keep the commas:flow,elapsed,lead time,duration`
      ),
      place: {
        path: 'reports',
        fragment: 'cycle-time',
        section: REPORTS_SECTION,
      },
      available: () => this.canReadReports(),
    },
    {
      id: 'place.reports.workload',
      label: $localize`:Command palette place for the workload section in the reports:Workload`,
      group: 'navigation',
      icon: 'chart-spline',
      keywords: searchTerms(
        $localize`:Comma-separated search terms for the workload report. Translate each term and keep the commas:assignees,capacity,open work`
      ),
      place: {
        path: 'reports',
        fragment: 'workload',
        section: REPORTS_SECTION,
      },
      available: () => this.canReadReports() && this.canReadMembers(),
    },
    {
      id: 'place.reports.burndown',
      label: $localize`:Command palette place for the sprint burndown in the reports:Burndown`,
      group: 'navigation',
      icon: 'chart-spline',
      keywords: searchTerms(
        $localize`:Comma-separated search terms for the sprint burndown report. Translate each term and keep the commas:sprint,remaining,scope`
      ),
      place: {
        path: 'reports',
        fragment: 'burndown',
        section: REPORTS_SECTION,
      },
      available: () => this.canReadReports() && this.canReadSprints(),
    },
    {
      id: 'place.reports.velocity',
      label: $localize`:Command palette place for the sprint velocity section in the reports:Velocity`,
      group: 'navigation',
      icon: 'chart-spline',
      keywords: searchTerms(
        $localize`:Comma-separated search terms for the sprint velocity report. Translate each term and keep the commas:sprint,committed,completed,points`
      ),
      place: {
        path: 'reports',
        fragment: 'velocity',
        section: REPORTS_SECTION,
      },
      available: () => this.canReadReports() && this.canReadSprints(),
    },
    {
      id: 'place.pages.archive',
      label: $localize`:Command palette place for the list of archived pages:Page archive`,
      group: 'navigation',
      icon: 'archive',
      keywords: searchTerms(
        $localize`:Comma-separated search terms for the archived pages list. Translate each term and keep the commas:archived,deleted,restore`
      ),
      place: { path: 'pages/archive', section: PAGES_SECTION },
      available: () => this.canReadPages() && !this.isPublicViewer(),
    },
    {
      id: 'place.pages.templates',
      label: $localize`:Command palette place for the templates section on the pages home:Page templates`,
      group: 'navigation',
      icon: 'file-plus',
      keywords: searchTerms(
        $localize`:Comma-separated search terms for the page templates section. Translate each term and keep the commas:new page,create,start`
      ),
      place: {
        path: 'pages',
        fragment: 'templates',
        section: PAGES_SECTION,
      },
      available: () => this.canReadPages() && this.canCreatePages(),
    },
    {
      id: 'place.settings.appearance',
      label: $localize`:Command palette place for the appearance section in personal settings:Appearance`,
      group: 'settings',
      icon: 'cog',
      keywords: searchTerms(
        $localize`:Comma-separated search terms for the appearance settings. Translate each term and keep the commas:theme,dark,light,page width,task layout`
      ),
      place: {
        path: 'settings/personal/general',
        fragment: 'appearance',
        section: PERSONAL_SETTINGS_SECTION,
      },
      available: () => this.authenticated(),
    },
    {
      id: 'place.settings.notifications',
      label: $localize`:Command palette place for personal notification preference settings:Notification Preferences`,
      group: 'settings',
      icon: 'cog',
      keywords: ['notifications', 'preferences', 'settings', 'email'],
      place: {
        path: 'settings/personal/notifications',
        section: PERSONAL_SETTINGS_SECTION,
      },
      available: () => this.authenticated(),
    },
    {
      id: 'place.settings.personalAssistant',
      label: $localize`:Command palette place for the personal assistant key settings:Assistant Key Settings`,
      group: 'settings',
      icon: 'sparkles',
      keywords: ['assistant', 'ai', 'api key', 'settings'],
      place: {
        path: 'settings/personal/assistant',
        section: PERSONAL_SETTINGS_SECTION,
      },
      available: () => this.panel.isAvailable(),
    },
    {
      id: 'place.settings.workspace',
      label: $localize`:Command palette place for general workspace settings:Workspace Settings`,
      group: 'settings',
      icon: 'settings-2',
      keywords: ['workspace', 'settings', 'general', 'branding'],
      place: {
        path: 'settings/workspace/general',
        section: WORKSPACE_SETTINGS_SECTION,
      },
      available: () => this.canReadWorkspace(),
    },
    {
      id: 'place.settings.tags',
      label: $localize`:Command palette place for workspace tag settings:Tag Settings`,
      group: 'settings',
      icon: 'tag',
      keywords: ['tags', 'labels', 'workspace', 'settings'],
      place: {
        path: 'settings/workspace/tags',
        section: WORKSPACE_SETTINGS_SECTION,
      },
      available: () => this.canReadTags(),
    },
    {
      id: 'place.settings.statuses',
      label: $localize`:Command palette place for workspace task status settings:Status Settings`,
      group: 'settings',
      icon: 'list-checks',
      keywords: ['statuses', 'status', 'workflow', 'workspace', 'settings'],
      place: {
        path: 'settings/workspace/statuses',
        section: WORKSPACE_SETTINGS_SECTION,
      },
      available: () => this.canReadStatuses(),
    },
    {
      id: 'place.settings.relations',
      label: $localize`:Command palette place for workspace task relation type settings:Relation Settings`,
      group: 'settings',
      icon: 'git-fork',
      keywords: ['relations', 'relation types', 'links', 'workspace'],
      place: {
        path: 'settings/workspace/relations',
        section: WORKSPACE_SETTINGS_SECTION,
      },
      available: () => this.canReadRelationTypes(),
    },
    {
      id: 'place.settings.serviceAccounts',
      label: $localize`:Command palette place for workspace service account settings:Service Accounts`,
      group: 'settings',
      icon: 'bot',
      keywords: ['service accounts', 'api', 'tokens', 'bots', 'workspace'],
      place: {
        path: 'settings/workspace/service-accounts',
        section: WORKSPACE_SETTINGS_SECTION,
      },
      available: () => this.canReadServiceAccounts(),
    },
    {
      id: 'place.settings.sourceControl',
      label: $localize`:Command palette place for workspace source control settings:Source Control Settings`,
      group: 'settings',
      icon: 'git-branch',
      keywords: [
        'source control',
        'github',
        'repositories',
        'git',
        'integrations',
      ],
      place: {
        path: 'settings/workspace/source-control',
        section: WORKSPACE_SETTINGS_SECTION,
      },
      available: () => this.canReadSourceControl(),
    },
    {
      id: 'place.settings.workspaceAssistant',
      label: $localize`:Command palette place for the workspace assistant settings:Workspace Assistant Settings`,
      group: 'settings',
      icon: 'sparkles',
      keywords: ['assistant', 'ai', 'conversations', 'workspace', 'settings'],
      place: {
        path: 'settings/workspace/assistant',
        section: WORKSPACE_SETTINGS_SECTION,
      },
      available: () => this.canReadAssistantConversations(),
    },
    {
      id: 'place.settings.data',
      label: $localize`:Command palette place for workspace import and export settings:Import & Export`,
      group: 'settings',
      icon: 'database',
      keywords: ['data', 'import', 'export', 'backup', 'workspace'],
      place: {
        path: 'settings/workspace/data',
        section: WORKSPACE_SETTINGS_SECTION,
      },
      available: () => this.canExportData(),
    },
  ];

  constructor() {
    injectCommands(() => {
      return this.places
        .filter((place) => place.available())
        .map((place) => this.toCommand(place));
    }, 'global');
  }

  private toCommand({ available: _available, ...command }: PlaceCommand) {
    return { ...command, execute: () => this.open(command.place) };
  }

  private open(place: CommandPlace) {
    const url = this.navigation.urlFor(place);

    if (url) this.navigation.open(url);
  }
}

function searchTerms(terms: string): string[] {
  return terms.split(',');
}
