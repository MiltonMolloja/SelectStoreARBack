UPDATE product_pending_changes
SET "Status" = 'Pending',
    reviewed_at = NULL,
    reviewed_by = NULL
WHERE change_type = 'Created'
  AND "ProductId" IS NULL;
