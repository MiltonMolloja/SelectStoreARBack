UPDATE product_pending_changes
SET "Status" = 'Approved',
    reviewed_at = NOW(),
    reviewed_by = 'admin-direct'
WHERE "Status" = 'Pending';
