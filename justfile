solution := "Minstrel.slnx"
configuration := "Release"

# Primary pre-push gate: full build + every test, including the local-only E2E snapshot suite.
push-check: build test e2e

ci: build test

# Build the SvelteKit Player bundle into Backend/wwwroot.
player:
	cd Player && npm install && npm run build

# Build the SvelteKit Remote bundle into Backend/wwwroot/remote (served under /remote).
remote:
	cd Remote && npm install && BASE_PATH=remote npm run build

# Build the Backend + Player + Remote.
build: player remote
	dotnet build {{solution}} -c {{configuration}}

# Test the Backend (.NET) and the frontends (Vitest). Excludes the E2E suite, which is local-only (see `e2e`).
test: player
	dotnet test --project Core.Tests/Core.Tests.csproj -c {{configuration}} --ignore-exit-code 8
	dotnet test --project FileSystem.Tests/FileSystem.Tests.csproj -c {{configuration}} --ignore-exit-code 8
	dotnet test --project Network.Tests/Network.Tests.csproj -c {{configuration}} --ignore-exit-code 8
	dotnet test --project Backend.Tests/Backend.Tests.csproj -c {{configuration}} --ignore-exit-code 8
	cd Player && npm test
	cd Remote && npm install && npm test

# Run only the Playwright E2E snapshot suite (builds the frontends first). Local-only: too unstable for CI.
e2e: player remote
	dotnet test --project E2E.Tests/E2E.Tests.csproj -c {{configuration}} --ignore-exit-code 8

# Build the Player + Remote bundles then run the Backend (serves both, auto-opens the Player).
run: player remote
	Music__Directory="{{justfile_directory()}}/Backend/Music" dotnet run --project Backend/Backend.csproj -c {{configuration}}

# Publish a self-contained, shippable Backend folder (binary + wwwroot + appsettings + seeded Music + README) for one RID.
publish RID="linux-x64" OUTPUT_DIRECTORY="publish/Minstrel": player remote
	dotnet publish Backend/Backend.csproj \
		-c {{configuration}} \
		--self-contained true \
		-r {{RID}} \
		-o {{OUTPUT_DIRECTORY}} \
		-p:InformationalVersion="$(git describe --tags --always)"
	rm -f {{OUTPUT_DIRECTORY}}/*.pdb
	mkdir -p {{OUTPUT_DIRECTORY}}/Music
	cp Backend/Music/*.m3u Backend/Music/*.mp3 {{OUTPUT_DIRECTORY}}/Music/
	cp packaging/README.txt {{OUTPUT_DIRECTORY}}/README.txt
	git describe --tags --always > {{OUTPUT_DIRECTORY}}/VERSION
