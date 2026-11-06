import {
  ActivityType,
  ActivityViewModel,
} from '@core/models/view-models/activity-view-model';

interface SourceControlActivityMeta {
  reference?: string;
  title?: string;
  commitCount?: number;
  checksTotal?: number;
  checksFailed?: number;
}

export const activityDetail = (value: ActivityViewModel): string => {
  switch (value.type) {
    case ActivityType.move: {
      const meta = value.meta as { group?: string; causedByStatus?: string };
      const group = meta?.group ?? '';
      const status = meta?.causedByStatus;

      if (status) {
        return $localize`:Detail of a card moved because its status changed. GROUP is a board column, STATUS a task status:to ${group}:GROUP: group after the status changed to ${status}:STATUS:`;
      }

      return $localize`:Detail of a card moved to a board column in the activity feed. GROUP is the column name:to ${group}:GROUP: group`;
    }
    case ActivityType.assign:
      return `to ${value.assignee?.displayName ?? '(removed user)'}`;
    case ActivityType.unassign:
      return `from ${value.assignee?.displayName ?? '(removed user)'}`;
    case ActivityType.invite: {
      const meta = value.meta as { emails: string[] };
      return meta?.emails?.join(', ') ?? '';
    }
    case ActivityType.remove: {
      // One type covers a member leaving a workspace and a task coming off a
      // board, so the meta it carries is what tells the two apart.
      const meta = value.meta as { board?: string; emails?: string[] };

      if (meta?.board) return `from ${meta.board}`;

      return meta?.emails?.join(', ') ?? '';
    }
    case ActivityType.permissionChanged: {
      const meta = value.meta as { permission: string; granted: boolean };
      const direction = meta?.granted ? 'granted' : 'revoked';
      return `${meta?.permission ?? ''} (${direction})`;
    }
    case ActivityType.addTag:
    case ActivityType.removeTag: {
      const meta = value.meta as { tagName: string };
      return meta?.tagName ?? '';
    }
    case ActivityType.addRelation:
    case ActivityType.removeRelation: {
      // The label is already written from this task's point of view, so it reads correctly on
      // whichever of the two tasks' feeds this entry appears in.
      const meta = value.meta as {
        label: string;
        relatedTaskSystemId: string;
      };

      if (!meta?.label) return meta?.relatedTaskSystemId ?? '';

      return `${meta.label} ${meta.relatedTaskSystemId ?? ''}`.trim();
    }
    case ActivityType.addFile:
    case ActivityType.removeFile: {
      const meta = value.meta as { fileName: string };
      return meta?.fileName ?? '';
    }
    case ActivityType.pullRequestOpened:
    case ActivityType.pullRequestMerged:
    case ActivityType.pullRequestClosed: {
      const meta = value.meta as SourceControlActivityMeta | null;
      return `#${meta?.reference ?? ''} ${meta?.title ?? ''}`.trim();
    }
    case ActivityType.commitsPushed: {
      const meta = value.meta as SourceControlActivityMeta | null;
      const count = meta?.commitCount ?? 1;
      const commits = count === 1 ? '1 commit' : `${count} commits`;
      return `${commits} to ${meta?.reference ?? ''}`.trim();
    }
    case ActivityType.checksPassed:
      return (value.meta as SourceControlActivityMeta | null)?.reference ?? '';
    case ActivityType.checksFailed: {
      const meta = value.meta as SourceControlActivityMeta | null;
      const failed = meta?.checksFailed ?? 0;
      const total = meta?.checksTotal ?? 0;
      return `${meta?.reference ?? ''} (${failed} of ${total} failed)`;
    }
    case ActivityType.runReview: {
      const meta = value.meta as { outcome?: string; findingCount?: number };
      const isFailed = meta?.outcome === 'failed';
      const count = meta?.findingCount ?? 0;

      if (isFailed) {
        return $localize`:Shown after an assistant review in the activity feed when it failed:(failed)`;
      }

      return count === 1
        ? $localize`:How many findings an assistant review raised, in the activity feed:(1 finding)`
        : $localize`:How many findings an assistant review raised, in the activity feed. COUNT is a number:(${count}:COUNT: findings)`;
    }
    case ActivityType.acceptReviewFinding:
    case ActivityType.dismissReviewFinding:
    case ActivityType.reopenReviewFinding: {
      const meta = value.meta as { title?: string };
      return meta?.title ? `"${meta.title}"` : '';
    }
    default:
      return '';
  }
};
