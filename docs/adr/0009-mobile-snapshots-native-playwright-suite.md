# Mobile image snapshots via a native Playwright JS suite, not the C# Verify suite

Status: accepted

Mobile needs E2E image snapshots — as regression cover and as the never-stale screenshots the README embeds. The repo already has an image-snapshot stack (`E2E.Tests`: TUnit + Playwright + Verify, C#), but it exists to drive the real Backend, and Mobile has no Backend: it is a static bundle. **Mobile gets its own `@playwright/test` snapshot suite inside `Mobile/`**, serving the built bundle and asserting with `toHaveScreenshot`, colocated with the app and written in its language.

## Considered Options

- **Extend `E2E.Tests` with MobileTests** (rejected): one tooling stack and uniform `*.verified.png` baselines, but it means testing a TypeScript app through a C# harness whose infrastructure (out-of-process Backend launch, `wwwroot` serving) exists for a topology Mobile deliberately discarded (ADR-0008). The consistency is superficial; the coupling is real.

## Consequences

- **Two snapshot stacks now live in the repo.** Do not "unify" them later by dragging Mobile into the C# suite — this split is deliberate. The stacks share the discipline, not the tooling.
- The Desktop suite's rules carry over as policy, not code: baselines are **environment-specific** (font rendering) and the suite is **local-only, excluded from CI**; wire it into `just e2e` / `just push-check` so it runs against a freshly built bundle, never a stale one.
- **The README embeds these baseline PNGs by path.** Renaming a test or its snapshot silently breaks the README images — check the README when touching baseline names.
