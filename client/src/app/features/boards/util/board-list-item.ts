import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { brandingImageUrl } from '@core/util/branding';
import { LucideChartColumnBig } from '@lucide/angular';
import { EntityListItem } from '@static/components/entity-list/entity-list.types';

export function toBoardListItem(
  board: BoardViewModel,
  workspaceSlug: string | undefined
): EntityListItem {
  return {
    id: board.id,
    name: board.name,
    identifier: board.identifier,
    icon: LucideChartColumnBig,
    color: board.metaInfo.color,
    imageUrl: brandingImageUrl(workspaceSlug, board.metaInfo?.logoFileId),
    people: board.assignees,
    count: board.taskCount,
    updatedAt: board.lastUpdated,
    link: ['.', board.identifier],
  };
}
