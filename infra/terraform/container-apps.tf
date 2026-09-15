resource "azurerm_container_app_environment" "main" {
  name                = "cae-${local.name_prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

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

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "review-portal"
      image  = var.review_portal_container_image
      cpu    = 0.5
      memory = "1Gi"

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
