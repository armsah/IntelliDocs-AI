data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "main" {
  name = substr(
    "kv-${local.name_prefix}-${random_string.resource_suffix.result}",
    0,
    24
  )

  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tenant_id           = data.azurerm_client_config.current.tenant_id

  sku_name = "standard"

  rbac_authorization_enabled    = true
  purge_protection_enabled      = true
  soft_delete_retention_days    = 7
  public_network_access_enabled = false

  tags = local.common_tags
}