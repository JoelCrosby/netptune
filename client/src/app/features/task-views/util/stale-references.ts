import { QueryFieldOptionsService } from '../services/query-field-options.service';
import {
  TaskQueryCatalog,
  TaskQueryGroup,
  TaskQueryValidationError,
} from '../models/task-view.models';

// The server rejects a stale reference when the view runs, but the detail page has the catalog and
// the option lists already loaded, so it can say which condition is broken without a second request.
export function findStaleReferences(
  group: TaskQueryGroup,
  catalog: TaskQueryCatalog,
  fieldOptions: QueryFieldOptionsService,
  path = 'query'
): TaskQueryValidationError[] {
  const errors: TaskQueryValidationError[] = [];

  group.conditions.forEach((condition, index) => {
    const conditionPath = `${path}.conditions[${index}]`;
    const field = catalog.fields.find(
      (candidate) => candidate.key === condition.field
    );

    if (!field) {
      errors.push({
        path: conditionPath,
        field: condition.field,
        message: $localize`:Shown when a saved view names a field the workspace no longer has:'${condition.field}:fieldKey:' is no longer a task field.`,
      });

      return;
    }

    const options = fieldOptions.optionsFor(field);

    if (!options.length) return;

    const known = new Set(options.map((option) => option.value));
    const missing = condition.values.filter((value) => !known.has(value));

    for (const value of missing) {
      errors.push({
        path: conditionPath,
        field: field.key,
        message: $localize`:Shown when a saved view references an entity that has been deleted:${field.name}:fieldName: no longer has an option for '${value}:value:'.`,
      });
    }
  });

  group.groups.forEach((nested, index) => {
    const nestedPath = `${path}.groups[${index}]`;

    errors.push(
      ...findStaleReferences(nested, catalog, fieldOptions, nestedPath)
    );
  });

  return errors;
}
