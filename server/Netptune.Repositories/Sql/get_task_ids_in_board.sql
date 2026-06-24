-- Ids of all non-deleted tasks on a board for TaskRepository.GetTaskIdsInBoard.
SELECT pt.id
FROM boards b

INNER JOIN board_groups bg ON b.id = bg.board_id AND NOT bg.is_deleted
INNER JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
INNER JOIN project_tasks pt on pt.id = ptibg.project_task_id AND NOT pt.is_deleted

WHERE b.identifier = @boardIdentifier
