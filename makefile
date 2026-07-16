SLN := foveo.sln

.PHONY: build test clean

build: 
	dotnet build $(SLN) 

test: build
	dotnet test $(SLN) --no-build

clean:
	dotnet clean $(SLN)

run:
	dotnet run --project src/Foveo.API/Foveo.API.csproj
