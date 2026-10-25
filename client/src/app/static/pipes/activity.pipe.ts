import { Pipe, PipeTransform } from '@angular/core';
import {
  ActivityType,
  ActivityViewModel,
} from '@core/models/view-models/activity-view-model';
import {
  activityEditCount,
  activityTypeToString,
  mergedActivitySummary,
} from '@core/transforms/activity-type';
import { fromNow } from '@core/util/dates';

@Pipe({
  name: 'activity',
  pure: true,
  standalone: true,
})
export class ActivityPipe implements PipeTransform {
  transform(value: ActivityViewModel): string {
    const action = getAction(value);
    const edits = activityEditCount(value.revisionCount);

    const time = fromNow(value.time);

    return `${action}${edits} ${time}`;
  }
}

const getAction = (value: ActivityViewModel) => {
  const merged = mergedActivitySummary(value);

  if (merged) return merged;

  const activityType = activityTypeToString(value.type);
  const meta = getMeta(value);

  return `${activityType} ${meta}`;
};

interface SourceControlActivityMeta {
  reference?: string;
  title?: string;
  commitCount?: number;
  checksTotal?: number;
  checksFailed?: number;
}

const getMeta = (value: ActivityViewModel) => {
  switch (value.type) {
    case ActivityType.move: {
      const meta = value.meta as { group: string };
      return `to ${meta?.group ?? ''} group`;
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
