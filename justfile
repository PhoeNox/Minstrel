solution := "Minstrel.slnx"
configuration := "Release"

ci: build test

build:
	dotnet build {{solution}} -c {{configuration}}

test:
	dotnet test --solution {{solution}} -c {{configuration}} --ignore-exit-code 8

next-solution := "Minstrel.Next.slnx"

# Build the SvelteKit Player bundle into Backend/wwwroot.
next-player:
	cd Player && npm install && npm run build

# Build the SvelteKit Remote bundle into Backend/wwwroot/remote (served under /remote).
next-remote:
	cd Remote && npm install && BASE_PATH=/remote npm run build

# Build the new Backend + Player + Remote.
next-build: next-player next-remote
	dotnet build {{next-solution}} -c {{configuration}}

# Test the new solution: Backend (.NET) and Player (Vitest).
next-test:
	dotnet test --solution {{next-solution}} -c {{configuration}} --ignore-exit-code 8
	cd Player && npm test

# Build the Player + Remote bundles then run the Backend (serves both, auto-opens the Player).
next-run: next-player next-remote
	Music__Directory="{{justfile_directory()}}/Backend/Music" dotnet run --project Backend/Backend.csproj -c {{configuration}}

# Orchestrate Backend + Player + Remote (SvelteKit dev servers) via the Aspire AppHost.
aspire:
	dotnet run --project Aspire/AppHost/AppHost.csproj

# Publish a self-contained Backend (Player + Remote bundled into wwwroot) for one RID.
publish RID="linux-x64" OUTPUT_DIRECTORY="publish/linux-x64": next-player next-remote
	dotnet publish Backend/Backend.csproj \
		-c {{configuration}} \
		--self-contained true \
		-r {{RID}} \
		-o {{OUTPUT_DIRECTORY}}
	cd {{OUTPUT_DIRECTORY}} && rm -f *.pdb
