solution := "Minstrel.slnx"
configuration := "Release"

ci: build test

build:
	dotnet build {{solution}} -c {{configuration}}

test:
	dotnet test --solution {{solution}} -c {{configuration}}
	
publish RID="linux-x64" OUTPUT_DIRECTORY="publish/linux-x64":
	dotnet publish App/App.csproj \
		-c {{configuration}} \
		--self-contained true /p:PublishSingleFile=true \
		-r {{RID}} \
		-o {{OUTPUT_DIRECTORY}}
	cd {{OUTPUT_DIRECTORY}} && rm *.pdb appsettings.Development.json
