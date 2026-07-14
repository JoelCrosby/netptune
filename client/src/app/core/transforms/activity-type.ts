import { ActivityType } from '@core/models/view-models/activity-view-model';

/**
 * Enough of an activity to describe it. Both the feed entry and a notification that points at a merged
 * entry can produce this, so they render the same sentence.
 */
export interface MergedActivity {
  type: ActivityType;
  changedFields?: string[] | null;
  revisionCount?: number | null;
  meta?: Record<string, unknown> | null;
}

const STATUS_FIELD = 'status';
const fieldLabel = (field: string): string =>
  field === 'duedate' ? 'due date' : field;

/** The fields the server writes are lower case, but nothing downstream should depend on that. */
const normaliseFields = (fields?: string[] | null): string[] => {
  if (!fields?.length) return [];

  const seen = new Set<string>();

  return fields
    .map((field) => field?.trim().toLowerCase())
    .filter((field): field is string => !!field)
    .filter((field) => (seen.has(field) ? false : (seen.add(field), true)));
};

/** `['a', 'b', 'c']` → `'a, b and c'`. */
const toSentenceList = (values: string[]): string =>
  values.length <= 1
    ? (values[0] ?? '')
    : `${values.slice(0, -1).join(', ')} and ${values[values.length - 1]}`;

/**
 * Status is a milestone people watch for, so it never reads as a generic "updated" — even folded into a
 * merged entry alongside other fields. The new value comes off the merged meta when it is there.
 */
const statusPhrase = (value: MergedActivity): string => {
  const fields = value.meta?.['fields'] as
    Record<string, { new?: string } | undefined> | undefined;

  const status = fields?.[STATUS_FIELD]?.new;

  return status ? `moved to ${status}` : 'changed status';
};

/**
 * The summary for a merged entry — "updated description and priority", "updated description and moved to
 * Done". Returns null when the entry describes a single field or a discrete event, which must keep
 * rendering exactly as it always has.
 */
export const mergedActivitySummary = (value: MergedActivity): string | null => {
  const fields = normaliseFields(value.changedFields);

  if (fields.length < 2) return null;

  const others = fields.filter((field) => field !== STATUS_FIELD);
  const parts: string[] = [];

  if (others.length) {
    parts.push(`updated ${toSentenceList(others.map(fieldLabel))}`);
  }

  if (fields.includes(STATUS_FIELD)) {
    parts.push(statusPhrase(value));
  }

  return parts.join(' and ');
};

/** " · 12 edits" — how many ledger events the entry swallowed. Empty when it swallowed one. */
export const activityEditCount = (revisionCount?: number | null): string =>
  revisionCount && revisionCount > 1 ? ` · ${revisionCount} edits` : '';

export const activityTypeToString = (value: ActivityType): string => {
  switch (value) {
    case ActivityType.assign:
      return 'Assigned';
    case ActivityType.create:
      return 'Created';
    case ActivityType.delete:
      return 'Deleted';
    case ActivityType.modify:
      return 'Modified';
    case ActivityType.move:
      return 'Moved';
    case ActivityType.reorder:
      return 'Reordered';
    case ActivityType.modifyName:
      return 'Modified name';
    case ActivityType.modifyDescription:
      return 'Modified description';
    case ActivityType.modifyStatus:
      return 'Changed status';
    case ActivityType.invite:
      return 'Invited';
    case ActivityType.remove:
      return 'Removed';
    case ActivityType.permissionChanged:
      return 'Changed permissions';
    case ActivityType.unassign:
      return 'Unassigned';
    case ActivityType.addTag:
      return 'Added tag';
    case ActivityType.removeTag:
      return 'Removed tag';
    case ActivityType.addRelation:
      return 'Linked task';
    case ActivityType.removeRelation:
      return 'Unlinked task';
    case ActivityType.roleChanged:
      return 'Changed role';
    case ActivityType.workspaceSettingsChanged:
      return 'Changed workspace settings';
    case ActivityType.exportRequested:
      return 'Requested export';
    case ActivityType.loginSuccess:
      return 'Logged in';
    case ActivityType.loginFailed:
      return 'Failed login';
    case ActivityType.mention:
      return 'Mentioned';
    case ActivityType.modifyPriority:
      return 'Changed priority';
    case ActivityType.modifyEstimate:
      return 'Changed estimate';
    case ActivityType.modifyDueDate:
      return 'Changed due date';
    case ActivityType.restore:
      return 'Restored';
    default:
      return '[UNKNOWN ACTIVITY TYPE]';
  }
};

/**
 * The line a notification renders. A notification that points at a merged entry says what the burst did —
 * "updated description and priority · 12 edits" — instead of naming one representative field. Everything
 * else is unchanged.
 *
 * Note the entry's meta is not carried on the notification feed, so a status change folded into a merged
 * notification reads "changed status" rather than "moved to Done". It is still never a generic "updated".
 */
export const notificationSummary = (value: {
  activityType: ActivityType;
  activityEntryId?: number | null;
  changedFields?: string[] | null;
  revisionCount?: number | null;
}): string => {
  if (!value.activityEntryId) return activityTypeToString(value.activityType);

  const merged = mergedActivitySummary({
    type: value.activityType,
    changedFields: value.changedFields,
    revisionCount: value.revisionCount,
  });

  return `${merged ?? activityTypeToString(value.activityType)}${activityEditCount(value.revisionCount)}`;
};
