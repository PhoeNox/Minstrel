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

# Build the new Backend + Player.
next-build: next-player
	dotnet build {{next-solution}} -c {{configuration}}

# Test the new solution: Backend (.NET) and Player (Vitest).
next-test:
	dotnet test --solution {{next-solution}} -c {{configuration}} --ignore-exit-code 8
	cd Player && npm test

# Build the Player bundle then run the Backend (serves the Player, auto-opens it).
next-run: next-player
	dotnet run --project Backend/Backend.csproj -c {{configuration}}

# Orchestrate Backend + Player (SvelteKit dev server) via the Aspire AppHost.
aspire:
	dotnet run --project Aspire/AppHost/AppHost.csproj

publish RID="linux-x64" OUTPUT_DIRECTORY="publish/linux-x64":
	dotnet publish App/App.csproj \
		-c {{configuration}} \
		--self-contained true -p:PublishSingleFile=true \
		-r {{RID}} \
		-o {{OUTPUT_DIRECTORY}}
	cd {{OUTPUT_DIRECTORY}} && rm *.pdb
