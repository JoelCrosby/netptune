import { computed, inject, Injectable } from '@angular/core';
import { PageContributor } from '@core/models/pages';
import { workspaceUsersResource } from '@core/resources/user.resource';
import { WorkspaceEventsService } from '@core/sse/workspace-events.service';
import { AvatarStackItem } from '@static/components/avatar-stack/avatar-stack.component';

@Injectable()
export class PagePresenceService {
  private readonly workspaceEvents = inject(WorkspaceEventsService);
  private readonly members = workspaceUsersResource();

  private readonly onlineIds = computed(() => {
    return new Set(this.workspaceEvents.onlineUserIds());
  });

  join(pageId: number) {
    this.workspaceEvents.joinGroup(`[page] ${pageId}`);
  }

  leave() {
    this.workspaceEvents.leaveGroup();
  }

  // Everyone viewing the page comes first, then the people who edited it, most recent first.
  people(contributors: readonly PageContributor[]): AvatarStackItem[] {
    const online = this.onlineIds();
    const contributorIds = new Set(contributors.map((person) => person.userId));
    const viewersOnly = this.members()
      .filter(
        (member) => online.has(member.id) && !contributorIds.has(member.id)
      )
      .map((member) => {
        return toItem(member.id, member.displayName, member.pictureUrl, true);
      });
    const editors = contributors.map((person) => {
      const isOnline = online.has(person.userId);

      return toItem(
        person.userId,
        person.displayName,
        person.pictureUrl,
        isOnline
      );
    });
    const onlineEditors = editors.filter((person) => person.online);
    const offlineEditors = editors.filter((person) => !person.online);

    return [...viewersOnly, ...onlineEditors, ...offlineEditors];
  }
}

function toItem(
  id: string,
  displayName: string,
  pictureUrl: string | null | undefined,
  online: boolean
): AvatarStackItem {
  const label = online
    ? $localize`:Tooltip on the avatar of someone viewing a page now:${displayName}:NAME: is viewing this page`
    : $localize`:Tooltip on the avatar of someone who edited a page:${displayName}:NAME: edited this page`;

  return { id, displayName, pictureUrl, online, label };
}
