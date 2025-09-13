import { EntityType } from '@core/models/entity-type';
import { Pipe, PipeTransform } from '@angular/core';
import { entityTypeToString } from '@core/transforms/entity-type';

@Pipe({
    name: 'entityType',
    pure: true
})
export class EntityTypePipe implements PipeTransform {
  transform(value: EntityType): string {
    return entityTypeToString(value);
  }
}
