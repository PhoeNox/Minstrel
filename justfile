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
	dotnet run --project Backend/Backend.csproj -c {{configuration}}

# Orchestrate Backend + Player + Remote (SvelteKit dev servers) via the Aspire AppHost.
aspire:
	dotnet run --project Aspire/AppHost/AppHost.csproj

publish RID="linux-x64" OUTPUT_DIRECTORY="publish/linux-x64":
	dotnet publish App/App.csproj \
		-c {{configuration}} \
		--self-contained true -p:PublishSingleFile=true \
		-r {{RID}} \
		-o {{OUTPUT_DIRECTORY}}
	cd {{OUTPUT_DIRECTORY}} && rm *.pdb
