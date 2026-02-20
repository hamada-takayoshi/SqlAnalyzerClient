# SqlAnalyzerClient

## Build

```powershell
dotnet restore
dotnet build
```

## Run

```powershell
dotnet run --project .\SqlAnalyzer.App
```

## Self-contained publish (Windows x64)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```