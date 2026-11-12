import {
  ActivityType,
  ActivityViewModel,
} from '@core/models/view-models/activity-view-model';
import {
  compactTime,
  dayHeading,
  localDayKey,
  timeRange,
} from '@core/util/dates';
import { appLocale } from '@core/util/locale';
import {
  LucideArrowLeftRight,
  LucideCircleCheck,
  LucideCircleX,
  LucideGitCommitHorizontal,
  LucideGitPullRequest,
  type LucideIconInput,
  LucideLink,
  LucideMessageSquare,
  LucidePaperclip,
  LucidePencil,
  LucidePlus,
  LucidePower,
  LucideRotateCcw,
  LucideSparkles,
  LucideTag,
  LucideTrash2,
  LucideUserMinus,
  LucideUserPlus,
} from '@lucide/angular';
import {
  EventTimelineGroup,
  EventTimelineRow,
  EventTimelineSegment,
} from '@static/components/event-timeline/event-timeline.component';
import { SegmentedOption } from '@static/components/segmented-control/segmented-control.component';
import { type Tone } from '@static/components/tones';
import { activityActor } from '@static/pipes/activity-actor.pipe';

export type ActivityTimelineCategory =
  | 'all'
  | 'status'
  | 'edits'
  | 'tags'
  | 'people'
  | 'deleted'
  | 'assistant'
  | 'code';

type FilterCategory = Exclude<ActivityTimelineCategory, 'all'>;

export interface ActivityTimelineContext {
  workspaceSlug: string | undefined;
  scope: { kind: 'board'; boardName: string } | { kind: 'task' };
  now: Date;
}

interface Presentation {
  category: FilterCategory | null;
  icon: LucideIconInput;
  tone: Tone;
}

interface Phrase {
  summary: EventTimelineSegment[];
  note: string | null;
}

interface SourceControlMeta {
  reference?: string;
  title?: string;
  commitCount?: number;
  checksTotal?: number;
  checksFailed?: number;
}

const STATUS_FIELD = 'status';

const statusPresentation: Presentation = {
  category: 'status',
  icon: LucideArrowLeftRight,
  tone: 'purple',
};

const changedFieldsOf = (activity: ActivityViewModel): string[] => {
  const fields = (activity.changedFields ?? [])
    .map((field) => field.trim().toLowerCase())
    .filter(Boolean);

  return [...new Set(fields)];
};

const presentationOf = (activity: ActivityViewModel): Presentation => {
  const isMergedStatusChange =
    activity.type === ActivityType.modify &&
    changedFieldsOf(activity).includes(STATUS_FIELD);

  if (isMergedStatusChange) return statusPresentation;

  switch (activity.type) {
    case ActivityType.move:
    case ActivityType.modifyStatus:
      return statusPresentation;
    case ActivityType.create:
      return { category: 'edits', icon: LucidePlus, tone: 'green' };
    case ActivityType.restore:
      return { category: 'edits', icon: LucideRotateCcw, tone: 'green' };
    case ActivityType.addComment:
    case ActivityType.modifyComment:
    case ActivityType.removeComment:
      return { category: 'edits', icon: LucideMessageSquare, tone: 'neutral' };
    case ActivityType.addFile:
    case ActivityType.removeFile:
      return { category: 'edits', icon: LucidePaperclip, tone: 'neutral' };
    case ActivityType.addRelation:
    case ActivityType.removeRelation:
      return { category: 'edits', icon: LucideLink, tone: 'neutral' };
    case ActivityType.enable:
    case ActivityType.disable:
      return { category: 'edits', icon: LucidePower, tone: 'neutral' };
    case ActivityType.modify:
    case ActivityType.modifyName:
    case ActivityType.modifyDescription:
    case ActivityType.modifyPriority:
    case ActivityType.modifyEstimate:
    case ActivityType.modifyDueDate:
    case ActivityType.modifyStartDate:
    case ActivityType.reorder:
      return { category: 'edits', icon: LucidePencil, tone: 'neutral' };
    case ActivityType.addTag:
    case ActivityType.removeTag:
      return { category: 'tags', icon: LucideTag, tone: 'amber' };
    case ActivityType.assign:
      return { category: 'people', icon: LucideUserPlus, tone: 'blue' };
    case ActivityType.unassign:
      return { category: 'people', icon: LucideUserMinus, tone: 'blue' };
    case ActivityType.delete:
    case ActivityType.remove:
      return { category: 'deleted', icon: LucideTrash2, tone: 'red' };
    case ActivityType.runReview:
    case ActivityType.acceptReviewFinding:
    case ActivityType.dismissReviewFinding:
    case ActivityType.reopenReviewFinding:
      return { category: 'assistant', icon: LucideSparkles, tone: 'pink' };
    case ActivityType.pullRequestOpened:
    case ActivityType.pullRequestMerged:
    case ActivityType.pullRequestClosed:
      return { category: 'code', icon: LucideGitPullRequest, tone: 'purple' };
    case ActivityType.commitsPushed:
      return {
        category: 'code',
        icon: LucideGitCommitHorizontal,
        tone: 'neutral',
      };
    case ActivityType.checksPassed:
      return { category: 'code', icon: LucideCircleCheck, tone: 'green' };
    case ActivityType.checksFailed:
      return { category: 'code', icon: LucideCircleX, tone: 'red' };
    default:
      return { category: null, icon: LucidePencil, tone: 'neutral' };
  }
};

// A translation can move a placeholder anywhere in its sentence, so emphasised values pass through
// $localize as private-use characters and are split back out once the sentence is translated.
const SLOT_BASE = 0xe000;
const FIRST = String.fromCharCode(SLOT_BASE);
const SECOND = String.fromCharCode(SLOT_BASE + 1);

const segmentsOf = (
  text: string,
  values: readonly string[] = []
): EventTimelineSegment[] => {
  const segments: EventTimelineSegment[] = [];
  let plain = '';

  for (const char of text) {
    const value: string | undefined = values[char.charCodeAt(0) - SLOT_BASE];

    if (value === undefined) {
      plain += char;
      continue;
    }

    if (plain) segments.push({ text: plain });

    segments.push({ text: value, strong: true });
    plain = '';
  }

  if (plain) segments.push({ text: plain });

  return segments;
};

const removedUser = () => {
  return $localize`:Stands in for a person who has since been removed from the workspace, in the activity timeline:a removed user`;
};

const fieldLabel = (field: string): string => {
  switch (field) {
    case 'name':
      return $localize`:activity field|A task field listed in a merged activity timeline entry, as in updated name and priority:name`;
    case 'description':
      return $localize`:activity field|A task field listed in a merged activity timeline entry, as in updated name and description:description`;
    case 'assignees':
      return $localize`:activity field|A task field listed in a merged activity timeline entry, as in updated name and assignees:assignees`;
    case 'owner':
      return $localize`:activity field|A task field listed in a merged activity timeline entry, as in updated name and owner:owner`;
    case 'priority':
      return $localize`:activity field|A task field listed in a merged activity timeline entry, as in updated name and priority:priority`;
    case 'estimate':
      return $localize`:activity field|A task field listed in a merged activity timeline entry, as in updated name and estimate:estimate`;
    case 'duedate':
      return $localize`:activity field|A task field listed in a merged activity timeline entry, as in updated name and due date:due date`;
    case 'startdate':
      return $localize`:activity field|A task field listed in a merged activity timeline entry, as in updated name and start date:start date`;
    case 'tags':
      return $localize`:activity field|A task field listed in a merged activity timeline entry, as in updated name and tags:tags`;
    case 'sprint':
      return $localize`:activity field|A task field listed in a merged activity timeline entry, as in updated name and sprint:sprint`;
    case 'boardgroup':
      return $localize`:activity field|The board group of a task, listed in a merged activity timeline entry, as in updated name and group:group`;
    default:
      return field;
  }
};

const fieldList = new Intl.ListFormat(appLocale, { type: 'conjunction' });

const mergedStatusOf = (activity: ActivityViewModel): string | null => {
  const fields = activity.meta?.['fields'] as
    Record<string, { new?: string } | undefined> | undefined;

  return fields?.[STATUS_FIELD]?.new ?? null;
};

const mergedSummaryOf = (
  activity: ActivityViewModel
): EventTimelineSegment[] | null => {
  const fields = changedFieldsOf(activity);

  if (fields.length < 2) return null;

  const others = fields
    .filter((field) => field !== STATUS_FIELD)
    .map(fieldLabel);
  const list = fieldList.format(others);
  const hasStatus = fields.includes(STATUS_FIELD);

  if (!hasStatus) {
    return segmentsOf(
      $localize`:Activity timeline entry after the person's name for several fields changed at once. FIELDS is a list such as name and priority:updated ${list}:FIELDS:`
    );
  }

  const status = mergedStatusOf(activity);

  if (!status) {
    return segmentsOf(
      $localize`:Activity timeline entry after the person's name for several fields changed at once, the status among them. FIELDS is a list such as name and priority:updated ${list}:FIELDS: and changed the status`
    );
  }

  return segmentsOf(
    $localize`:Activity timeline entry after the person's name for several fields changed at once, ending with a move to a new status. FIELDS is a list such as name and priority:updated ${list}:FIELDS: and moved to ${FIRST}:STATUS:`,
    [status]
  );
};

const relationSummaryOf = (
  activity: ActivityViewModel
): EventTimelineSegment[] => {
  const meta = activity.meta as {
    label?: string;
    relatedTaskSystemId?: string;
  } | null;
  const task = meta?.relatedTaskSystemId;
  const label = meta?.label;
  const isLink = activity.type === ActivityType.addRelation;

  if (!task) {
    return segmentsOf(
      isLink
        ? $localize`:Activity timeline entry after the person's name when they linked this task to another:linked a task`
        : $localize`:Activity timeline entry after the person's name when they removed a link between this task and another:unlinked a task`
    );
  }

  if (!label) {
    return segmentsOf(
      isLink
        ? $localize`:Activity timeline entry after the person's name when they linked this task to another. TASK is the other task's id:linked ${FIRST}:TASK:`
        : $localize`:Activity timeline entry after the person's name when they removed a link to another task. TASK is the other task's id:unlinked ${FIRST}:TASK:`,
      [task]
    );
  }

  return segmentsOf(
    isLink
      ? $localize`:Activity timeline entry after the person's name when they linked this task to another. TASK is the other task's id and RELATION says how they relate, as in blocks:linked ${FIRST}:TASK: (${label}:RELATION:)`
      : $localize`:Activity timeline entry after the person's name when they removed a link to another task. TASK is the other task's id and RELATION says how they related, as in blocks:unlinked ${FIRST}:TASK: (${label}:RELATION:)`,
    [task]
  );
};

const pullRequestSummaryOf = (
  activity: ActivityViewModel
): EventTimelineSegment[] => {
  const meta = activity.meta as SourceControlMeta | null;
  const reference = meta?.reference;

  if (!reference) {
    switch (activity.type) {
      case ActivityType.pullRequestMerged:
        return segmentsOf(
          $localize`:Activity timeline entry after the person's name when they merged a pull request:merged a pull request`
        );
      case ActivityType.pullRequestClosed:
        return segmentsOf(
          $localize`:Activity timeline entry after the person's name when they closed a pull request:closed a pull request`
        );
      default:
        return segmentsOf(
          $localize`:Activity timeline entry after the person's name when they opened a pull request:opened a pull request`
        );
    }
  }

  const pullRequest = `#${reference} ${meta?.title ?? ''}`.trim();

  switch (activity.type) {
    case ActivityType.pullRequestMerged:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they merged a pull request. PULL_REQUEST is its number and title:merged pull request ${FIRST}:PULL_REQUEST:`,
        [pullRequest]
      );
    case ActivityType.pullRequestClosed:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they closed a pull request. PULL_REQUEST is its number and title:closed pull request ${FIRST}:PULL_REQUEST:`,
        [pullRequest]
      );
    default:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they opened a pull request. PULL_REQUEST is its number and title:opened pull request ${FIRST}:PULL_REQUEST:`,
        [pullRequest]
      );
  }
};

const commitsSummaryOf = (
  activity: ActivityViewModel
): EventTimelineSegment[] => {
  const meta = activity.meta as SourceControlMeta | null;
  const count = meta?.commitCount ?? 1;
  const branch = meta?.reference;

  if (!branch) {
    return segmentsOf(
      count === 1
        ? $localize`:Activity timeline entry after the person's name when they pushed one commit:pushed 1 commit`
        : $localize`:Activity timeline entry after the person's name when they pushed commits. COUNT is a number above one:pushed ${count}:COUNT: commits`
    );
  }

  return segmentsOf(
    count === 1
      ? $localize`:Activity timeline entry after the person's name when they pushed one commit. BRANCH is the branch name:pushed 1 commit to ${FIRST}:BRANCH:`
      : $localize`:Activity timeline entry after the person's name when they pushed commits. COUNT is a number above one and BRANCH is the branch name:pushed ${count}:COUNT: commits to ${FIRST}:BRANCH:`,
    [branch]
  );
};

const checksSummaryOf = (
  activity: ActivityViewModel
): EventTimelineSegment[] => {
  const meta = activity.meta as SourceControlMeta | null;
  const reference = meta?.reference;
  const failed = meta?.checksFailed ?? 0;
  const total = meta?.checksTotal ?? 0;
  const isPassed = activity.type === ActivityType.checksPassed;

  if (!reference) {
    return segmentsOf(
      isPassed
        ? $localize`:Activity timeline entry after the code host's name when its checks passed:checks passed`
        : $localize`:Activity timeline entry after the code host's name when some checks failed. FAILED and TOTAL are numbers:checks failed (${failed}:FAILED: of ${total}:TOTAL:)`
    );
  }

  return segmentsOf(
    isPassed
      ? $localize`:Activity timeline entry after the code host's name when its checks passed. REFERENCE is a branch or commit:checks passed on ${FIRST}:REFERENCE:`
      : $localize`:Activity timeline entry after the code host's name when some checks failed. REFERENCE is a branch or commit, FAILED and TOTAL are numbers:checks failed on ${FIRST}:REFERENCE: (${failed}:FAILED: of ${total}:TOTAL:)`,
    [reference]
  );
};

const reviewSummaryOf = (
  activity: ActivityViewModel
): EventTimelineSegment[] => {
  const meta = activity.meta as {
    outcome?: string;
    findingCount?: number;
  } | null;
  const isFailed = meta?.outcome === 'failed';
  const count = meta?.findingCount ?? 0;

  if (isFailed) {
    return segmentsOf(
      $localize`:Activity timeline entry after the person's name when an assistant review they ran failed:ran an assistant review, which failed`
    );
  }

  return segmentsOf(
    count === 1
      ? $localize`:Activity timeline entry after the person's name when an assistant review they ran raised one finding:ran an assistant review that raised 1 finding`
      : $localize`:Activity timeline entry after the person's name for an assistant review they ran. COUNT is a number:ran an assistant review that raised ${count}:COUNT: findings`
  );
};

const findingSummaryOf = (
  activity: ActivityViewModel
): EventTimelineSegment[] => {
  const title = (activity.meta as { title?: string } | null)?.title;

  switch (activity.type) {
    case ActivityType.acceptReviewFinding:
      return title
        ? segmentsOf(
            $localize`:Activity timeline entry after the person's name when they accepted a finding from an assistant review. FINDING is its title:accepted the review finding “${FIRST}:FINDING:”`,
            [title]
          )
        : segmentsOf(
            $localize`:Activity timeline entry after the person's name when they accepted a finding from an assistant review:accepted a review finding`
          );
    case ActivityType.dismissReviewFinding:
      return title
        ? segmentsOf(
            $localize`:Activity timeline entry after the person's name when they dismissed a finding from an assistant review. FINDING is its title:dismissed the review finding “${FIRST}:FINDING:”`,
            [title]
          )
        : segmentsOf(
            $localize`:Activity timeline entry after the person's name when they dismissed a finding from an assistant review:dismissed a review finding`
          );
    default:
      return title
        ? segmentsOf(
            $localize`:Activity timeline entry after the person's name when they reopened a finding from an assistant review. FINDING is its title:reopened the review finding “${FIRST}:FINDING:”`,
            [title]
          )
        : segmentsOf(
            $localize`:Activity timeline entry after the person's name when they reopened a finding from an assistant review:reopened a review finding`
          );
  }
};

const summaryOf = (activity: ActivityViewModel): EventTimelineSegment[] => {
  switch (activity.type) {
    case ActivityType.create:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they created a task or board:created`
      );
    case ActivityType.modify:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they changed a task or board:made changes`
      );
    case ActivityType.delete:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they deleted a task or board:deleted`
      );
    case ActivityType.restore:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they restored a deleted task:restored`
      );
    case ActivityType.reorder:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they reordered tasks on a board:reordered`
      );
    case ActivityType.move: {
      const meta = activity.meta as {
        group?: string;
        causedByStatus?: string;
      } | null;
      const group = meta?.group;
      const status = meta?.causedByStatus;

      if (group && status) {
        return segmentsOf(
          $localize`:Activity timeline entry after the person's name when a status change moved a task to the board group mapped to that status. GROUP is the group's name, STATUS the new status:moved to ${FIRST}:GROUP: after the status changed to ${SECOND}:STATUS:`,
          [group, status]
        );
      }

      return group
        ? segmentsOf(
            $localize`:Activity timeline entry after the person's name when they moved a task to a board group. GROUP is the group's name:moved to ${FIRST}:GROUP:`,
            [group]
          )
        : segmentsOf(
            $localize`:Activity timeline entry after the person's name when they moved a task to another board group:moved`
          );
    }
    case ActivityType.modifyName:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they renamed a task or board:renamed`
      );
    case ActivityType.modifyDescription:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they changed a description:changed the description`
      );
    case ActivityType.modifyStatus:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they changed a task's status:changed the status`
      );
    case ActivityType.modifyPriority:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they changed a task's priority:changed the priority`
      );
    case ActivityType.modifyEstimate:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they changed a task's estimate:changed the estimate`
      );
    case ActivityType.modifyDueDate:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they changed a task's due date:changed the due date`
      );
    case ActivityType.modifyStartDate:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they changed a task's start date:changed the start date`
      );
    case ActivityType.assign:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they assigned someone to a task. PERSON is that person's name:assigned ${FIRST}:PERSON:`,
        [activity.assignee?.displayName ?? removedUser()]
      );
    case ActivityType.unassign:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they took someone off a task. PERSON is that person's name:unassigned ${FIRST}:PERSON:`,
        [activity.assignee?.displayName ?? removedUser()]
      );
    case ActivityType.addTag:
    case ActivityType.removeTag: {
      const tag = (activity.meta as { tagName?: string } | null)?.tagName;
      const isAdded = activity.type === ActivityType.addTag;

      if (!tag) {
        return segmentsOf(
          isAdded
            ? $localize`:Activity timeline entry after the person's name when they tagged a task:added a tag`
            : $localize`:Activity timeline entry after the person's name when they removed a tag from a task:removed a tag`
        );
      }

      return segmentsOf(
        isAdded
          ? $localize`:Activity timeline entry after the person's name when they tagged a task. TAG is the tag's name:added the tag ${FIRST}:TAG:`
          : $localize`:Activity timeline entry after the person's name when they removed a tag from a task. TAG is the tag's name:removed the tag ${FIRST}:TAG:`,
        [tag]
      );
    }
    case ActivityType.remove: {
      const board = (activity.meta as { board?: string } | null)?.board;

      return board
        ? segmentsOf(
            $localize`:Activity timeline entry after the person's name when they took a task off a board. BOARD is the board's name:removed it from ${FIRST}:BOARD:`,
            [board]
          )
        : segmentsOf(
            $localize`:Activity timeline entry after the person's name when they removed something from a board:removed`
          );
    }
    case ActivityType.addRelation:
    case ActivityType.removeRelation:
      return relationSummaryOf(activity);
    case ActivityType.addFile:
    case ActivityType.removeFile: {
      const file = (activity.meta as { fileName?: string } | null)?.fileName;
      const isAdded = activity.type === ActivityType.addFile;

      if (!file) {
        return segmentsOf(
          isAdded
            ? $localize`:Activity timeline entry after the person's name when they attached a file to a task:attached a file`
            : $localize`:Activity timeline entry after the person's name when they removed a file from a task:removed an attachment`
        );
      }

      return segmentsOf(
        isAdded
          ? $localize`:Activity timeline entry after the person's name when they attached a file to a task. FILE is the file name:attached ${FIRST}:FILE:`
          : $localize`:Activity timeline entry after the person's name when they removed a file from a task. FILE is the file name:removed the attachment ${FIRST}:FILE:`,
        [file]
      );
    }
    case ActivityType.addComment:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they commented on a task:commented`
      );
    case ActivityType.modifyComment:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they edited a comment:edited a comment`
      );
    case ActivityType.removeComment:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they deleted a comment:deleted a comment`
      );
    case ActivityType.enable:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they switched something on:enabled`
      );
    case ActivityType.disable:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name when they switched something off:disabled`
      );
    case ActivityType.pullRequestOpened:
    case ActivityType.pullRequestMerged:
    case ActivityType.pullRequestClosed:
      return pullRequestSummaryOf(activity);
    case ActivityType.commitsPushed:
      return commitsSummaryOf(activity);
    case ActivityType.checksPassed:
    case ActivityType.checksFailed:
      return checksSummaryOf(activity);
    case ActivityType.runReview:
      return reviewSummaryOf(activity);
    case ActivityType.acceptReviewFinding:
    case ActivityType.dismissReviewFinding:
    case ActivityType.reopenReviewFinding:
      return findingSummaryOf(activity);
    default:
      return segmentsOf(
        $localize`:Activity timeline entry after the person's name for a change the timeline has no wording for:made a change`
      );
  }
};

const editCountOf = (activity: ActivityViewModel): string | null => {
  const count = activity.revisionCount;

  if (count <= 1) return null;

  return $localize`:Follows an activity timeline entry that folds several edits together. COUNT is a number above one:· ${count}:COUNT: edits`;
};

const phraseOf = (activity: ActivityViewModel): Phrase => {
  return {
    summary: mergedSummaryOf(activity) ?? summaryOf(activity),
    note: editCountOf(activity),
  };
};

export const activityCategory = (
  activity: ActivityViewModel
): FilterCategory | null => {
  return presentationOf(activity).category;
};

export const activityCategoryCounts = (
  activities: ActivityViewModel[]
): Record<ActivityTimelineCategory, number> => {
  const counts: Record<ActivityTimelineCategory, number> = {
    all: activities.length,
    status: 0,
    edits: 0,
    tags: 0,
    people: 0,
    deleted: 0,
    assistant: 0,
    code: 0,
  };

  for (const activity of activities) {
    const category = activityCategory(activity);

    if (category) counts[category]++;
  }

  return counts;
};

export const activityCategoryOptions = (
  counts: Record<ActivityTimelineCategory, number>,
  selected: ActivityTimelineCategory
): SegmentedOption<ActivityTimelineCategory>[] => {
  const options: SegmentedOption<ActivityTimelineCategory>[] = [
    {
      value: 'all',
      label: $localize`:Activity timeline filter that shows every entry:All`,
    },
    {
      value: 'status',
      label: $localize`:Activity timeline filter for tasks moving between groups or statuses:Status`,
    },
    {
      value: 'edits',
      label: $localize`:Activity timeline filter for changes to task fields, comments, files and links:Edits`,
    },
    {
      value: 'tags',
      label: $localize`:Activity timeline filter for tags being added or removed:Tags`,
    },
    {
      value: 'people',
      label: $localize`:Activity timeline filter for people being assigned or unassigned:Assignees`,
    },
    {
      value: 'deleted',
      label: $localize`:Activity timeline filter for deleted or removed tasks:Deleted`,
    },
    {
      value: 'assistant',
      label: $localize`:Activity timeline filter for assistant reviews and their findings:Assistant`,
    },
    {
      value: 'code',
      label: $localize`:Activity timeline filter for pull requests, commits and checks:Code`,
    },
  ];

  return options
    .filter((option) => {
      const isHiddenCode =
        option.value === 'code' && counts.code === 0 && selected !== 'code';

      return !isHiddenCode;
    })
    .map((option) => ({ ...option, count: counts[option.value] }));
};

interface Run {
  key: string;
  dayKey: string;
  items: ActivityViewModel[];
  phrase: Phrase;
}

const runKey = (
  activity: ActivityViewModel,
  dayKey: string,
  phrase: Phrase
): string => {
  const actor = activity.userId ?? activityActor(activity).name;
  const summary = phrase.summary.map((segment) => segment.text).join('');

  return [
    actor,
    activity.agent ?? '',
    activity.type,
    activity.entityType,
    activity.entityId,
    dayKey,
    summary,
    phrase.note ?? '',
  ].join('|');
};

const toRuns = (activities: ActivityViewModel[]): Run[] => {
  const runs: Run[] = [];

  for (const activity of activities) {
    const dayKey = localDayKey(activity.time);
    const phrase = phraseOf(activity);
    const key = runKey(activity, dayKey, phrase);
    const previous = runs.at(-1);

    if (previous?.key === key) {
      previous.items.push(activity);
    } else {
      runs.push({ key, dayKey, items: [activity], phrase });
    }
  }

  return runs;
};

const subjectOf = (
  activity: ActivityViewModel,
  context: ActivityTimelineContext
): Pick<
  EventTimelineRow,
  'subjectId' | 'subjectLink' | 'subjectLabel' | 'subjectStruck'
> => {
  const { scope } = context;

  if (scope.kind === 'task') return {};

  const isDeleted = activity.type === ActivityType.delete;
  const taskSystemId = activity.taskSystemId;
  const canLink = !!taskSystemId && !isDeleted && !!context.workspaceSlug;

  return {
    subjectId: taskSystemId ?? null,
    subjectLink: canLink
      ? ['/', context.workspaceSlug, 'tasks', taskSystemId]
      : null,
    subjectLabel: taskSystemId ? (activity.taskName ?? '') : scope.boardName,
    subjectStruck: isDeleted,
  };
};

const toRow = (
  run: Run,
  context: ActivityTimelineContext
): EventTimelineRow => {
  const [activity] = run.items;
  const oldest = run.items[run.items.length - 1];
  const actor = activityActor(activity);
  const presentation = presentationOf(activity);
  const count = run.items.length;

  return {
    key: `${activity.id}`,
    actor: {
      name:
        actor.name ||
        $localize`:Stands in for the person behind an activity entry when they are unknown:Someone`,
      pictureUrl: actor.pictureUrl,
      isServiceAccount: actor.isServiceAccount,
    },
    actorNote: activity.agent
      ? $localize`:Follows the person in an activity entry and names the assistant that acted for them. AGENT is its name:via ${activity.agent}:AGENT:`
      : null,
    icon: presentation.icon,
    tone: presentation.tone,
    summary: run.phrase.summary,
    note: run.phrase.note,
    ...subjectOf(activity, context),
    time: compactTime(activity.time, context.now),
    timeTitle: timeRange(oldest.firstTime, activity.time),
    repeatLabel:
      count > 1
        ? $localize`:Toggle that lists the times an action repeated, in the activity timeline. COUNT is a number above one:${count}:COUNT: times`
        : null,
    repeatTimes: run.items.map((item) => compactTime(item.time, context.now)),
  };
};

export const activityTimelineGroups = (
  activities: ActivityViewModel[],
  context: ActivityTimelineContext
): EventTimelineGroup[] => {
  const groups: EventTimelineGroup[] = [];

  for (const run of toRuns(activities)) {
    let group = groups.at(-1);

    if (group?.key !== run.dayKey) {
      group = {
        key: run.dayKey,
        label: dayHeading(run.items[0].time, context.now),
        rows: [],
      };
      groups.push(group);
    }

    group.rows.push(toRow(run, context));
  }

  return groups;
};
