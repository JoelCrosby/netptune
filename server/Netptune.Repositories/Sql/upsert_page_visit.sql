INSERT INTO page_visits (page_id, user_id, visited_at)
VALUES (@PageId, @UserId, NOW())
ON CONFLICT (page_id, user_id)
DO UPDATE SET visited_at = EXCLUDED.visited_at;
