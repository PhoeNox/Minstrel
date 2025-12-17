solution := "Minstrel.slnx"
configuration := "Release"

ci: build test

build:
	dotnet build {{solution}} -c {{configuration}}

test:
	dotnet test --solution {{solution}} -c {{configuration}} 
