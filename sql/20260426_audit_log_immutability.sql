-- Enforce PostgreSQL-level immutability on activity_logs.
-- The application already uses AuditLogImmutabilityInterceptor; these rules add a
-- second layer of protection that cannot be bypassed by direct DB connections.

CREATE OR REPLACE FUNCTION raise_audit_log_mutation()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'activity_logs records are immutable and cannot be updated or deleted.';
END;
$$;

CREATE OR REPLACE TRIGGER trg_activity_logs_no_update
    BEFORE UPDATE ON activity_logs
    FOR EACH ROW EXECUTE FUNCTION raise_audit_log_mutation();

CREATE OR REPLACE TRIGGER trg_activity_logs_no_delete
    BEFORE DELETE ON activity_logs
    FOR EACH ROW EXECUTE FUNCTION raise_audit_log_mutation();
