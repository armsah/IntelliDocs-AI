resource "azurerm_postgresql_flexible_server" "main" {
  name = "psql-${local.name_prefix}-${random_string.resource_suffix.result}"

  resource_group_name = azurerm_resource_group.main.name
  location            = var.postgresql_location
  zone                = var.postgresql_zone

  version = "16"

  administrator_login    = var.postgresql_administrator_login
  administrator_password = var.postgresql_administrator_password

  sku_name   = var.postgresql_sku_name
  storage_mb = var.postgresql_storage_mb

  backup_retention_days        = 7
  geo_redundant_backup_enabled = false

  public_network_access_enabled = true

  tags = local.common_tags
}

resource "azurerm_postgresql_flexible_server_database" "intellidocs" {
  name      = "intellidocs"
  server_id = azurerm_postgresql_flexible_server.main.id

  charset   = "UTF8"
  collation = "en_US.utf8"
}