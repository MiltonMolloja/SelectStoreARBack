UPDATE products p
SET base_price_usd = pc.proposed_price_usd,
    availability = pc.proposed_availability,
    inspiration = pc.proposed_inspiration,
    updated_at = NOW()
FROM product_pending_changes pc
WHERE pc."ProductId" = p."Id"
  AND pc.change_type = 'PriceChanged'
  AND pc."Status" = 'Approved'
  AND pc.reviewed_by = 'admin-direct';
