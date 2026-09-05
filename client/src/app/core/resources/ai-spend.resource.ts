import { PERMISSIONS } from '../auth/permissions';
import { AiSpend } from '../models/ai-spend';
import { ClientResponse } from '../models/client-response';
import { permissionResource } from './permission.resource';

export const aiSpendUrl = 'api/ai/admin/spend';
export const aiSpendCapUrl = 'api/ai/admin/spend-cap';

export const aiSpendResource = () => {
  return permissionResource<AiSpend | null>(
    PERMISSIONS.assistant.readAllConversations,
    () => ({ url: aiSpendUrl }),
    {
      defaultValue: null,
      parse: (response) => {
        return (response as ClientResponse<AiSpend>).payload ?? null;
      },
    }
  );
};
