import {
  LucideActivity,
  LucideArrowRightLeft,
  LucideCircleMinus,
  LucideCirclePlus,
  LucideClipboardList,
  LucideDownload,
  LucideEye,
  LucideFilePen,
  LucideIconInput,
  LucideImage,
  LucideLayoutDashboard,
  LucideMessageCircle,
  LucideMessageCirclePlus,
  LucideMessageCircleX,
  LucideMoveRight,
  LucidePencil,
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
    move: { key: 'tasks.move', label: 'Move Tasks', icon: LucideMoveRight },
    reassign: {
      key: 'tasks.reassign',
      label: 'Reassign Tasks',
      icon: LucideArrowRightLeft,
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
  activity: {
    read: {
      key: 'activity.read',
      label: 'View Activity',
      icon: LucideActivity,
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
  },
  export: {
    projectTasks: {
      key: 'export.tasks',
      label: 'Export Tasks',
      icon: LucideDownload,
    },
  },
  import: {
    projectTasks: {
      key: 'import.tasks',
      label: 'Import Tasks',
      icon: LucideCircleMinus,
    },
  },
};
