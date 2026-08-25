import { Service, computed } from '@angular/core';
import { EstimateType, estimateTypeLabels } from '@core/enums/estimate-type';
import { TaskPriority, taskPriorityLabels } from '@core/enums/task-priority';
import { StatusCategory, statusCategoryLabels } from '@core/models/status';
import { projectResource } from '@core/resources/project.resource';
import { relationTypeResource } from '@core/resources/relation-type.resource';
import { workspaceBoardsResource } from '@core/resources/board.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { statusResource } from '@core/resources/status.resource';
import { tagResource } from '@core/resources/tag.resource';
import { workspaceUsersResource } from '@core/resources/user.resource';
import { SprintStatus } from '@core/enums/sprint-status';
import { QueryBuilderCatalog } from '@shared/components/query-builder/query-builder.models';
import { explainQueryGroup } from '@shared/components/query-builder/query-explanation';
import { emptyTaskQueryMessage } from '../models/task-query-copy';
import { toBuilderCatalog, toBuilderGroup } from '../models/task-query-builder';
import {
  TaskQueryCatalog,
  TaskQueryField,
  TaskQueryGroup,
  TaskQueryOptionSource,
} from '../models/task-view.models';

export interface QueryFieldOption {
  value: string;
  label: string;
}

@Service()
export class QueryFieldOptionsService {
  private readonly statuses = statusResource().value;
  private readonly tags = tagResource().value;
  private readonly projects = projectResource().value;
  private readonly relationTypes = relationTypeResource().value;
  private readonly boards = workspaceBoardsResource().value;
  private readonly members = workspaceUsersResource();
  private readonly sprints = sprintResource([
    SprintStatus.planning,
    SprintStatus.active,
    SprintStatus.completed,
  ]).value;

  private readonly optionsBySource = computed(() => {
    const sources = new Map<TaskQueryOptionSource, QueryFieldOption[]>();

    sources.set(
      'statuses',
      this.statuses().map((status) => ({
        value: String(status.id),
        label: status.name,
      }))
    );

    sources.set(
      'status-categories',
      enumOptions(StatusCategory, statusCategoryLabels)
    );

    sources.set('priorities', enumOptions(TaskPriority, taskPriorityLabels));
    sources.set(
      'estimate-types',
      enumOptions(EstimateType, estimateTypeLabels)
    );

    sources.set(
      'projects',
      this.projects().map((project) => ({
        value: String(project.id),
        label: project.name,
      }))
    );

    sources.set(
      'sprints',
      this.sprints().map((sprint) => ({
        value: String(sprint.id),
        label: sprint.name,
      }))
    );

    sources.set(
      'members',
      this.members().map((member) => ({
        value: member.id,
        label: member.displayName,
      }))
    );

    sources.set(
      'tags',
      this.tags().map((tag) => ({ value: tag.name, label: tag.name }))
    );

    sources.set(
      'boards',
      this.boards().flatMap((project) => {
        return project.boards.map((board) => ({
          value: String(board.id),
          label: board.name,
        }));
      })
    );

    sources.set('relation-types', this.relationTypeOptions());

    return sources;
  });

  optionsFor(field: TaskQueryField | undefined): QueryFieldOption[] {
    if (!field?.optionSource) return [];

    return this.optionsBySource().get(field.optionSource) ?? [];
  }

  labelFor(field: TaskQueryField | undefined, value: string): string {
    const options = this.optionsFor(field);
    const match = options.find((option) => option.value === value);

    return match?.label ?? value;
  }

  // The shared query builder edits and explains a query through a catalog that already carries its
  // own option lists, so resolving those lists is this service's half of the bargain.
  builderCatalog(catalog: TaskQueryCatalog): QueryBuilderCatalog {
    return toBuilderCatalog(catalog, (field) => this.optionsFor(field));
  }

  explain(group: TaskQueryGroup, catalog: TaskQueryCatalog): string {
    const summary = explainQueryGroup(
      toBuilderGroup(group),
      this.builderCatalog(catalog)
    );

    return summary || emptyTaskQueryMessage;
  }

  // Each relation type yields two options, because "blocks" and "blocked by" are the same
  // type read from opposite ends and a user filtering tasks means one or the other.
  private relationTypeOptions(): QueryFieldOption[] {
    return this.relationTypes().flatMap((relationType) => {
      const forward = {
        value: `${relationType.id}:source`,
        label: relationType.name,
      };

      if (!relationType.inverseName) return [forward];

      return [
        forward,
        {
          value: `${relationType.id}:target`,
          label: relationType.inverseName,
        },
      ];
    });
  }
}

function enumOptions(
  source: Record<string, string | number>,
  labels: Record<number, string>
): QueryFieldOption[] {
  return Object.values(source)
    .filter((value): value is number => typeof value === 'number')
    .map((value) => ({ value: String(value), label: labels[value] }));
}
