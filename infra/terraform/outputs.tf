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

output "api_container_app_fqdn" {
  description = "Public FQDN of the IntelliDocs API Container App when application deployment is enabled."
  value = var.deploy_application_container_apps ? (
    azurerm_container_app.api[0].ingress[0].fqdn
  ) : null
}

output "review_portal_container_app_fqdn" {
  description = "Public FQDN of the IntelliDocs human-review portal when application deployment is enabled."
  value = var.deploy_application_container_apps ? (
    azurerm_container_app.review_portal[0].ingress[0].fqdn
  ) : null
}

output "key_vault_name" {
  description = "Key Vault used for IntelliDocs secret management."
  value       = azurerm_key_vault.main.name
}

output "key_vault_uri" {
  description = "URI of the IntelliDocs Key Vault."
  value       = azurerm_key_vault.main.vault_uri
}

output "worker_container_app_name" {
  description = "Name of the IntelliDocs document-processing Worker Container App when application deployment is enabled."
  value = var.deploy_application_container_apps ? (
    azurerm_container_app.worker[0].name
  ) : null
}

output "entra_tenant_id" {
  description = "Microsoft Entra tenant ID used by IntelliDocs."
  value       = data.azurerm_client_config.current.tenant_id
}

output "api_entra_client_id" {
  description = "Client ID of the IntelliDocs API Entra application registration."
  value       = azuread_application_registration.api.client_id
}

output "review_portal_entra_client_id" {
  description = "Client ID of the IntelliDocs Review Portal Entra application registration."
  value       = azuread_application_registration.review_portal.client_id
}

output "review_access_scope" {
  description = "Delegated OAuth scope exposed by the IntelliDocs API."
  value       = "api://${azuread_application_registration.api.client_id}/Review.Access"
}

output "review_portal_credential_identity_client_id" {
  description = "Client ID of the user-assigned managed identity used as the Review Portal application credential."
  value       = azurerm_user_assigned_identity.review_portal_credential.client_id
}

output "virtual_network_name" {
  description = "Name of the IntelliDocs virtual network."
  value       = azurerm_virtual_network.main.name
}

output "container_apps_subnet_id" {
  description = "Resource ID of the dedicated Container Apps infrastructure subnet."
  value       = azurerm_subnet.container_apps.id
}

output "private_endpoints_subnet_id" {
  description = "Resource ID of the dedicated private-endpoint subnet."
  value       = azurerm_subnet.private_endpoints.id
}

output "blob_private_endpoint_ip" {
  description = "Private IP address assigned to the Blob Storage private endpoint."
  value       = azurerm_private_endpoint.blob.private_service_connection[0].private_ip_address
}

output "service_bus_private_endpoint_ip" {
  description = "Private IP address assigned to the Service Bus private endpoint."
  value       = azurerm_private_endpoint.service_bus.private_service_connection[0].private_ip_address
}

output "key_vault_private_endpoint_ip" {
  description = "Private IP address assigned to the Key Vault private endpoint."
  value       = azurerm_private_endpoint.key_vault.private_service_connection[0].private_ip_address
}

output "document_intelligence_private_endpoint_ip" {
  description = "Private IP address assigned to the Document Intelligence private endpoint."
  value       = azurerm_private_endpoint.document_intelligence.private_service_connection[0].private_ip_address
}

output "postgresql_private_endpoint_ip" {
  description = "Private IP address assigned to the PostgreSQL private endpoint."
  value       = azurerm_private_endpoint.postgresql.private_service_connection[0].private_ip_address
}
