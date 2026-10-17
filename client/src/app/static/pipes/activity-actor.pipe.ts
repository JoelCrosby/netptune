import { Pipe, PipeTransform } from '@angular/core';
import {
  sourceControlProviderFromSlug,
  sourceControlProviderNames,
} from '@core/models/source-control';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';

export interface ActivityActor {
  name: string;
  pictureUrl: string | null;
  isServiceAccount: boolean;
}

interface ExternalActorMeta {
  actorLogin?: string | null;
  actorAvatarUrl?: string | null;
  provider?: string | null;
}

// Code activity can come from someone with no account here, in which case the entry carries the
// provider's username and avatar instead of a user.
@Pipe({ name: 'activityActor', pure: true })
export class ActivityActorPipe implements PipeTransform {
  transform(activity: ActivityViewModel): ActivityActor {
    if (activity.userUsername) {
      return {
        name: activity.userUsername,
        pictureUrl: activity.userPictureUrl,
        isServiceAccount: activity.userIsServiceAccount ?? false,
      };
    }

    const meta = activity.meta as ExternalActorMeta | null;
    const provider = meta?.provider
      ? sourceControlProviderFromSlug(meta.provider)
      : null;
    const providerName =
      provider === null ? null : sourceControlProviderNames[provider];

    return {
      name: meta?.actorLogin ?? providerName ?? '',
      pictureUrl: meta?.actorAvatarUrl ?? null,
      isServiceAccount: false,
    };
  }
}
