import {
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { ScmConnectMethod } from '@core/models/source-control';
import { PERMISSIONS } from '@core/auth/permissions';
import { workspaceAiCredentialResource } from '@core/resources/ai-credential.resource';
import {
  sourceControlConnectionsResource,
  sourceControlProvidersResource,
} from '@core/resources/source-control.resource';
import { membersAndInvitesResource } from '@core/resources/user.resource';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceSetupTemplatesService } from '@core/services/workspace-setup-templates.service';
import { colorDictionary, fallbackColor } from '@core/util/colors/colors';
import {
  LucideCheck,
  LucideChevronLeft,
  LucideChevronRight,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { FlowPageComponent } from '@static/components/flow-page/flow-page.component';
import { FlowStepListComponent } from '@static/components/flow-step-list/flow-step-list.component';
import { FormControlShapeDirective } from '@static/components/form-control/form-control.directives';
import { SetupAssistantStepComponent } from '../../components/setup-steps/setup-assistant-step.component';
import { SetupRepositoryStepComponent } from '../../components/setup-steps/setup-repository-step.component';
import { SetupTeamStepComponent } from '../../components/setup-steps/setup-team-step.component';
import {
  WorkspaceFlowOptionalState,
  workspaceFlowOptionalSteps,
  workspaceFlowSetupStep,
  WorkspaceSetupStepKey,
  workspaceFlowWorkflowStep,
} from '../../workspace-flow-steps';

interface SetupStepCopy {
  name: string;
  heading: string;
  description: string;
}

const laterInSettings = $localize`:Reassures that a workspace setup step can wait:You can do this later in Settings.`;

const setupStepCopy: Record<WorkspaceSetupStepKey, SetupStepCopy> = {
  repository: {
    name: $localize`:Name of the repository step in the back and next buttons:Repository`,
    heading: $localize`:Title of the repository step:Link a repository`,
    description: `${$localize`:Description of the repository step:Link branches, commits and pull requests to tasks.`} ${laterInSettings}`,
  },
  assistant: {
    name: $localize`:Name of the assistant step in the back and next buttons:Assistant`,
    heading: $localize`:Title of the AI assistant key step:Add an AI assistant key`,
    description: `${$localize`:Description of the AI assistant key step:Turns on the assistant and task reviews for members without their own key.`} ${laterInSettings}`,
  },
  team: {
    name: $localize`:Name of the team step in the back and next buttons:Team`,
    heading: $localize`:Title of the invite teammates step:Invite teammates`,
    description: `${$localize`:Description of the invite teammates step:Send email invitations to this workspace.`} ${laterInSettings}`,
  },
};

const couldNotLoad = $localize`:Summary of a workspace setup step whose current state could not be loaded:Could not load`;

const stepsBeforeSetup = 2;

@Component({
  selector: 'app-workspace-setup-view',
  imports: [
    CalloutComponent,
    FlatButtonComponent,
    FlowPageComponent,
    FlowStepListComponent,
    FormControlShapeDirective,
    LucideChevronLeft,
    LucideChevronRight,
    RouterLink,
    SetupAssistantStepComponent,
    SetupRepositoryStepComponent,
    SetupTeamStepComponent,
    StrokedButtonComponent,
  ],
  template: `
    <div appFormShape="rounded">
      <app-flow-page
        i18n-eyebrow="Eyebrow above the name of a workspace being set up"
        eyebrow="Set up workspace"
        i18n-railLabel="Accessible label of the workspace setup steps list"
        railLabel="Workspace steps"
        [title]="workspaceName()"
        [position]="position()"
        [heading]="copy().heading"
        [description]="copy().description">
        <a app-flat-button flowPageActions [routerLink]="workspaceHome()">
          <span i18n="Button that leaves setup and opens the new workspace"
            >Go to Workspace</span
          >
        </a>

        <app-flow-step-list
          flowPageRail
          [items]="railItems()"
          [selected]="selected()"
          (selectStep)="select($event)" />

        @if (isFirstStep()) {
          <app-callout
            flowPageNotice
            class="mb-5"
            color="primary"
            [icon]="readyIcon">
            <span
              i18n="
                Confirms the workspace exists and the remaining steps can be
                skipped. NAME is the workspace name
              "
              >{{
                workspaceName() // i18n(ph="NAME")
              }}
              is ready. Everything from here is optional.</span
            >
          </app-callout>
        }

        @switch (selected()) {
          @case ('repository') {
            <app-setup-repository-step
              [workspace]="workspaceSlug()"
              [returnTo]="repositoryReturnUrl()"
              [redirectProviders]="redirectProviderList()"
              [connections]="connectionList()"
              [loadFailed]="sourceControlFailed()"
              (retry)="reloadSourceControl()" />
          }
          @case ('assistant') {
            <app-setup-assistant-step
              [credentials]="credentialList()"
              [loadFailed]="credentialsFailed()"
              (retry)="credentials.reload()"
              (saved)="credentials.reload()" />
          }
          @case ('team') {
            <app-setup-team-step
              [invitedCount]="invitedCount()"
              [(invitees)]="invitees"
              (invited)="membersAndInvites.reload()" />
          }
        }

        <ng-container flowPageFooter>
          @if (previous(); as step) {
            <button
              app-stroked-button
              class="gap-1.5"
              type="button"
              (click)="select(step)">
              <svg lucideChevronLeft class="h-3.5 w-3.5"></svg>
              <span>{{ stepName(step) }}</span>
            </button>
          }

          <span class="flex-1"></span>

          @if (next(); as step) {
            <button
              app-stroked-button
              class="gap-1.5"
              type="button"
              (click)="select(step)">
              <span>{{ stepName(step) }}</span>
              <svg lucideChevronRight class="h-3.5 w-3.5"></svg>
            </button>
          }
        </ng-container>
      </app-flow-page>
    </div>
  `,
})
export class WorkspaceSetupViewComponent {
  private readonly currentWorkspace = inject(CurrentWorkspaceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly setupTemplates = inject(WorkspaceSetupTemplatesService);

  readonly step = input<string>();
  readonly workflow = input<string>();

  protected readonly readyIcon = LucideCheck;

  private readonly colors = colorDictionary();
  private readonly canManageSourceControl = hasPermission(
    PERMISSIONS.sourceControl.manage
  );
  private readonly canUpdateWorkspace = hasPermission(
    PERMISSIONS.workspace.update
  );
  private readonly canInvite = hasPermission(PERMISSIONS.members.invite);

  private readonly providers = sourceControlProvidersResource();
  private readonly connections = sourceControlConnectionsResource();
  protected readonly credentials = workspaceAiCredentialResource();
  protected readonly membersAndInvites = membersAndInvitesResource();

  protected readonly invitees = signal<string[]>([]);

  readonly unsentInviteCount = computed(() => this.invitees().length);

  protected readonly workspaceName = computed(() => {
    return this.currentWorkspace.workspace()?.name ?? '';
  });

  protected readonly workspaceSlug = computed(() => {
    return this.currentWorkspace.slug() ?? '';
  });

  protected readonly workspaceHome = computed(() => {
    return ['/', this.workspaceSlug(), 'projects'];
  });

  protected readonly redirectProviderList = computed(() => {
    const providers = this.providers.hasValue() ? this.providers.value() : [];

    return providers.filter(
      (provider) => provider.connectMethod === ScmConnectMethod.redirect
    );
  });

  protected readonly connectionList = computed(() => {
    return this.connections.hasValue() ? this.connections.value() : [];
  });

  protected readonly credentialList = computed(() => {
    return this.credentials.hasValue() ? this.credentials.value() : [];
  });

  protected readonly sourceControlFailed = computed(() => {
    return !!this.providers.error() || !!this.connections.error();
  });

  protected readonly credentialsFailed = computed(() => {
    return !!this.credentials.error();
  });

  // Everyone counted except the person setting the workspace up, who is always a member.
  protected readonly invitedCount = computed(() => {
    const totalCount = this.membersAndInvites.hasValue()
      ? (this.membersAndInvites.value()?.payload?.totalCount ?? 0)
      : 0;

    return Math.max(totalCount - 1, 0);
  });

  private readonly hasNoProviders = computed(() => {
    const hasLoaded = this.providers.hasValue() && !this.providers.isLoading();
    const hasProviders = this.redirectProviderList().length > 0;
    const hasConnections = this.connectionList().length > 0;

    return hasLoaded && !hasProviders && !hasConnections;
  });

  private readonly steps = computed<WorkspaceSetupStepKey[]>(() => {
    const canLinkRepositories =
      this.canManageSourceControl() && !this.hasNoProviders();

    return [
      ...(canLinkRepositories ? (['repository'] as const) : []),
      ...(this.canUpdateWorkspace() ? (['assistant'] as const) : []),
      ...(this.canInvite() ? (['team'] as const) : []),
    ];
  });

  protected readonly selected = computed<WorkspaceSetupStepKey>(() => {
    const steps = this.steps();
    const requested = steps.find((key) => key === this.step());

    return requested ?? steps[0] ?? 'team';
  });

  private readonly selectedIndex = computed(() => {
    return this.steps().indexOf(this.selected());
  });

  protected readonly isFirstStep = computed(() => this.selectedIndex() === 0);

  protected readonly previous = computed(() => {
    return this.steps()[this.selectedIndex() - 1];
  });

  protected readonly next = computed(() => {
    return this.steps()[this.selectedIndex() + 1];
  });

  protected readonly copy = computed(() => setupStepCopy[this.selected()]);

  protected readonly position = computed(() => {
    const stepNumber = stepsBeforeSetup + this.selectedIndex() + 1;
    const total = stepsBeforeSetup + this.steps().length;

    return $localize`:Position of the open step in workspace setup. STEP is its number, TOTAL the number of steps:Step ${stepNumber}:STEP: of ${total}:TOTAL:`;
  });

  protected readonly repositoryReturnUrl = computed(() => {
    const url = this.router.createUrlTree(
      ['/', this.workspaceSlug(), 'setup'],
      {
        queryParams: { step: 'repository', workflow: this.workflow() },
      }
    );

    return this.router.serializeUrl(url);
  });

  protected readonly railItems = computed(() => {
    const workspace = this.currentWorkspace.workspace();
    const color = workspace?.metaInfo?.color ?? fallbackColor;
    const colorLabel =
      this.colors.find((item) => item.name === color)?.label ?? color;
    const template = this.setupTemplates
      .templates()
      .find((item) => item.key === this.workflow());

    return [
      workspaceFlowSetupStep({
        identifier: this.workspaceSlug(),
        colorLabel,
        hasDescription: !!workspace?.description,
        locked: true,
      }),
      workspaceFlowWorkflowStep(template, true),
      ...workspaceFlowOptionalSteps(this.steps().map((key) => this.state(key))),
    ];
  });

  constructor() {
    effect(() => {
      if (this.steps().length > 0) return;

      void this.router.navigate(this.workspaceHome(), { replaceUrl: true });
    });
  }

  protected stepName(step: WorkspaceSetupStepKey): string {
    return setupStepCopy[step].name;
  }

  protected select(step: string) {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { step },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  protected reloadSourceControl() {
    this.providers.reload();
    this.connections.reload();
  }

  private state(key: WorkspaceSetupStepKey): WorkspaceFlowOptionalState {
    switch (key) {
      case 'repository':
        return this.repositoryState();
      case 'assistant':
        return this.assistantState();
      case 'team':
        return this.teamState();
    }
  }

  private repositoryState(): WorkspaceFlowOptionalState {
    if (this.sourceControlFailed()) {
      return { key: 'repository', summary: couldNotLoad, done: false };
    }

    const [connection] = this.connectionList();

    if (!connection) {
      return {
        key: 'repository',
        summary: $localize`:Summary when no source control account is connected:Not connected`,
        done: false,
      };
    }

    const { accountLogin, linkedRepoCount } = connection;

    return {
      key: 'repository',
      summary:
        linkedRepoCount === 1
          ? $localize`:Summary of a connected account with one linked repository. ACCOUNT is its name:${accountLogin}:ACCOUNT:, 1 repository linked`
          : $localize`:Summary of a connected account and its linked repositories. ACCOUNT is its name, COUNT how many are linked:${accountLogin}:ACCOUNT:, ${linkedRepoCount}:COUNT: repositories linked`,
      done: true,
    };
  }

  private assistantState(): WorkspaceFlowOptionalState {
    if (this.credentialsFailed()) {
      return { key: 'assistant', summary: couldNotLoad, done: false };
    }

    const [credential] = this.credentialList();

    if (!credential) {
      return {
        key: 'assistant',
        summary: $localize`:Summary when the workspace has no AI key:No key`,
        done: false,
      };
    }

    const { label, secretHint } = credential;

    return {
      key: 'assistant',
      summary: $localize`:Summary of a stored AI key. PROVIDER is its name, HINT its last characters:${label}:PROVIDER: key …${secretHint}:HINT:`,
      done: true,
    };
  }

  private teamState(): WorkspaceFlowOptionalState {
    if (this.membersAndInvites.error()) {
      return { key: 'team', summary: couldNotLoad, done: false };
    }

    const count = this.invitedCount();

    if (count === 0) {
      return {
        key: 'team',
        summary: $localize`:Summary when nobody has been invited to the workspace:Nobody invited yet`,
        done: false,
      };
    }

    return {
      key: 'team',
      summary:
        count === 1
          ? $localize`:Summary when one person has been invited:1 invitation sent`
          : $localize`:Summary of how many people have been invited. COUNT is that number:${count}:COUNT: invitations sent`,
      done: true,
    };
  }
}
