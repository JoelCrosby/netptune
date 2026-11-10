INSERT INTO scm_webhook_deliveries
    (provider, delivery_id, event_name, action, external_account_id, connection_id, body, received_at, attempts)
VALUES
    (@Provider, @DeliveryId, @EventName, @Action, @ExternalAccountId, @ConnectionId, @Body, @ReceivedAt, 0)
ON CONFLICT (provider, delivery_id) DO NOTHING
RETURNING id;
