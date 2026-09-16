resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-${local.name_prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  sku               = "PerGB2018"
  retention_in_days = 30

  tags = local.common_tags
}

resource "azurerm_application_insights" "main" {
  name                = "appi-${local.name_prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  application_type = "web"
  workspace_id     = azurerm_log_analytics_workspace.main.id

  tags = local.common_tags
}

resource "azurerm_application_insights_workbook" "operations" {
  name                = uuidv5("url", "https://intellidocs.ai/${local.name_prefix}/p11-observability")
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  display_name        = "IntelliDocs - Operations, AI Quality and Cost"

  data_json = jsonencode({
    version = "Notebook/1.0"

    items = [
      {
        type = 1
        content = {
          json = "# IntelliDocs observability\nOperations, AI quality, human review, and cost/usage signals."
        }
        name = "overview"
      },
      {
        type = 3
        content = {
          version      = "KqlItem/1.0"
          query        = "requests | summarize Requests=count(), Failed=countif(success == false), P95DurationMs=percentile(duration, 95) by bin(timestamp, 1h) | order by timestamp asc"
          size         = 0
          title        = "API throughput, failures and P95 latency"
          queryType    = 0
          resourceType = "microsoft.insights/components"
        }
        name = "operations"
      },
      {
        type = 3
        content = {
          version      = "KqlItem/1.0"
          query        = "customMetrics | where name startswith 'intellidocs.ai.' or name startswith 'intellidocs.routing.' or name startswith 'intellidocs.review.' | summarize Value=sum(valueSum), Samples=sum(valueCount) by name, bin(timestamp, 1h) | order by timestamp asc"
          size         = 0
          title        = "AI quality and human-review signals"
          queryType    = 0
          resourceType = "microsoft.insights/components"
        }
        name = "quality"
      },
      {
        type = 3
        content = {
          version      = "KqlItem/1.0"
          query        = "customMetrics | where name == 'intellidocs.ai.document_intelligence.operations' or name == 'intellidocs.documents.processed' | summarize Value=sum(valueSum) by name, bin(timestamp, 1d) | order by timestamp asc"
          size         = 0
          title        = "Processing and Document Intelligence usage"
          queryType    = 0
          resourceType = "microsoft.insights/components"
        }
        name = "usage"
      }
    ]
  })

  tags = local.common_tags
}