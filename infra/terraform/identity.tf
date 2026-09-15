resource "azurerm_role_assignment" "api_blob_data_contributor" {
  count = var.deploy_application_container_apps ? 1 : 0

  scope                = azurerm_storage_account.documents.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app.api[0].identity[0].principal_id
}

resource "azurerm_role_assignment" "api_service_bus_sender" {
  count = var.deploy_application_container_apps ? 1 : 0

  scope                = azurerm_servicebus_namespace.main.id
  role_definition_name = "Azure Service Bus Data Sender"
  principal_id         = azurerm_container_app.api[0].identity[0].principal_id
}

resource "azurerm_role_assignment" "worker_blob_data_contributor" {
  count = var.deploy_application_container_apps ? 1 : 0

  scope                = azurerm_storage_account.documents.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app.worker[0].identity[0].principal_id
}

resource "azurerm_role_assignment" "worker_service_bus_receiver" {
  count = var.deploy_application_container_apps ? 1 : 0

  scope                = azurerm_servicebus_namespace.main.id
  role_definition_name = "Azure Service Bus Data Receiver"
  principal_id         = azurerm_container_app.worker[0].identity[0].principal_id
}

resource "azurerm_role_assignment" "worker_document_intelligence_user" {
  count = var.deploy_application_container_apps ? 1 : 0

  scope                = azurerm_cognitive_account.document_intelligence.id
  role_definition_name = "Cognitive Services User"
  principal_id         = azurerm_container_app.worker[0].identity[0].principal_id
}

resource "azurerm_role_assignment" "api_key_vault_secrets_user" {
  count = var.deploy_application_container_apps ? 1 : 0

  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_container_app.api[0].identity[0].principal_id
}

resource "azurerm_role_assignment" "worker_key_vault_secrets_user" {
  count = var.deploy_application_container_apps ? 1 : 0

  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_container_app.worker[0].identity[0].principal_id
}