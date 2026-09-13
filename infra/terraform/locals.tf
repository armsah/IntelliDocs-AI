locals {
  name_prefix = "${var.project_name}-${var.environment}"

  common_tags = merge(
    {
      project     = "IntelliDocs AI"
      environment = var.environment
      managed_by  = "Terraform"
      repository  = "IntelliDocs-AI"
    },
    var.tags
  )
}