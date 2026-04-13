export const netptunePermissions = {
  workspace: {
    read: 'workspace.read',
    create: 'workspace.create',
    update: 'workspace.update',
    delete: 'workspace.delete',
    deletePermanent: 'workspace.delete_permanent',
  },
  members: {
    read: 'members.read',
    invite: 'members.invite',
    remove: 'members.remove',
    updateProfile: 'members.update_profile',
  },
  projects: {
    read: 'projects.read',
    create: 'projects.create',
    update: 'projects.update',
    delete: 'projects.delete',
  },
  boards: {
    read: 'boards.read',
    create: 'boards.create',
    update: 'boards.update',
    delete: 'boards.delete',
  },
  boardGroups: {
    read: 'board_groups.read',
    create: 'board_groups.create',
    update: 'board_groups.update',
    delete: 'board_groups.delete',
  },
  tasks: {
    read: 'tasks.read',
    create: 'tasks.create',
    update: 'tasks.update',
    delete: 'tasks.delete',
    deleteAny: 'tasks.delete_any',
    move: 'tasks.move',
    reassign: 'tasks.reassign',
  },
  comments: {
    read: 'comments.read',
    create: 'comments.create',
    deleteOwn: 'comments.delete_own',
    deleteAny: 'comments.delete_any',
  },
  tags: {
    read: 'tags.read',
    create: 'tags.create',
    update: 'tags.update',
    delete: 'tags.delete',
    assign: 'tags.assign',
  },
  activity: {
    read: 'activity.read',
  },
  storage: {
    uploadProfilePicture: 'storage.upload_profile_picture',
    uploadMedia: 'storage.upload_media',
  },
  export: {
    projectTasks: 'export.tasks',
  },
  import: {
    projectTasks: 'import.tasks',
  },
} as const;

type Values<T> = T[keyof T];
export type Permission = Values<{
  [K in keyof typeof netptunePermissions]: Values<
    (typeof netptunePermissions)[K]
  >;
}>;
