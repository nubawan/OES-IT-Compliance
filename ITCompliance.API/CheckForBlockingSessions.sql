-- READ-ONLY. Checks for open transactions / blocking sessions that
-- could explain queries against RoleAssignments timing out.

-- Any session with an open (uncommitted) transaction right now?
SELECT
    s.session_id,
    s.login_name,
    s.host_name,
    s.program_name,
    s.status,
    t.transaction_id,
    at.name AS transaction_name,
    at.transaction_begin_time,
    DATEDIFF(MINUTE, at.transaction_begin_time, GETDATE()) AS open_minutes
FROM sys.dm_tran_session_transactions t
JOIN sys.dm_tran_active_transactions at ON t.transaction_id = at.transaction_id
JOIN sys.dm_exec_sessions s ON t.session_id = s.session_id
ORDER BY at.transaction_begin_time;

-- Anything actively blocking another request right now?
SELECT
    blocking_session_id,
    session_id AS blocked_session_id,
    wait_type,
    wait_time,
    status,
    command
FROM sys.dm_exec_requests
WHERE blocking_session_id <> 0;

-- Locks specifically held on RoleAssignments.
-- resource_associated_entity_id is only a real object_id when
-- resource_type = 'OBJECT' - for row/page/key locks it's a HOBT id,
-- which OBJECT_NAME() can't take directly (overflows int), so
-- resolve those via sys.partitions instead.
SELECT
    l.request_session_id,
    l.resource_type,
    l.request_mode,
    l.request_status,
    s.login_name,
    s.program_name,
    COALESCE(
        OBJECT_NAME(NULLIF(p.object_id, 0)),
        CASE WHEN l.resource_type = 'OBJECT'
             THEN OBJECT_NAME(l.resource_associated_entity_id)
        END
    ) AS locked_object
FROM sys.dm_tran_locks l
JOIN sys.dm_exec_sessions s ON l.request_session_id = s.session_id
LEFT JOIN sys.partitions p ON l.resource_associated_entity_id = p.hobt_id
WHERE l.resource_database_id = DB_ID();
