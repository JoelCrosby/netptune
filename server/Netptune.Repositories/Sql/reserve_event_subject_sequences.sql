-- Reserves a block of sequence numbers per event subject in one round trip. Each row inserts with its
-- block size, or adds it to the existing head, and returns the last number reserved; the block is the
-- count numbers ending there. Subjects must be distinct, because ON CONFLICT cannot touch a row twice.
INSERT INTO event_stream_heads (workspace_id, subject_type, subject_id, current_sequence)
SELECT reservation.workspace_id, reservation.subject_type, reservation.subject_id, reservation.count
FROM unnest(@workspaceIds::integer[], @subjectTypes::text[], @subjectIds::text[], @counts::bigint[])
    AS reservation(workspace_id, subject_type, subject_id, count)
ON CONFLICT (workspace_id, subject_type, subject_id)
DO UPDATE SET current_sequence = event_stream_heads.current_sequence + EXCLUDED.current_sequence
RETURNING workspace_id, subject_type, subject_id, current_sequence;
