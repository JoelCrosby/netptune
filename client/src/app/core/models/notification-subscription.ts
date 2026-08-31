export enum NotificationScope {
  project = 0,
  board = 1,
  boardGroup = 2,
  sprint = 3,
}

export enum NotificationSubscriptionEvent {
  taskCreated = 1,
  taskUpdated = 2,
  taskAdded = 4,
  taskRemoved = 8,
}

export interface NotificationSubscription {
  id: number;
  scope: NotificationScope;
  scopeEntityId: number;
  events: number;
  name: string;
  context: string | null;
  link: string;
}

export interface UpsertNotificationSubscriptionRequest {
  scope: NotificationScope;
  scopeEntityId: number;
  events: number;
}

export function hasSubscriptionEvent(
  events: number,
  event: NotificationSubscriptionEvent
): boolean {
  return (events & event) !== 0;
}

export function toggleSubscriptionEvent(
  events: number,
  event: NotificationSubscriptionEvent
): number {
  const isSelected = hasSubscriptionEvent(events, event);

  return isSelected ? events & ~event : events | event;
}
