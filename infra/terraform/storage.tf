resource "azurerm_storage_account" "documents" {
  name = substr(
    replace(
      "st${var.project_name}${var.environment}${random_string.resource_suffix.result}",
      "-",
      ""
    ),
    0,
    24
  )

  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false

  tags                  = local.common_tags
  public_network_access = "Disabled"

}

resource "azurerm_storage_container" "documents" {
  name               = "documents"
  storage_account_id = azurerm_storage_account.documents.id

  container_access_type = "private"
}

resource "azurerm_storage_container" "classifier_training" {
  name               = "classifier-training"
  storage_account_id = azurerm_storage_account.documents.id

  container_access_type = "private"
}