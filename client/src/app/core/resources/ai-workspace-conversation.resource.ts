import { PERMISSIONS } from '../auth/permissions';
import { AiWorkspaceConversation } from '../models/ai-workspace-conversation';
import { permissionResource } from './permission.resource';

export const aiWorkspaceConversationResource = () => {
  return permissionResource<AiWorkspaceConversation[]>(
    PERMISSIONS.assistant.readAllConversations,
    () => ({ url: 'api/ai/admin/conversations' }),
    { defaultValue: [] }
  );
};
