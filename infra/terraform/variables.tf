variable "project_name" {
  description = "Short project identifier used in Azure resource names."
  type        = string
  default     = "intellidocs"
}

variable "environment" {
  description = "Deployment environment."
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Primary Azure region."
  type        = string
  default     = "germanywestcentral"
}

variable "postgresql_administrator_login" {
  description = "Administrator login for Azure Database for PostgreSQL Flexible Server."
  type        = string
  default     = "intellidocsadmin"
}

variable "postgresql_administrator_password" {
  description = "Administrator password for Azure Database for PostgreSQL Flexible Server."
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.postgresql_administrator_password) >= 12
    error_message = "PostgreSQL administrator password must contain at least 12 characters."
  }
}

variable "postgresql_sku_name" {
  description = "Azure Database for PostgreSQL Flexible Server compute SKU."
  type        = string
  default     = "B_Standard_B1ms"
}

variable "postgresql_storage_mb" {
  description = "Storage allocated to PostgreSQL Flexible Server."
  type        = number
  default     = 32768
}

variable "tags" {
  description = "Additional tags applied to Azure resources."
  type        = map(string)
  default     = {}
}

variable "postgresql_location" {
  description = "Azure region for PostgreSQL Flexible Server. May differ from the primary region when subscription restrictions apply."
  type        = string
  default     = "westeurope"
}

variable "postgresql_zone" {
  description = "Availability zone used by PostgreSQL Flexible Server."
  type        = string
  default     = "3"
}

variable "deploy_application_container_apps" {
  description = "Whether to deploy the IntelliDocs API and human-review portal Container Apps."
  type        = bool
  default     = false
}

variable "api_container_image" {
  description = "OCI image reference for the IntelliDocs API."
  type        = string
  default     = "mcr.microsoft.com/dotnet/samples:aspnetapp"
}

variable "review_portal_container_image" {
  description = "OCI image reference for the IntelliDocs human-review portal."
  type        = string
  default     = "mcr.microsoft.com/dotnet/samples:aspnetapp"
}

variable "worker_container_image" {
  description = "OCI image reference for the IntelliDocs document-processing Worker."
  type        = string
  default     = "mcr.microsoft.com/dotnet/samples:aspnetapp"
}

variable "postgresql_connection_string_key_vault_secret_id" {
  description = "Versionless Key Vault secret ID containing the PostgreSQL application connection string. Required when application Container Apps are deployed."
  type        = string
  default     = ""

  validation {
    condition = (
      !var.deploy_application_container_apps ||
      length(trimspace(var.postgresql_connection_string_key_vault_secret_id)) > 0
    )

    error_message = "postgresql_connection_string_key_vault_secret_id must be supplied when application Container Apps are deployed."
  }
}
