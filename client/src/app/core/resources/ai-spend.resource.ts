import { PERMISSIONS } from '../auth/permissions';
import { AiSpend } from '../models/ai-spend';
import { permissionResource } from './permission.resource';

export const aiSpendUrl = 'api/ai/admin/spend';
export const aiSpendCapUrl = 'api/ai/admin/spend-cap';

export const aiSpendResource = () => {
  return permissionResource<AiSpend | null>(
    PERMISSIONS.assistant.readAllConversations,
    () => ({ url: aiSpendUrl }),
    {
      defaultValue: null,
      parse: (response) => response.payload ?? null,
    }
  );
};
