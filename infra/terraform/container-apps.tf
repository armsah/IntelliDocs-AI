resource "azurerm_container_app_environment" "main" {
  name                = "cae-${local.name_prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  infrastructure_subnet_id = azurerm_subnet.container_apps.id

  logs_destination           = "log-analytics"
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id

  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
    minimum_count         = 0
    maximum_count         = 0
  }

  tags = local.common_tags
}

resource "azurerm_container_app" "api" {
  count = var.deploy_application_container_apps ? 1 : 0

  name                         = "ca-${local.name_prefix}-api"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  secret {
    name                = "postgresql-connection-string"
    identity            = "System"
    key_vault_secret_id = var.postgresql_connection_string_key_vault_secret_id
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "api"
      image  = var.api_container_image
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }

      env {
        name        = "ConnectionStrings__PostgreSql"
        secret_name = "postgresql-connection-string"
      }

      env {
        name  = "BlobStorage__ServiceUri"
        value = azurerm_storage_account.documents.primary_blob_endpoint
      }

      env {
        name  = "ServiceBus__FullyQualifiedNamespace"
        value = "${azurerm_servicebus_namespace.main.name}.servicebus.windows.net"
      }

      env {
        name  = "ServiceBus__DocumentProcessingQueueName"
        value = azurerm_servicebus_queue.document_processing.name
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  tags = local.common_tags
}

resource "azurerm_container_app" "review_portal" {
  count = var.deploy_application_container_apps ? 1 : 0

  name                         = "ca-${local.name_prefix}-review"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned, UserAssigned"

    identity_ids = [
      azurerm_user_assigned_identity.review_portal_credential.id
    ]
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "review-portal"
      image  = var.review_portal_container_image
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "AzureAd__TenantId"
        value = data.azurerm_client_config.current.tenant_id
      }

      env {
        name  = "AzureAd__ClientId"
        value = azuread_application_registration.review_portal.client_id
      }

      env {
        name  = "AzureAd__ClientCredentials__0__ManagedIdentityClientId"
        value = azurerm_user_assigned_identity.review_portal_credential.client_id
      }

      env {
        name  = "ReviewApi__Scopes__0"
        value = "api://${azuread_application_registration.api.client_id}/Review.Access"
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }

      env {
        name  = "ReviewApi__BaseUrl"
        value = "https://${azurerm_container_app.api[0].ingress[0].fqdn}/"
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  tags = local.common_tags
}

resource "azurerm_container_app" "worker" {
  count = var.deploy_application_container_apps ? 1 : 0

  name                         = "ca-${local.name_prefix}-worker"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  secret {
    name                = "postgresql-connection-string"
    identity            = "System"
    key_vault_secret_id = var.postgresql_connection_string_key_vault_secret_id
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "worker"
      image  = var.worker_container_image
      cpu    = 0.5
      memory = "1Gi"

      env {
        name        = "ConnectionStrings__PostgreSql"
        secret_name = "postgresql-connection-string"
      }

      env {
        name  = "BlobStorage__ServiceUri"
        value = azurerm_storage_account.documents.primary_blob_endpoint
      }

      env {
        name  = "BlobStorage__ContainerName"
        value = azurerm_storage_container.documents.name
      }

      env {
        name  = "ServiceBus__FullyQualifiedNamespace"
        value = "${azurerm_servicebus_namespace.main.name}.servicebus.windows.net"
      }

      env {
        name  = "ServiceBus__DocumentProcessingQueueName"
        value = azurerm_servicebus_queue.document_processing.name
      }

      env {
        name  = "DocumentIntelligence__Endpoint"
        value = azurerm_cognitive_account.document_intelligence.endpoint
      }

      env {
        name  = "DocumentIntelligence__ClassifierId"
        value = "intellidocs-p6-classifier-v1"
      }
    }
  }

  tags = local.common_tags
}