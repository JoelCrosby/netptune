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
    updatePermissions: 'members.update_permission',
    updateRole: 'members.update_role',
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
    restore: 'tasks.restore',
    move: 'tasks.move',
    reassign: 'tasks.reassign',
    export: 'tasks.export',
    import: 'tasks.import',
  },
  sprints: {
    read: 'sprints.read',
    create: 'sprints.create',
    update: 'sprints.update',
    delete: 'sprints.delete',
    manageTasks: 'sprints.manage_tasks',
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
  statuses: {
    read: 'statuses.read',
    manage: 'statuses.manage',
  },
  relationTypes: {
    read: 'relation_types.read',
    manage: 'relation_types.manage',
  },
  activity: {
    read: 'activity.read',
  },
  audit: {
    read: 'audit.read',
    export: 'audit.export',
  },
  notifications: {
    read: 'notifications.read',
    update: 'notifications.update',
  },
  automations: {
    read: 'automations.read',
    manage: 'automations.manage',
  },
  flags: {
    read: 'flags.read',
    resolve: 'flags.resolve',
  },
  serviceAccounts: {
    read: 'service_accounts.read',
    create: 'service_accounts.create',
    update: 'service_accounts.update',
    delete: 'service_accounts.delete',
    manageCredentials: 'service_accounts.manage_credentials',
  },
  storage: {
    uploadProfilePicture: 'storage.upload_profile_picture',
    uploadMedia: 'storage.upload_media',
    read: 'storage.read',
    manage: 'storage.manage',
  },
  files: {
    read: 'files.read',
    upload: 'files.upload',
    deleteOwn: 'files.delete_own',
    deleteAny: 'files.delete_any',
  },
} as const;

type Values<T> = T[keyof T];
export type Permission = Values<{
  [K in keyof typeof netptunePermissions]: Values<
    (typeof netptunePermissions)[K]
  >;
}>;
