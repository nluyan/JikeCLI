# AGENTS.md

## Focus

- This repository should be treated as a `JikeCLI` workspace first.
- `ConsoleAppFramework` is important here because `JikeCLI` depends on the local source generator project, but most task work should start from `src/JikeCLI`.
- Unless the task explicitly targets framework internals, prefer app-level changes over library-level changes.

## Primary Working Area

- Main app: `src/JikeCLI`
- App solution: `src/JikeCLI/JikeCLI.slnx`
- Entry point: `src/JikeCLI/Program.cs`
- Commands:
- `login`
- `order add`
- Infrastructure:
- `Infrastructure/JikeApiClient.cs`
- `Infrastructure/JikeConfigStore.cs`
- `Infrastructure/PasswordPrompt.cs`
- Models and serializer context:
- `Models/`
- `Serialization/JikeJsonSerializerContext.cs`

## Repository Context

- The repo also contains the upstream `ConsoleAppFramework` source tree under `src/ConsoleAppFramework*`.
- `JikeCLI` consumes the local `src/ConsoleAppFramework/ConsoleAppFramework.csproj` as an analyzer/source generator.
- Keep that local project reference intact unless the user explicitly asks to change packaging or dependency strategy.

## Solutions

- Prefer `src/JikeCLI/JikeCLI.slnx` for day-to-day app work.
- `ConsoleAppFramework.slnx` is still useful when a task requires framework or test changes, but it is not the default starting point for this repo.

## Toolchain And Style

- Local SDK verified: `.NET 10.0.103`
- `JikeCLI` currently targets `net10.0`.
- Follow `.editorconfig`.
- General text files use `utf-8`, `lf`, and trimmed trailing whitespace.
- `*.cs` files use `utf-8-bom` and 4-space indentation.

## Verified Commands

- Build app: `dotnet build src\JikeCLI\JikeCLI.csproj`
- Run app help: `dotnet run --project src\JikeCLI\JikeCLI.csproj --no-build -- --help`
- Run login help: `dotnet run --project src\JikeCLI\JikeCLI.csproj --no-build -- login --help`
- Run order help: `dotnet run --project src\JikeCLI\JikeCLI.csproj --no-build -- order add --help`
- Run framework generator tests: `dotnet test --project tests\ConsoleAppFramework.GeneratorTests\ConsoleAppFramework.GeneratorTests.csproj`

## Publishing

- Windows publish script: `publish-win.ps1`
- Linux publish script: `publish-linux.sh`
- Both scripts publish `src/JikeCLI/JikeCLI.csproj`.
- Expected publish output: `src/JikeCLI/bin/<Configuration>/net10.0/<Runtime>/publish`
- Linux NativeAOT publishing must run on Linux, WSL, Docker, or CI.

## JikeCLI Behavior Notes

- `Program.cs` configures `HttpClient`, `JikeApiClient`, and `JikeConfigStore` through DI.
- `JikeApiClient.DefaultBaseUrl` currently points to `https://test5011.jikefw.com/`.
- Config is stored in `%USERPROFILE%\.jike\config.json` by default.
- `JIKE_CONFIG_HOME` overrides the config base directory and is useful for sandboxed runs.
- `login` prompts for password interactively.
- `order add` requires a saved token from a previous login.

## Working Agreements

- Start investigations in `src/JikeCLI` unless there is evidence the issue is in the framework generator.
- If you need framework changes for `JikeCLI`, validate that the change does not regress existing framework tests.
- Be careful with the dirty worktree. `src/JikeCLI`, the publish scripts, and sandbox content are currently untracked.
- Do not delete untracked files or rewrite unrelated user changes.
- Prefer `rg` and `rg --files` for search.
- Prefer serial validation commands when working on the same project output.

## Known Caveats

- Several Chinese user-facing strings in `src/JikeCLI` are mojibake. If you edit those files, fix them intentionally instead of propagating corrupted text.
- Parallel `dotnet build` and `dotnet run` executions against the same project can hit transient file-lock errors on generated `.deps.json` files.
- The root `ReadMe.md` mainly documents `ConsoleAppFramework`, not the `JikeCLI` application workflow.

## Good Default Validation After Changes

- `dotnet build src\JikeCLI\JikeCLI.csproj`
- `dotnet run --project src\JikeCLI\JikeCLI.csproj --no-build -- --help`
- If framework internals were touched: `dotnet test --project tests\ConsoleAppFramework.GeneratorTests\ConsoleAppFramework.GeneratorTests.csproj`
