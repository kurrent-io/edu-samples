.PHONY: publish-edb-commerce
publish-edb-commerce:
	dotnet publish -o ../.artifacts/linux-x64 -c release -p:PublishSingleFile=true --self-contained true -r linux-x64 -p:IncludeNativeLibrariesForSelfExtract=true
	dotnet publish -o ../.artifacts/win-x64 -c release -p:PublishSingleFile=true --self-contained true -r win-x64 -p:IncludeNativeLibrariesForSelfExtract=true
	dotnet publish -o ../.artifacts/linux-arm64 -c release -p:PublishSingleFile=true --self-contained true -r linux-arm64 -p:IncludeNativeLibrariesForSelfExtract=true
	dotnet publish -o ../.artifacts/osx-x64 -c release -p:PublishSingleFile=true --self-contained true -r osx-x64 -p:IncludeNativeLibrariesForSelfExtract=true
	dotnet publish -o ../.artifacts/osx-arm64 -c release -p:PublishSingleFile=true --self-contained true -r osx-arm64 -p:IncludeNativeLibrariesForSelfExtract=true
