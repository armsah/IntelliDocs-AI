resource "azurerm_servicebus_namespace" "main" {
  name = "sb-${local.name_prefix}-${random_string.resource_suffix.result}"

  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  sku = "Premium"

  capacity = 1

  public_network_access_enabled = false

  tags = local.common_tags
}

resource "azurerm_servicebus_queue" "document_processing" {
  name = "document-processing"

  namespace_id = azurerm_servicebus_namespace.main.id

  lock_duration = "PT1M"

  max_delivery_count = 5

  requires_duplicate_detection = true

  duplicate_detection_history_time_window = "PT10M"

  dead_lettering_on_message_expiration = true

  default_message_ttl = "P1D"

  max_size_in_megabytes = 1024
}