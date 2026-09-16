resource "azurerm_virtual_network" "main" {
  name                = "vnet-${local.name_prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  address_space = [
    var.virtual_network_address_space
  ]

  tags = local.common_tags
}

resource "azurerm_subnet" "container_apps" {
  name                 = "snet-container-apps"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name

  address_prefixes = [
    var.container_apps_subnet_address_prefix
  ]

  delegation {
    name = "container-apps"

    service_delegation {
      name = "Microsoft.App/environments"

      actions = [
        "Microsoft.Network/virtualNetworks/subnets/join/action"
      ]
    }
  }
}

resource "azurerm_subnet" "private_endpoints" {
  name                 = "snet-private-endpoints"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name

  address_prefixes = [
    var.private_endpoints_subnet_address_prefix
  ]

  private_endpoint_network_policies = "Disabled"
}

resource "azurerm_private_dns_zone" "blob" {
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = azurerm_resource_group.main.name

  tags = local.common_tags
}

resource "azurerm_private_dns_zone" "service_bus" {
  name                = "privatelink.servicebus.windows.net"
  resource_group_name = azurerm_resource_group.main.name

  tags = local.common_tags
}

resource "azurerm_private_dns_zone" "key_vault" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = azurerm_resource_group.main.name

  tags = local.common_tags
}

resource "azurerm_private_dns_zone" "document_intelligence" {
  name                = "privatelink.cognitiveservices.azure.com"
  resource_group_name = azurerm_resource_group.main.name

  tags = local.common_tags
}

resource "azurerm_private_dns_zone" "postgresql" {
  name                = "privatelink.postgres.database.azure.com"
  resource_group_name = azurerm_resource_group.main.name

  tags = local.common_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "blob" {
  name                = "link-${local.name_prefix}-blob"
  private_dns_zone_id = azurerm_private_dns_zone.blob.id
  virtual_network_id  = azurerm_virtual_network.main.id

  registration_enabled = false

  tags = local.common_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "service_bus" {
  name                = "link-${local.name_prefix}-servicebus"
  private_dns_zone_id = azurerm_private_dns_zone.service_bus.id
  virtual_network_id  = azurerm_virtual_network.main.id

  registration_enabled = false

  tags = local.common_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "key_vault" {
  name                = "link-${local.name_prefix}-keyvault"
  private_dns_zone_id = azurerm_private_dns_zone.key_vault.id
  virtual_network_id  = azurerm_virtual_network.main.id

  registration_enabled = false

  tags = local.common_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "document_intelligence" {
  name                = "link-${local.name_prefix}-documentintelligence"
  private_dns_zone_id = azurerm_private_dns_zone.document_intelligence.id
  virtual_network_id  = azurerm_virtual_network.main.id

  registration_enabled = false

  tags = local.common_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "postgresql" {
  name                = "link-${local.name_prefix}-postgresql"
  private_dns_zone_id = azurerm_private_dns_zone.postgresql.id
  virtual_network_id  = azurerm_virtual_network.main.id

  registration_enabled = false

  tags = local.common_tags
}