resource "azurerm_user_assigned_identity" "review_portal_credential" {
  name = "id-${local.name_prefix}-review-portal"

  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tags = local.common_tags
}

resource "random_uuid" "review_access_scope" {}

resource "azuread_application_registration" "api" {
  display_name = "IntelliDocs API - ${var.environment}"

  description = "Protected IntelliDocs review API."

  sign_in_audience               = "AzureADMyOrg"
  requested_access_token_version = 2
}

resource "azuread_application_permission_scope" "review_access" {
  application_id = azuread_application_registration.api.id
  scope_id       = random_uuid.review_access_scope.result

  value = "Review.Access"
  type  = "User"

  admin_consent_display_name = "Access IntelliDocs review workflows"

  admin_consent_description = "Allows the IntelliDocs Review Portal to access review workflows on behalf of the signed-in user."

  user_consent_display_name = "Access IntelliDocs review workflows"

  user_consent_description = "Allows the IntelliDocs Review Portal to access review workflows on your behalf."
}

resource "azuread_application_registration" "review_portal" {
  display_name = "IntelliDocs Review Portal - ${var.environment}"

  description = "Human-review portal for IntelliDocs document workflows."

  sign_in_audience = "AzureADMyOrg"
}

resource "azuread_application_api_access" "review_portal_api" {
  application_id = azuread_application_registration.review_portal.id
  api_client_id  = azuread_application_registration.api.client_id

  scope_ids = [
    azuread_application_permission_scope.review_access.scope_id
  ]
}

resource "azuread_application_federated_identity_credential" "review_portal_managed_identity" {
  application_id = azuread_application_registration.review_portal.id

  display_name = "review-portal-managed-identity"

  description = "Allows the Review Portal user-assigned managed identity to authenticate as the portal application without a client secret."

  audiences = [
    "api://AzureADTokenExchange"
  ]

  issuer = "https://login.microsoftonline.com/${data.azurerm_client_config.current.tenant_id}/v2.0"

  subject = azurerm_user_assigned_identity.review_portal_credential.principal_id
}