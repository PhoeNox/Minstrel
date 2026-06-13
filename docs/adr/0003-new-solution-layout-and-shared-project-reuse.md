# New-solution layout and shared-project reuse

Status: accepted

The Backend + Player rewrite is built as a **second solution in the same repository, with its projects at the repo root** alongside the existing Minstrel projects. A new `Minstrel.Next.slnx` lists the new projects (`Backend`, `Backend.Tests`, plus the SvelteKit `Player` as a non-.NET sibling directory); the existing `Minstrel.slnx` is left untouched and still builds and runs. The Backend **references** `Core`, `Infrastructure.FileSystem`, and `Infrastructure.Network` via `ProjectReference` to the existing project files — they are reused in place, unmodified, not copied.

This is the human call ADR-0002 deferred to the first vertical slice (`.scratch/backend-two-frontend-rewrite/issues/01-spine-play-one-song.md`).

## Considered Options

- **Projects at repo root, new `.slnx` (chosen)**: one repo, both solutions visible side by side; shared projects referenced once. No directory nesting to reason about in `ProjectReference` paths.
- **New solution under a `next/` subdirectory**: cleaner visual separation, but every `ProjectReference` to a shared project crosses a directory boundary and the two solutions look more forked than they are.
- **Copy `Core` / `Infrastructure.*` into the new tree**: full isolation, rejected — it creates two divergent copies of code the rewrite explicitly intends to preserve unchanged.

## Consequences

- One source of truth for `Core` and the two preserved infrastructure projects: a change to a shared record or provider is seen by both solutions, so they cannot silently diverge during the migration.
- Both solutions coexist until the old Blazor stack is retired; `Minstrel.slnx` remains the authority for the old app, `Minstrel.Next.slnx` for the new one.
- The SvelteKit `Player` lives at the repo root as a normal directory and is not a `.slnx` project; its build output is served by the Backend as static files.
