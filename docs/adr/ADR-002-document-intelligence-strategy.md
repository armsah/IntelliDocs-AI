# ADR-002: Azure AI Document Intelligence Strategy

- Status: Accepted
- Phase: P0

## Context

The platform requires OCR, layout understanding, document classification, structured field extraction, and table extraction.

A custom machine-learning stack would significantly increase project complexity without initially demonstrating more enterprise engineering value.

## Decision

Use Azure AI Document Intelligence as the primary document AI provider.

Start with managed capabilities including:

- OCR;
- layout extraction;
- prebuilt models where appropriate;
- managed classification and extraction.

Introduce custom Document Intelligence models only when evaluation data demonstrates that managed or prebuilt capabilities do not meet quality requirements.

## Application Boundary

The application will depend on an internal AI provider interface rather than directly coupling domain code to the Azure SDK.

This allows:

- deterministic fake providers in tests;
- easier AI service upgrades;
- alternative providers if required later;
- failure simulation.

## Consequences

Positive:

- faster implementation;
- managed Azure integration;
- less ML infrastructure;
- strong fit for moderate AI complexity;
- simpler operational model.

Trade-offs:

- external service dependency;
- consumption cost;
- model/version behavior must be tracked;
- quality still requires independent evaluation.

## Model Governance

Every processing result will ultimately record:

- AI provider;
- model identifier;
- model version where available;
- processing timestamp;
- extraction version.
