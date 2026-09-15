terraform {
  required_version = ">= 1.15.0, < 2.0.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.4"
    }

    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.9"
    }


    random = {
      source  = "hashicorp/random"
      version = "~> 3.7"
    }
  }
}