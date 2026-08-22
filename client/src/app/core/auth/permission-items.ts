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
  LucideFileArchive,
  LucideFilePen,
  LucideFlag,
  LucideSparkles,
  LucideIconInput,
  LucideImage,
  LucideLayoutDashboard,
  LucideListFilter,
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
import { PERMISSIONS } from './permissions';

export interface PermissionMeta {
  key: string;
  label: string;
  icon: LucideIconInput;
}

export type PermissionLabels = {
  [K in keyof typeof PERMISSIONS]: {
    [P in keyof (typeof PERMISSIONS)[K]]: PermissionMeta;
  };
};

export const netptunePermissionLabels: PermissionLabels = {
  workspace: {
    read: {
      key: 'workspace.read',
      label: $localize`:Name of a workspace permission:View Workspace`,
      icon: LucideEye,
    },
    create: {
      key: 'workspace.create',
      label: $localize`:Name of a workspace permission:Create Workspace`,
      icon: LucideCirclePlus,
    },
    update: {
      key: 'workspace.update',
      label: $localize`:Name of a workspace permission:Edit Workspace`,
      icon: LucidePencil,
    },
    delete: {
      key: 'workspace.delete',
      label: $localize`:Name of a workspace permission:Delete Workspace`,
      icon: LucideTrash2,
    },
    deletePermanent: {
      key: 'workspace.delete_permanent',
      label: $localize`:Name of a workspace permission:Permanently Delete Workspace`,
      icon: LucideShieldX,
    },
  },
  members: {
    read: {
      key: 'members.read',
      label: $localize`:Name of a workspace permission:View Members`,
      icon: LucideUsers,
    },
    invite: {
      key: 'members.invite',
      label: $localize`:Name of a workspace permission:Invite Members`,
      icon: LucideUserPlus,
    },
    remove: {
      key: 'members.remove',
      label: $localize`:Name of a workspace permission:Remove Members`,
      icon: LucideUserMinus,
    },
    updateProfile: {
      key: 'members.update_profile',
      label: $localize`:Name of a workspace permission:Edit Member Profiles`,
      icon: LucideUserX,
    },
    updatePermissions: {
      key: 'members.update_permission',
      label: $localize`:Name of a workspace permission:Update Member Permissions`,
      icon: LucideUserLock,
    },
    updateRole: {
      key: 'members.update_role',
      label: $localize`:Name of a workspace permission:Update Member Roles`,
      icon: LucideUserLock,
    },
  },
  projects: {
    read: {
      key: 'projects.read',
      label: $localize`:Name of a workspace permission:View Projects`,
      icon: LucideEye,
    },
    create: {
      key: 'projects.create',
      label: $localize`:Name of a workspace permission:Create Projects`,
      icon: LucideCirclePlus,
    },
    update: {
      key: 'projects.update',
      label: $localize`:Name of a workspace permission:Edit Projects`,
      icon: LucidePencil,
    },
    delete: {
      key: 'projects.delete',
      label: $localize`:Name of a workspace permission:Delete Projects`,
      icon: LucideTrash2,
    },
  },
  boards: {
    read: {
      key: 'boards.read',
      label: $localize`:Name of a workspace permission:View Boards`,
      icon: LucideEye,
    },
    create: {
      key: 'boards.create',
      label: $localize`:Name of a workspace permission:Create Boards`,
      icon: LucideLayoutDashboard,
    },
    update: {
      key: 'boards.update',
      label: $localize`:Name of a workspace permission:Edit Boards`,
      icon: LucidePencil,
    },
    delete: {
      key: 'boards.delete',
      label: $localize`:Name of a workspace permission:Delete Boards`,
      icon: LucideTrash2,
    },
  },
  boardGroups: {
    read: {
      key: 'board_groups.read',
      label: $localize`:Name of a workspace permission:View Board Groups`,
      icon: LucideEye,
    },
    create: {
      key: 'board_groups.create',
      label: $localize`:Name of a workspace permission:Create Board Groups`,
      icon: LucideCirclePlus,
    },
    update: {
      key: 'board_groups.update',
      label: $localize`:Name of a workspace permission:Edit Board Groups`,
      icon: LucidePencil,
    },
    delete: {
      key: 'board_groups.delete',
      label: $localize`:Name of a workspace permission:Delete Board Groups`,
      icon: LucideTrash2,
    },
  },
  tasks: {
    read: {
      key: 'tasks.read',
      label: $localize`:Name of a workspace permission:View Tasks`,
      icon: LucideClipboardList,
    },
    create: {
      key: 'tasks.create',
      label: $localize`:Name of a workspace permission:Create Tasks`,
      icon: LucideCirclePlus,
    },
    update: {
      key: 'tasks.update',
      label: $localize`:Name of a workspace permission:Edit Tasks`,
      icon: LucideFilePen,
    },
    delete: {
      key: 'tasks.delete',
      label: $localize`:Name of a workspace permission:Delete Own Tasks`,
      icon: LucideTrash2,
    },
    deleteAny: {
      key: 'tasks.delete_any',
      label: $localize`:Name of a workspace permission:Delete Any Task`,
      icon: LucideSquareX,
    },
    restore: {
      key: 'tasks.restore',
      label: $localize`:Name of a workspace permission:Restore Tasks`,
      icon: LucideArchiveRestore,
    },
    move: {
      key: 'tasks.move',
      label: $localize`:Name of a workspace permission:Move Tasks`,
      icon: LucideMoveRight,
    },
    reassign: {
      key: 'tasks.reassign',
      label: $localize`:Name of a workspace permission:Reassign Tasks`,
      icon: LucideArrowRightLeft,
    },
    export: {
      key: 'tasks.export',
      label: $localize`:Name of a workspace permission:Export Tasks`,
      icon: LucideDownload,
    },
    import: {
      key: 'tasks.import',
      label: $localize`:Name of a workspace permission:Import Tasks`,
      icon: LucideUpload,
    },
  },
  taskViews: {
    read: {
      key: 'task_views.read',
      label: $localize`:Name of a workspace permission:View Task Views`,
      icon: LucideListFilter,
    },
    create: {
      key: 'task_views.create',
      label: $localize`:Name of a workspace permission:Create Task Views`,
      icon: LucideCirclePlus,
    },
    update: {
      key: 'task_views.update',
      label: $localize`:Name of a workspace permission:Edit Task Views`,
      icon: LucideFilePen,
    },
    delete: {
      key: 'task_views.delete',
      label: $localize`:Name of a workspace permission:Delete Task Views`,
      icon: LucideTrash2,
    },
    manageShared: {
      key: 'task_views.manage_shared',
      label: $localize`:Name of a workspace permission:Manage Shared Task Views`,
      icon: LucideUsers,
    },
  },
  data: {
    export: {
      key: 'data.export',
      label: $localize`:Name of a workspace permission:Export Data`,
      icon: LucideDownload,
    },
    import: {
      key: 'data.import',
      label: $localize`:Name of a workspace permission:Import Data`,
      icon: LucideUpload,
    },
    manageDefinitions: {
      key: 'data.manage_definitions',
      label: $localize`:Name of a workspace permission:Manage Export Definitions`,
      icon: LucideFilePen,
    },
    exportArchive: {
      key: 'data.export_archive',
      label: $localize`:Name of a workspace permission:Export Workspace Archive`,
      icon: LucideFileArchive,
    },
    importArchive: {
      key: 'data.import_archive',
      label: $localize`:Name of a workspace permission:Import Workspace Archive`,
      icon: LucideArchiveRestore,
    },
  },
  sprints: {
    read: {
      key: 'sprints.read',
      label: $localize`:Name of a workspace permission:View Sprints`,
      icon: LucideCalendarDays,
    },
    create: {
      key: 'sprints.create',
      label: $localize`:Name of a workspace permission:Create Sprints`,
      icon: LucideCirclePlus,
    },
    update: {
      key: 'sprints.update',
      label: $localize`:Name of a workspace permission:Edit Sprints`,
      icon: LucidePencil,
    },
    delete: {
      key: 'sprints.delete',
      label: $localize`:Name of a workspace permission:Delete Sprints`,
      icon: LucideTrash2,
    },
    manageTasks: {
      key: 'sprints.manage_tasks',
      label: $localize`:Name of a workspace permission:Manage Sprint Tasks`,
      icon: LucideClipboardList,
    },
  },
  comments: {
    read: {
      key: 'comments.read',
      label: $localize`:Name of a workspace permission:View Comments`,
      icon: LucideMessageCircle,
    },
    create: {
      key: 'comments.create',
      label: $localize`:Name of a workspace permission:Post Comments`,
      icon: LucideMessageCirclePlus,
    },
    deleteOwn: {
      key: 'comments.delete_own',
      label: $localize`:Name of a workspace permission:Delete Own Comments`,
      icon: LucideMessageCircleX,
    },
    deleteAny: {
      key: 'comments.delete_any',
      label: $localize`:Name of a workspace permission:Delete Any Comment`,
      icon: LucideMessageCircleX,
    },
  },
  tags: {
    read: {
      key: 'tags.read',
      label: $localize`:Name of a workspace permission:View Tags`,
      icon: LucideTag,
    },
    create: {
      key: 'tags.create',
      label: $localize`:Name of a workspace permission:Create Tags`,
      icon: LucideCirclePlus,
    },
    update: {
      key: 'tags.update',
      label: $localize`:Name of a workspace permission:Edit Tags`,
      icon: LucidePencil,
    },
    delete: {
      key: 'tags.delete',
      label: $localize`:Name of a workspace permission:Delete Tags`,
      icon: LucideTrash2,
    },
    assign: {
      key: 'tags.assign',
      label: $localize`:Name of a workspace permission:Assign Tags`,
      icon: LucideTags,
    },
  },
  statuses: {
    read: {
      key: 'statuses.read',
      label: $localize`:Name of a workspace permission:View Statuses`,
      icon: LucideEye,
    },
    manage: {
      key: 'statuses.manage',
      label: $localize`:Name of a workspace permission:Manage Statuses`,
      icon: LucidePencil,
    },
  },
  relationTypes: {
    read: {
      key: 'relation_types.read',
      label: $localize`:Name of a workspace permission:View Relation Types`,
      icon: LucideEye,
    },
    manage: {
      key: 'relation_types.manage',
      label: $localize`:Name of a workspace permission:Manage Relation Types`,
      icon: LucidePencil,
    },
  },
  activity: {
    read: {
      key: 'activity.read',
      label: $localize`:Name of a workspace permission:View Activity`,
      icon: LucideActivity,
    },
  },
  audit: {
    read: {
      key: 'audit.read',
      label: $localize`:Name of a workspace permission:View Audit Log`,
      icon: LucideShield,
    },
    export: {
      key: 'audit.export',
      label: $localize`:Name of a workspace permission:Export Audit Log`,
      icon: LucideDownload,
    },
  },
  notifications: {
    read: {
      key: 'notifications.read',
      label: $localize`:Name of a workspace permission:View Notifications`,
      icon: LucideBell,
    },
    update: {
      key: 'notifications.update',
      label: $localize`:Name of a workspace permission:Manage Notifications`,
      icon: LucideBellDot,
    },
  },
  automations: {
    read: {
      key: 'automations.read',
      label: $localize`:Name of a workspace permission:View Automations`,
      icon: LucideWorkflow,
    },
    manage: {
      key: 'automations.manage',
      label: $localize`:Name of a workspace permission:Manage Automations`,
      icon: LucideWorkflow,
    },
  },
  assistant: {
    readAllConversations: {
      key: 'assistant.read_all_conversations',
      label: $localize`:Name of a workspace permission:View All Assistant Conversations`,
      icon: LucideSparkles,
    },
  },
  flags: {
    read: {
      key: 'flags.read',
      label: $localize`:Name of a workspace permission:View Task Flags`,
      icon: LucideFlag,
    },
    resolve: {
      key: 'flags.resolve',
      label: $localize`:Name of a workspace permission:Resolve Task Flags`,
      icon: LucideFlag,
    },
  },
  serviceAccounts: {
    read: {
      key: 'service_accounts.read',
      label: $localize`:Name of a workspace permission:View Service Accounts`,
      icon: LucideShield,
    },
    create: {
      key: 'service_accounts.create',
      label: $localize`:Name of a workspace permission:Create Service Accounts`,
      icon: LucideCirclePlus,
    },
    update: {
      key: 'service_accounts.update',
      label: $localize`:Name of a workspace permission:Edit Service Accounts`,
      icon: LucidePencil,
    },
    delete: {
      key: 'service_accounts.delete',
      label: $localize`:Name of a workspace permission:Delete Service Accounts`,
      icon: LucideTrash2,
    },
    manageCredentials: {
      key: 'service_accounts.manage_credentials',
      label: $localize`:Name of a workspace permission:Manage API Credentials`,
      icon: LucideUserLock,
    },
  },
  storage: {
    uploadProfilePicture: {
      key: 'storage.upload_profile_picture',
      label: $localize`:Name of a workspace permission:Upload Profile Picture`,
      icon: LucideImage,
    },
    uploadMedia: {
      key: 'storage.upload_media',
      label: $localize`:Name of a workspace permission:Upload Media`,
      icon: LucideUpload,
    },
    read: {
      key: 'storage.read',
      label: $localize`:Name of a workspace permission:View Workspace Storage`,
      icon: LucideEye,
    },
    manage: {
      key: 'storage.manage',
      label: $localize`:Name of a workspace permission:Manage Workspace Storage`,
      icon: LucideTrash2,
    },
  },
  files: {
    read: {
      key: 'files.read',
      label: $localize`:Name of a workspace permission:View Files`,
      icon: LucideEye,
    },
    upload: {
      key: 'files.upload',
      label: $localize`:Name of a workspace permission:Upload Files`,
      icon: LucideUpload,
    },
    deleteOwn: {
      key: 'files.delete_own',
      label: $localize`:Name of a workspace permission:Delete Own Files`,
      icon: LucideTrash2,
    },
    deleteAny: {
      key: 'files.delete_any',
      label: $localize`:Name of a workspace permission:Delete Any File`,
      icon: LucideTrash2,
    },
  },
};
