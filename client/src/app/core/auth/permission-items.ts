import {
  LucideActivity,
  LucideArchiveRestore,
  LucideArrowRightLeft,
  LucideBell,
  LucideBellDot,
  LucideCalendarDays,
  LucideCirclePlus,
  LucideClipboardList,
  LucideDownload,
  LucideEye,
  LucideFilePen,
  LucideFlag,
  LucideIconInput,
  LucideImage,
  LucideLayoutDashboard,
  LucideMessageCircle,
  LucideMessageCirclePlus,
  LucideMessageCircleX,
  LucideMoveRight,
  LucidePencil,
  LucideShield,
  LucideShieldX,
  LucideSquareX,
  LucideTag,
  LucideTags,
  LucideTrash2,
  LucideUpload,
  LucideUserLock,
  LucideUserMinus,
  LucideUserPlus,
  LucideUsers,
  LucideUserX,
  LucideWorkflow,
} from '@lucide/angular';
import { netptunePermissions } from './permissions';

export interface PermissionMeta {
  key: string;
  label: string;
  icon: LucideIconInput;
}

export type PermissionLabels = {
  [K in keyof typeof netptunePermissions]: {
    [P in keyof (typeof netptunePermissions)[K]]: PermissionMeta;
  };
};

export const netptunePermissionLabels: PermissionLabels = {
  workspace: {
    read: { key: 'workspace.read', label: 'View Workspace', icon: LucideEye },
    create: {
      key: 'workspace.create',
      label: 'Create Workspace',
      icon: LucideCirclePlus,
    },
    update: {
      key: 'workspace.update',
      label: 'Edit Workspace',
      icon: LucidePencil,
    },
    delete: {
      key: 'workspace.delete',
      label: 'Delete Workspace',
      icon: LucideTrash2,
    },
    deletePermanent: {
      key: 'workspace.delete_permanent',
      label: 'Permanently Delete Workspace',
      icon: LucideShieldX,
    },
  },
  members: {
    read: { key: 'members.read', label: 'View Members', icon: LucideUsers },
    invite: {
      key: 'members.invite',
      label: 'Invite Members',
      icon: LucideUserPlus,
    },
    remove: {
      key: 'members.remove',
      label: 'Remove Members',
      icon: LucideUserMinus,
    },
    updateProfile: {
      key: 'members.update_profile',
      label: 'Edit Member Profiles',
      icon: LucideUserX,
    },
    updatePermissions: {
      key: 'members.update_permission',
      label: 'Update Member Permissions',
      icon: LucideUserLock,
    },
    updateRole: {
      key: 'members.update_role',
      label: 'Update Member Roles',
      icon: LucideUserLock,
    },
  },
  projects: {
    read: { key: 'projects.read', label: 'View Projects', icon: LucideEye },
    create: {
      key: 'projects.create',
      label: 'Create Projects',
      icon: LucideCirclePlus,
    },
    update: {
      key: 'projects.update',
      label: 'Edit Projects',
      icon: LucidePencil,
    },
    delete: {
      key: 'projects.delete',
      label: 'Delete Projects',
      icon: LucideTrash2,
    },
  },
  boards: {
    read: { key: 'boards.read', label: 'View Boards', icon: LucideEye },
    create: {
      key: 'boards.create',
      label: 'Create Boards',
      icon: LucideLayoutDashboard,
    },
    update: { key: 'boards.update', label: 'Edit Boards', icon: LucidePencil },
    delete: {
      key: 'boards.delete',
      label: 'Delete Boards',
      icon: LucideTrash2,
    },
  },
  boardGroups: {
    read: {
      key: 'board_groups.read',
      label: 'View Board Groups',
      icon: LucideEye,
    },
    create: {
      key: 'board_groups.create',
      label: 'Create Board Groups',
      icon: LucideCirclePlus,
    },
    update: {
      key: 'board_groups.update',
      label: 'Edit Board Groups',
      icon: LucidePencil,
    },
    delete: {
      key: 'board_groups.delete',
      label: 'Delete Board Groups',
      icon: LucideTrash2,
    },
  },
  tasks: {
    read: { key: 'tasks.read', label: 'View Tasks', icon: LucideClipboardList },
    create: {
      key: 'tasks.create',
      label: 'Create Tasks',
      icon: LucideCirclePlus,
    },
    update: { key: 'tasks.update', label: 'Edit Tasks', icon: LucideFilePen },
    delete: {
      key: 'tasks.delete',
      label: 'Delete Own Tasks',
      icon: LucideTrash2,
    },
    deleteAny: {
      key: 'tasks.delete_any',
      label: 'Delete Any Task',
      icon: LucideSquareX,
    },
    restore: {
      key: 'tasks.restore',
      label: 'Restore Tasks',
      icon: LucideArchiveRestore,
    },
    move: { key: 'tasks.move', label: 'Move Tasks', icon: LucideMoveRight },
    reassign: {
      key: 'tasks.reassign',
      label: 'Reassign Tasks',
      icon: LucideArrowRightLeft,
    },
    export: {
      key: 'tasks.export',
      label: 'Export Tasks',
      icon: LucideDownload,
    },
    import: {
      key: 'tasks.import',
      label: 'Import Tasks',
      icon: LucideUpload,
    },
  },
  sprints: {
    read: {
      key: 'sprints.read',
      label: 'View Sprints',
      icon: LucideCalendarDays,
    },
    create: {
      key: 'sprints.create',
      label: 'Create Sprints',
      icon: LucideCirclePlus,
    },
    update: {
      key: 'sprints.update',
      label: 'Edit Sprints',
      icon: LucidePencil,
    },
    delete: {
      key: 'sprints.delete',
      label: 'Delete Sprints',
      icon: LucideTrash2,
    },
    manageTasks: {
      key: 'sprints.manage_tasks',
      label: 'Manage Sprint Tasks',
      icon: LucideClipboardList,
    },
  },
  comments: {
    read: {
      key: 'comments.read',
      label: 'View Comments',
      icon: LucideMessageCircle,
    },
    create: {
      key: 'comments.create',
      label: 'Post Comments',
      icon: LucideMessageCirclePlus,
    },
    deleteOwn: {
      key: 'comments.delete_own',
      label: 'Delete Own Comments',
      icon: LucideMessageCircleX,
    },
    deleteAny: {
      key: 'comments.delete_any',
      label: 'Delete Any Comment',
      icon: LucideMessageCircleX,
    },
  },
  tags: {
    read: { key: 'tags.read', label: 'View Tags', icon: LucideTag },
    create: {
      key: 'tags.create',
      label: 'Create Tags',
      icon: LucideCirclePlus,
    },
    update: { key: 'tags.update', label: 'Edit Tags', icon: LucidePencil },
    delete: { key: 'tags.delete', label: 'Delete Tags', icon: LucideTrash2 },
    assign: { key: 'tags.assign', label: 'Assign Tags', icon: LucideTags },
  },
  statuses: {
    read: { key: 'statuses.read', label: 'View Statuses', icon: LucideEye },
    manage: {
      key: 'statuses.manage',
      label: 'Manage Statuses',
      icon: LucidePencil,
    },
  },
  relationTypes: {
    read: {
      key: 'relation_types.read',
      label: 'View Relation Types',
      icon: LucideEye,
    },
    manage: {
      key: 'relation_types.manage',
      label: 'Manage Relation Types',
      icon: LucidePencil,
    },
  },
  activity: {
    read: {
      key: 'activity.read',
      label: 'View Activity',
      icon: LucideActivity,
    },
  },
  audit: {
    read: { key: 'audit.read', label: 'View Audit Log', icon: LucideShield },
    export: {
      key: 'audit.export',
      label: 'Export Audit Log',
      icon: LucideDownload,
    },
  },
  notifications: {
    read: {
      key: 'notifications.read',
      label: 'View Notifications',
      icon: LucideBell,
    },
    update: {
      key: 'notifications.update',
      label: 'Manage Notifications',
      icon: LucideBellDot,
    },
  },
  automations: {
    read: {
      key: 'automations.read',
      label: 'View Automations',
      icon: LucideWorkflow,
    },
    manage: {
      key: 'automations.manage',
      label: 'Manage Automations',
      icon: LucideWorkflow,
    },
  },
  flags: {
    read: {
      key: 'flags.read',
      label: 'View Task Flags',
      icon: LucideFlag,
    },
    resolve: {
      key: 'flags.resolve',
      label: 'Resolve Task Flags',
      icon: LucideFlag,
    },
  },
  serviceAccounts: {
    read: {
      key: 'service_accounts.read',
      label: 'View Service Accounts',
      icon: LucideShield,
    },
    create: {
      key: 'service_accounts.create',
      label: 'Create Service Accounts',
      icon: LucideCirclePlus,
    },
    update: {
      key: 'service_accounts.update',
      label: 'Edit Service Accounts',
      icon: LucidePencil,
    },
    delete: {
      key: 'service_accounts.delete',
      label: 'Delete Service Accounts',
      icon: LucideTrash2,
    },
    manageCredentials: {
      key: 'service_accounts.manage_credentials',
      label: 'Manage API Credentials',
      icon: LucideUserLock,
    },
  },
  storage: {
    uploadProfilePicture: {
      key: 'storage.upload_profile_picture',
      label: 'Upload Profile Picture',
      icon: LucideImage,
    },
    uploadMedia: {
      key: 'storage.upload_media',
      label: 'Upload Media',
      icon: LucideUpload,
    },
    read: {
      key: 'storage.read',
      label: 'View Workspace Storage',
      icon: LucideEye,
    },
    manage: {
      key: 'storage.manage',
      label: 'Manage Workspace Storage',
      icon: LucideTrash2,
    },
  },
  files: {
    read: { key: 'files.read', label: 'View Files', icon: LucideEye },
    upload: { key: 'files.upload', label: 'Upload Files', icon: LucideUpload },
    deleteOwn: {
      key: 'files.delete_own',
      label: 'Delete Own Files',
      icon: LucideTrash2,
    },
    deleteAny: {
      key: 'files.delete_any',
      label: 'Delete Any File',
      icon: LucideTrash2,
    },
  },
};
