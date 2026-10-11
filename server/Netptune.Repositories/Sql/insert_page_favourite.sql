INSERT INTO page_favourites (page_id, user_id, sort_order)
VALUES (@PageId, @UserId, 0)
ON CONFLICT (page_id, user_id) DO NOTHING;
