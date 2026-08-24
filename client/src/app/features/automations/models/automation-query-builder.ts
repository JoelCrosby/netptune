import { Status } from '@core/models/status';
import {
  LucideAsterisk,
  LucideCircleDashed,
  LucideCircleDot,
  LucideEqual,
  LucideEqualNot,
  LucideMinus,
  LucidePlus,
  LucideTextSearch,
} from '@lucide/angular';
import {
  QueryBuilderCatalog,
  QueryBuilderCondition,
  QueryBuilderField,
  QueryBuilderGroup,
  QueryBuilderGroupOperator,
  QueryBuilderOperator,
  QueryBuilderOption,
} from '@shared/components/query-builder/query-builder.models';
import {
  conditionOperatorLabels,
  taskChangeFieldLabels,
} from './automation-copy';
import {
  AutomationConditionGroup,
  AutomationConditionGroupOperator,
  AutomationConditionOperator,
  AutomationFieldCondition,
  TaskChangeField,
} from './automation.models';

const conditionFields: TaskChangeField[] = [
  TaskChangeField.name,
  TaskChangeField.description,
  TaskChangeField.status,
  TaskChangeField.assignees,
  TaskChangeField.owner,
  TaskChangeField.priority,
  TaskChangeField.estimate,
  TaskChangeField.dueDate,
  TaskChangeField.tags,
  TaskChangeField.startDate,
];

const fieldKeys: Record<TaskChangeField, string> = {
  [TaskChangeField.name]: 'name',
  [TaskChangeField.description]: 'description',
  [TaskChangeField.status]: 'status',
  [TaskChangeField.assignees]: 'assignees',
  [TaskChangeField.owner]: 'owner',
  [TaskChangeField.priority]: 'priority',
  [TaskChangeField.estimate]: 'estimate',
  [TaskChangeField.dueDate]: 'dueDate',
  [TaskChangeField.tags]: 'tags',
  [TaskChangeField.startDate]: 'startDate',
  [TaskChangeField.sprint]: 'sprint',
  [TaskChangeField.boardGroup]: 'boardGroup',
};

const fieldsByKey = new Map<string, TaskChangeField>(
  Object.entries(fieldKeys).map(([field, key]) => {
    return [key, Number(field) as TaskChangeField];
  })
);

const operatorKeys: Record<AutomationConditionOperator, string> = {
  [AutomationConditionOperator.any]: 'any',
  [AutomationConditionOperator.equals]: 'equals',
  [AutomationConditionOperator.notEquals]: 'notEquals',
  [AutomationConditionOperator.contains]: 'contains',
  [AutomationConditionOperator.isEmpty]: 'isEmpty',
  [AutomationConditionOperator.isNotEmpty]: 'isNotEmpty',
  [AutomationConditionOperator.added]: 'added',
  [AutomationConditionOperator.removed]: 'removed',
};

const operatorsByKey = new Map<string, AutomationConditionOperator>(
  Object.entries(operatorKeys).map(([operator, key]) => {
    return [key, Number(operator) as AutomationConditionOperator];
  })
);

const collectionFields: TaskChangeField[] = [
  TaskChangeField.assignees,
  TaskChangeField.tags,
];

const textFields: TaskChangeField[] = [
  TaskChangeField.name,
  TaskChangeField.description,
];

const dateFields: TaskChangeField[] = [
  TaskChangeField.startDate,
  TaskChangeField.dueDate,
];

// A rule that fires on anything other than a task change has no "before" to compare against, so
// the operators that describe a change are withheld until the trigger can supply one.
const changeOperatorKeys = new Set(
  [
    AutomationConditionOperator.any,
    AutomationConditionOperator.added,
    AutomationConditionOperator.removed,
  ].map((operator) => operatorKeys[operator])
);

// Operators are labelled with the same words the rule preview uses, so a condition reads the same
// wherever it is shown: picked from the dropdown, summarised under the builder, or narrated on the
// rule itself.
const anyChange: QueryBuilderOperator = {
  key: operatorKeys[AutomationConditionOperator.any],
  label: conditionOperatorLabels[AutomationConditionOperator.any],
  arity: 0,
  icon: LucideAsterisk,
};

const isEmpty: QueryBuilderOperator = {
  key: operatorKeys[AutomationConditionOperator.isEmpty],
  label: conditionOperatorLabels[AutomationConditionOperator.isEmpty],
  arity: 0,
  icon: LucideCircleDashed,
};

const isNotEmpty: QueryBuilderOperator = {
  key: operatorKeys[AutomationConditionOperator.isNotEmpty],
  label: conditionOperatorLabels[AutomationConditionOperator.isNotEmpty],
  arity: 0,
  icon: LucideCircleDot,
};

export function automationConditionCatalog(
  statuses: Status[],
  supportsChangeOperators: boolean
): QueryBuilderCatalog {
  return {
    fields: conditionFields.map((field) => {
      return toBuilderField(field, statuses, supportsChangeOperators);
    }),
    maximumDepth: 4,
  };
}

export function toBuilderGroup(
  group: AutomationConditionGroup
): QueryBuilderGroup {
  return {
    operator: group.operator as number as QueryBuilderGroupOperator,
    conditions: group.conditions.map((condition) => ({
      field: fieldKeys[condition.field],
      operator: operatorKeys[condition.operator],
      values: condition.value == null ? [] : [condition.value],
    })),
    groups: group.groups.map(toBuilderGroup),
  };
}

export function fromBuilderGroup(
  group: QueryBuilderGroup
): AutomationConditionGroup {
  return {
    operator: group.operator as number as AutomationConditionGroupOperator,
    conditions: group.conditions.map(fromBuilderCondition),
    groups: group.groups.map(fromBuilderGroup),
  };
}

function fromBuilderCondition(
  condition: QueryBuilderCondition
): AutomationFieldCondition {
  return {
    field: fieldsByKey.get(condition.field) ?? TaskChangeField.status,
    operator:
      operatorsByKey.get(condition.operator) ??
      AutomationConditionOperator.equals,
    value: condition.values[0] ?? null,
  };
}

function toBuilderField(
  field: TaskChangeField,
  statuses: Status[],
  supportsChangeOperators: boolean
): QueryBuilderField {
  return {
    key: fieldKeys[field],
    name: taskChangeFieldLabels[field],
    inputType: dateFields.includes(field) ? 'date' : 'text',
    operators: operatorsFor(field).filter((operator) => {
      return supportsChangeOperators || !changeOperatorKeys.has(operator.key);
    }),
    options: optionsFor(field, statuses),
    valuePlaceholder: placeholderFor(field),
  };
}

function operatorsFor(field: TaskChangeField): QueryBuilderOperator[] {
  // A collection holds many values at once, so its operators read as membership rather than
  // equality, and it is the only place where a single value can be added or taken away.
  if (collectionFields.includes(field)) {
    return [
      anyChange,
      {
        key: operatorKeys[AutomationConditionOperator.equals],
        label: $localize`:Condition operator matching when a value is present:includes`,
        arity: 1,
        icon: LucideEqual,
      },
      {
        key: operatorKeys[AutomationConditionOperator.notEquals],
        label: $localize`:Condition operator matching when a value is absent:does not include`,
        arity: 1,
        icon: LucideEqualNot,
      },
      {
        key: operatorKeys[AutomationConditionOperator.contains],
        label: $localize`:Condition operator matching a substring:contains text`,
        arity: 1,
        icon: LucideTextSearch,
      },
      isEmpty,
      isNotEmpty,
      {
        key: operatorKeys[AutomationConditionOperator.added],
        label: conditionOperatorLabels[AutomationConditionOperator.added],
        arity: 1,
        icon: LucidePlus,
      },
      {
        key: operatorKeys[AutomationConditionOperator.removed],
        label: conditionOperatorLabels[AutomationConditionOperator.removed],
        arity: 1,
        icon: LucideMinus,
      },
    ];
  }

  const operators: QueryBuilderOperator[] = [
    anyChange,
    {
      key: operatorKeys[AutomationConditionOperator.equals],
      label: conditionOperatorLabels[AutomationConditionOperator.equals],
      arity: 1,
      icon: LucideEqual,
    },
    {
      key: operatorKeys[AutomationConditionOperator.notEquals],
      label: conditionOperatorLabels[AutomationConditionOperator.notEquals],
      arity: 1,
      icon: LucideEqualNot,
    },
  ];

  if (textFields.includes(field)) {
    operators.push({
      key: operatorKeys[AutomationConditionOperator.contains],
      label: conditionOperatorLabels[AutomationConditionOperator.contains],
      arity: 1,
      icon: LucideTextSearch,
    });
  }

  operators.push(isEmpty, isNotEmpty);

  return operators;
}

function optionsFor(
  field: TaskChangeField,
  statuses: Status[]
): QueryBuilderOption[] {
  if (field === TaskChangeField.status) {
    return statuses.map((status) => ({
      value: status.id.toString(),
      label: status.name,
    }));
  }

  // Priority and estimate travel as the server's enum names rather than their numbers, because a
  // condition value is stored and compared as text.
  if (field === TaskChangeField.priority) {
    return [
      { value: 'None', label: $localize`:Task priority level, none:None` },
      { value: 'Low', label: $localize`:Task priority level, low:Low` },
      {
        value: 'Medium',
        label: $localize`:Task priority level, medium:Medium`,
      },
      { value: 'High', label: $localize`:Task priority level, high:High` },
      {
        value: 'Critical',
        label: $localize`:Task priority level, critical:Critical`,
      },
    ];
  }

  if (field === TaskChangeField.estimate) {
    return [
      {
        value: 'StoryPoints',
        label: $localize`:Estimation unit, story points:Story points`,
      },
      { value: 'Hours', label: $localize`:Estimation unit, hours:Hours` },
      {
        value: 'TShirt',
        label: $localize`:Estimation unit, t-shirt sizes:T-shirt`,
      },
    ];
  }

  return [];
}

function placeholderFor(field: TaskChangeField): string {
  if (field === TaskChangeField.tags) {
    return $localize`:Placeholder for the value of a tag condition:Tag name`;
  }

  if (field === TaskChangeField.assignees) {
    return $localize`:Placeholder for the value of an assignee condition:User ID`;
  }

  return '';
}
