import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { AiCredentialAvailability } from '@core/models/ai-credential';
import { AiModelOption } from '@core/models/ai-model';
import { ClientResponse } from '@core/models/client-response';
import {
  AiChangeSet,
  AiConversation,
  AiConversationDetail,
} from '@core/models/ai-conversation';

export type AiChangeSetAction = 'apply' | 'retry' | 'undo' | 'discard';

export interface AiChangeFieldEdit {
  name: string;
  value: string;
}

export interface AiChangeEditResult {
  changeSet: AiChangeSet | null;
  error: string | null;
}

/** The handler turns a rejected edit into a message; anything else is a bare failure. */
const readEditError = (error: unknown): string => {
  const response = error as { error?: ClientResponse<unknown> };
  const message = response.error?.message;

  if (typeof message === 'string' && message.length > 0) {
    return message;
  }

  return $localize`:Shown when an edit to a proposed change is refused:That change could not be edited.`;
};

@Service()
export class AiApiService {
  private readonly http = inject(HttpClient);

  async listModels(): Promise<AiModelOption[]> {
    const models = await this.http
      .get<AiModelOption[]>('api/ai/models')
      .toPromise();

    return models ?? [];
  }

  async readCredentialAvailability(): Promise<AiCredentialAvailability | null> {
    const availability = await this.http
      .get<AiCredentialAvailability>('api/ai/credentials/availability')
      .toPromise();

    return availability ?? null;
  }

  async listConversations(): Promise<AiConversation[]> {
    const conversations = await this.http
      .get<AiConversation[]>('api/ai/conversations')
      .toPromise();

    return conversations ?? [];
  }

  async readConversation(
    conversationId: string
  ): Promise<AiConversationDetail | null> {
    try {
      const response = await this.http
        .get<ClientResponse<AiConversationDetail>>(
          `api/ai/conversations/${conversationId}`
        )
        .toPromise();

      return response?.payload ?? null;
    } catch {
      return null;
    }
  }

  async deleteConversation(conversationId: string) {
    await this.http
      .delete(`api/ai/conversations/${conversationId}`)
      .toPromise();
  }

  async stopConversation(conversationId: string) {
    try {
      await this.http
        .post(`api/ai/conversations/${conversationId}/stop`, {})
        .toPromise();
    } catch {
      /* The turn ends on its own once the server notices. */
    }
  }

  async readChangeSet(changeSetId: string): Promise<AiChangeSet | null> {
    try {
      const response = await this.http
        .get<ClientResponse<AiChangeSet>>(`api/ai/change-sets/${changeSetId}`)
        .toPromise();

      return response?.payload ?? null;
    } catch {
      /* The turn recovers the proposals from the conversation instead. */
      return null;
    }
  }

  async listConversationChangeSets(
    conversationId: string
  ): Promise<AiChangeSet[]> {
    try {
      const changeSets = await this.http
        .get<AiChangeSet[]>(
          `api/ai/conversations/${conversationId}/change-sets`
        )
        .toPromise();

      return changeSets ?? [];
    } catch {
      return [];
    }
  }

  async readPendingChangeSet(
    conversationId: string
  ): Promise<AiChangeSet | null> {
    try {
      const response = await this.http
        .get<ClientResponse<AiChangeSet>>(
          `api/ai/conversations/${conversationId}/change-set`
        )
        .toPromise();

      return response?.payload ?? null;
    } catch {
      return null;
    }
  }

  async updateChange(
    changeSetId: string,
    changeId: number,
    fields: AiChangeFieldEdit[]
  ): Promise<AiChangeEditResult> {
    try {
      const response = await this.http
        .patch<ClientResponse<AiChangeSet>>(
          `api/ai/change-sets/${changeSetId}/changes/${changeId}`,
          { fields }
        )
        .toPromise();

      return { changeSet: response?.payload ?? null, error: null };
    } catch (error) {
      return { changeSet: null, error: readEditError(error) };
    }
  }

  async runChangeSetAction(
    changeSetId: string,
    action: AiChangeSetAction,
    body: object = {}
  ) {
    await this.http
      .post(`api/ai/change-sets/${changeSetId}/${action}`, body)
      .toPromise();
  }
}
