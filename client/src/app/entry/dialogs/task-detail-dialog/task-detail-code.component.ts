import { Component, computed, inject } from '@angular/core';
import {
  LucideGitBranch,
  LucideGitCommitHorizontal,
  LucideGitMerge,
  LucideGitPullRequest,
  LucideGitPullRequestClosed,
  LucideGitPullRequestDraft,
  type LucideIconInput,
} from '@lucide/angular';
import {
  ScmCheckState,
  ScmCheckSummary,
  ScmPullRequestState,
  sourceControlProviderSlug,
  TaskCodeLink,
  TaskCodeLinkKind,
} from '@core/models/source-control';
import { taskCodeLinksResource } from '@core/resources/source-control.resource';
import { fromNow } from '@core/util/dates';
import type { BadgeColor } from '@static/components/badge/badge.component';
import {
  ExternalLinkBadge,
  ExternalLinkGroup,
  ExternalLinkListComponent,
  ExternalLinkListItem,
} from '@static/components/external-link-list/external-link-list.component';
import { TaskDetailService } from './task-detail.service';

const pullRequestIcons: Record<ScmPullRequestState, LucideIconInput> = {
  [ScmPullRequestState.open]: LucideGitPullRequest,
  [ScmPullRequestState.draft]: LucideGitPullRequestDraft,
  [ScmPullRequestState.merged]: LucideGitMerge,
  [ScmPullRequestState.closed]: LucideGitPullRequestClosed,
};

const pullRequestColors: Record<ScmPullRequestState, BadgeColor> = {
  [ScmPullRequestState.open]: 'success',
  [ScmPullRequestState.draft]: 'neutral',
  [ScmPullRequestState.merged]: 'primary',
  [ScmPullRequestState.closed]: 'warn',
};

const checkColors: Record<ScmCheckState, BadgeColor> = {
  [ScmCheckState.pending]: 'pending',
  [ScmCheckState.success]: 'success',
  [ScmCheckState.failure]: 'warn',
  [ScmCheckState.neutral]: 'neutral',
};

@Component({
  selector: 'app-task-detail-code',
  imports: [ExternalLinkListComponent],
  template: `
    @if (groups().length) {
      <app-external-link-list
        [groups]="groups()"
        i18n-showMoreLabel="
          Button that reveals the rest of a long list of linked code
        "
        showMoreLabel="Show all"
        i18n-newTabLabel="Screen reader hint on a link that opens in a new tab"
        newTabLabel="(opens in a new tab)" />
    }
  `,
})
export class TaskDetailCodeComponent {
  private readonly taskDetail = inject(TaskDetailService);

  protected readonly links = taskCodeLinksResource(this.taskDetail.systemId);

  private readonly loadedLinks = computed(() => {
    return this.links.hasValue() ? this.links.value() : [];
  });

  readonly count = computed(() => this.loadedLinks().length);

  private readonly labels = {
    pullRequests: $localize`:Group of pull requests linked to a task:Pull requests`,
    branches: $localize`:Group of branches linked to a task:Branches`,
    commits: $localize`:Group of commits linked to a task:Commits`,
    open: $localize`:pull request state|State of an open pull request:Open`,
    draft: $localize`:State of a draft pull request:Draft`,
    merged: $localize`:State of a merged pull request:Merged`,
    closed: $localize`:State of a closed pull request:Closed`,
    deleted: $localize`:Shown on a linked branch that was deleted:Deleted`,
    checksPending: $localize`:CI checks still running on linked code:Checks running`,
    checksPassed: $localize`:CI checks that all passed on linked code:Checks passed`,
    checksFailed: $localize`:CI checks that failed on linked code:Checks failed`,
    checksNeutral: $localize`:CI checks that neither passed nor failed:Checks skipped`,
  };

  protected readonly groups = computed<ExternalLinkGroup[]>(() => {
    const links = this.loadedLinks();

    const groups: ExternalLinkGroup[] = [
      {
        id: 'pullRequests',
        label: this.labels.pullRequests,
        items: this.itemsOf(links, TaskCodeLinkKind.pullRequest),
      },
      {
        id: 'branches',
        label: this.labels.branches,
        items: this.itemsOf(links, TaskCodeLinkKind.branch),
      },
      {
        id: 'commits',
        label: this.labels.commits,
        items: this.itemsOf(links, TaskCodeLinkKind.commit),
      },
    ];

    return groups.filter((group) => group.items.length > 0);
  });

  private itemsOf(
    links: TaskCodeLink[],
    kind: TaskCodeLinkKind
  ): ExternalLinkListItem[] {
    return links
      .filter((link) => link.kind === kind)
      .map((link) => this.toItem(link));
  }

  private toItem(link: TaskCodeLink): ExternalLinkListItem {
    return {
      id: link.id,
      href: link.webUrl,
      icon: this.iconOf(link),
      label: link.title,
      meta: this.metaOf(link),
      provider: sourceControlProviderSlug(link.provider),
      muted: link.isRemoved,
      badges: this.badgesOf(link),
    };
  }

  private iconOf(link: TaskCodeLink): LucideIconInput {
    switch (link.kind) {
      case TaskCodeLinkKind.pullRequest:
        return pullRequestIcons[
          link.pullRequestState ?? ScmPullRequestState.open
        ];
      case TaskCodeLinkKind.branch:
        return LucideGitBranch;
      case TaskCodeLinkKind.commit:
        return LucideGitCommitHorizontal;
    }
  }

  private metaOf(link: TaskCodeLink): string {
    return [
      link.repoFullName,
      this.referenceOf(link),
      link.authorLogin,
      fromNow(link.occurredAt),
    ]
      .filter((part) => !!part)
      .join(' · ');
  }

  private referenceOf(link: TaskCodeLink): string | null {
    switch (link.kind) {
      case TaskCodeLinkKind.pullRequest:
        return `#${link.externalRef}`;
      case TaskCodeLinkKind.commit:
        return link.externalRef.slice(0, 7);
      case TaskCodeLinkKind.branch:
        return null;
    }
  }

  private badgesOf(link: TaskCodeLink): ExternalLinkBadge[] {
    const badges: ExternalLinkBadge[] = [];

    if (link.checks) {
      badges.push(this.checksBadge(link.checks));
    }

    if (link.pullRequestState !== null) {
      badges.push({
        label: this.pullRequestLabel(link.pullRequestState),
        color: pullRequestColors[link.pullRequestState],
      });
    }

    if (link.isRemoved) {
      badges.push({ label: this.labels.deleted, color: 'neutral' });
    }

    return badges;
  }

  private checksBadge(checks: ScmCheckSummary): ExternalLinkBadge {
    const title = $localize`:Tooltip counting a linked commit's CI checks:${checks.passed}:passed: of ${checks.total}:total: passed, ${checks.failed}:failed: failed, ${checks.pending}:pending: running`;

    return {
      label: this.checksLabel(checks.state),
      color: checkColors[checks.state],
      title,
    };
  }

  private checksLabel(state: ScmCheckState): string {
    switch (state) {
      case ScmCheckState.pending:
        return this.labels.checksPending;
      case ScmCheckState.success:
        return this.labels.checksPassed;
      case ScmCheckState.failure:
        return this.labels.checksFailed;
      case ScmCheckState.neutral:
        return this.labels.checksNeutral;
    }
  }

  private pullRequestLabel(state: ScmPullRequestState): string {
    switch (state) {
      case ScmPullRequestState.open:
        return this.labels.open;
      case ScmPullRequestState.draft:
        return this.labels.draft;
      case ScmPullRequestState.merged:
        return this.labels.merged;
      case ScmPullRequestState.closed:
        return this.labels.closed;
    }
  }
}
