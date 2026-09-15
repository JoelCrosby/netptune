export const DELETE_CONVERSATIONS_FAILED = $localize`:Error shown after an action fails:The conversation(s) could not be deleted. Please try again.`;

export function buildConversationsDeletedMessage(count: number): string {
  if (count === 1) {
    return $localize`:Confirmation shown after an assistant conversation is deleted:Conversation deleted`;
  }

  return $localize`:Confirmation shown after assistant conversations are deleted. COUNT is how many:${count}:COUNT: conversations deleted`;
}
