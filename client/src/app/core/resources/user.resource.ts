import { computed, inject, Signal } from '@angular/core';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { SessionService } from '@core/services/session.service';
import { PERMISSONS } from '../auth/permissions';
import { AssigneeViewModel } from '../models/view-models/board-view';
import { WorkspaceAppUser } from '../models/appuser';
import { ClientResponse } from '../models/client-response';
import { MAX_PAGE_SIZE, Page } from '../models/pagination';
import { WorkspaceRole } from '../enums/workspace-role';
import { permissionResource } from './permission-resource';

export const userResource = () => {
  return permissionResource<ClientResponse<Page<WorkspaceAppUser>>>(
    PERMISSONS.members.read,
    () => ({
      url: 'api/users',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    })
  );
};

export const userDetailResource = (userId: Signal<string | undefined>) => {
  return permissionResource<WorkspaceAppUser>(
    PERMISSONS.members.read,
    () => {
      const id = userId();

      return id ? { url: `api/users/${id}` } : undefined;
    },
    { refreshOn: ['users'] }
  );
};

export const workspaceUsersResource = (): Signal<WorkspaceAppUser[]> => {
  const isPublicViewer = inject(SessionService).isPublicViewer;
  const workspaceKey = inject(CurrentWorkspaceService).slug;

  const members = permissionResource<WorkspaceAppUser[]>(
    PERMISSONS.members.read,
    () => {
      if (isPublicViewer()) return undefined;

      return {
        url: 'api/users',
        params: { page: 1, pageSize: MAX_PAGE_SIZE },
      };
    },
    {
      defaultValue: [],
      refreshOn: ['users'],
      parse: (response) => {
        return (
          (response as ClientResponse<Page<WorkspaceAppUser>>).payload?.items ??
          []
        );
      },
    }
  );

  const publicMembers = permissionResource<WorkspaceAppUser[]>(
    PERMISSONS.tasks.read,
    () => {
      const key = workspaceKey();

      if (!isPublicViewer() || !key) return undefined;

      return {
        url: `api/public/workspaces/${key}/members`,
        params: { page: 1, pageSize: MAX_PAGE_SIZE },
      };
    },
    {
      defaultValue: [],
      parse: (response) => {
        const page = response as Page<AssigneeViewModel>;

        return page.items.map(toWorkspaceUser);
      },
    }
  );

  return computed(() => {
    return isPublicViewer() ? publicMembers.value() : members.value();
  });
};

function toWorkspaceUser(member: AssigneeViewModel): WorkspaceAppUser {
  return {
    id: member.id,
    firstname: '',
    lastname: '',
    email: '',
    userName: '',
    displayName: member.displayName,
    pictureUrl: member.pictureUrl,
    isServiceAccount: member.isServiceAccount,
    lastLoginTime: new Date(0),
    registrationDate: new Date(0),
    token: '',
    tasks: [],
    permissions: [],
    role: WorkspaceRole.viewer,
  };
}
