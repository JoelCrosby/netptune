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
      const meta = value.meta as { emails: string[] };
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
    default:
      return '';
  }
};
