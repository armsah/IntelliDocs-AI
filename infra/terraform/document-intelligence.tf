resource "azurerm_cognitive_account" "document_intelligence" {
  name = "di-${local.name_prefix}-${random_string.resource_suffix.result}"

  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  kind     = "FormRecognizer"
  sku_name = "S0"

  public_network_access_enabled = true
  local_auth_enabled            = true

  tags = local.common_tags
}
