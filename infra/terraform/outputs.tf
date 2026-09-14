output "resource_group_name" {
  description = "Azure Resource Group containing IntelliDocs resources."
  value       = azurerm_resource_group.main.name
}

output "location" {
  description = "Azure region used for the environment."
  value       = azurerm_resource_group.main.location
}

output "storage_account_name" {
  description = "Blob Storage account name."
  value       = azurerm_storage_account.documents.name
}

output "documents_container_name" {
  description = "Blob container used for original documents."
  value       = azurerm_storage_container.documents.name
}

output "classifier_training_container_name" {
  description = "Private Blob container used for Document Intelligence classifier training data."
  value       = azurerm_storage_container.classifier_training.name
}

output "postgresql_server_name" {
  description = "Azure Database for PostgreSQL Flexible Server name."
  value       = azurerm_postgresql_flexible_server.main.name
}

output "postgresql_fqdn" {
  description = "PostgreSQL Flexible Server FQDN."
  value       = azurerm_postgresql_flexible_server.main.fqdn
}

output "postgresql_database_name" {
  description = "Application PostgreSQL database."
  value       = azurerm_postgresql_flexible_server_database.intellidocs.name
}

output "log_analytics_workspace_name" {
  description = "Log Analytics workspace."
  value       = azurerm_log_analytics_workspace.main.name
}

output "container_app_environment_name" {
  description = "Azure Container Apps environment."
  value       = azurerm_container_app_environment.main.name
}

output "postgresql_location" {
  description = "Azure region hosting PostgreSQL Flexible Server."
  value       = azurerm_postgresql_flexible_server.main.location
}
output "document_intelligence_name" {
  description = "Azure AI Document Intelligence account name."
  value       = azurerm_cognitive_account.document_intelligence.name
}

output "document_intelligence_endpoint" {
  description = "Azure AI Document Intelligence endpoint."
  value       = azurerm_cognitive_account.document_intelligence.endpoint
}

output "service_bus_namespace_name" {
  description = "Azure Service Bus namespace name."
  value       = azurerm_servicebus_namespace.main.name
}

output "service_bus_fully_qualified_namespace" {
  description = "Azure Service Bus fully qualified namespace."
  value       = "${azurerm_servicebus_namespace.main.name}.servicebus.windows.net"
}

output "service_bus_document_processing_queue_name" {
  description = "Document processing Service Bus queue name."
  value       = azurerm_servicebus_queue.document_processing.name
}
