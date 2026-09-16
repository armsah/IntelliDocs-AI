# P12 Load and Runtime Reliability Evidence

## Objective

Validate that the IntelliDocs API can start independently in the local
development environment, expose an operational health probe, and remain
responsive under bounded concurrent requests.

This is a local reliability/performance probe, not a production capacity
benchmark.

## Runtime defect discovered

The initial P12 run exposed an integration gap that the existing automated
test suite did not detect.

The API process could start, but requests to `/health` returned HTTP 500
because authentication middleware attempted to initialize
Microsoft.Identity.Web while the standalone Development environment had no
Entra `AzureAd:ClientId` configuration.

The health probe was changed to a pipeline branch that executes before
authentication middleware. Application controllers remain behind the
existing Entra authentication and authorization pipeline.

A regression test,
`HealthEndpointTests.Health_DoesNotRequireEntraConfiguration`, was added to
verify that the operational health endpoint does not require Entra
configuration.

## Regression result

Full .NET suite after the fix:

- total: 65
- passed: 65
- failed: 0
- skipped: 0

Standalone runtime verification:

- endpoint: `/health`
- HTTP status: 200
- response: `{"status":"healthy","service":"IntelliDocs.Api"}`

## Smoke probe

Configuration:

- requests: 20
- concurrency: 2
- successful: 20
- failed: 0
- success rate: 100%
- elapsed: 0.83 s
- reported throughput: 24.07 requests/s
- P50: 2.11 ms
- P95: 84.11 ms
- P99: 88.66 ms
- max: 88.66 ms

## Bounded-concurrency probe

Configuration:

- requests: 500
- concurrency: 20
- successful: 500
- failed: 0
- success rate: 100%
- elapsed: 45.79 s
- reported throughput: 10.92 requests/s
- P50: 3.77 ms
- P95: 26.40 ms
- P99: 662.57 ms
- max: 750.84 ms

## Interpretation

The primary result is reliability: all 520 measured requests completed
successfully after the health-path fix.

The throughput value must not be interpreted as the API's production
throughput ceiling. The harness uses Windows PowerShell background jobs and
targets a lightweight local health endpoint. Process/job scheduling and
client-side harness overhead materially affect aggregate throughput and
tail latency.

Production capacity would require a dedicated load generator, realistic
authenticated document workloads, representative Azure deployment sizing,
and server-side Azure Monitor telemetry.

## Exit

PASS for the P12 local runtime/load gate.

The API starts independently, the operational health path is regression
tested, and both bounded-concurrency probes completed with zero failed
requests.