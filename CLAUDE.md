# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

Step is an HTN (hierarchical task network) planner used as a text-generation / templating engine. It's intended for Unity but the core (`Step/`) targets `netstandard2.1` so it runs on any .NET or Mono host. The user-facing language is described in `README.md` (basics) and `Documentation/Step Language Reference.{pdf,docx}` (full reference). The `Step/` library is host-agnostic — UI code lives in the separate REPL projects.

## Solution layout

`Step.sln` contains four projects. `global.json` pins the .NET SDK floor to 8.0.0 with `rollForward: latestMajor`.

- **`Step/`** — the core library. `netstandard2.1`, nullable enabled. Has no dependencies beyond `System.Collections.Immutable`. `[assembly: InternalsVisibleTo("Tests")]` is set in `Module.cs`.
- **`Tests/`** — MSTest suite (`net8.0`). `Usings.cs` globally imports MSTest. `TestUtils.cs` contains the `[assembly: DoNotParallelize]` attribute — tests share static state on `Module` and must not run in parallel. Tests reference both `Step` and `StepRepl`.
- **`StepRepl/`** — Avalonia desktop GUI REPL + debugger (`net8.0`, `WinExe`, self-contained, single-file in Release). RID list: `win-x64;osx-x64;osx-arm64;linux-x64`.
- **`StepWasmStandalone/`** — Blazor WebAssembly REPL (`net9.0`). Has its own `StepWasmStandalone.sln` for SDK isolation.

## Common commands

Run from the repo root unless noted.

```bash
dotnet build Step.sln                                       # build everything
dotnet test Tests/Tests.csproj                              # run all tests
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~CallTests"  # one class
dotnet test Tests/Tests.csproj --filter "Name=SomeTestMethod"           # one method
dotnet run --project StepRepl                               # launch the desktop REPL
dotnet run --project StepWasmStandalone                     # launch the Blazor REPL (net9 SDK required)
```

Distribution builds for `StepRepl` go through `StepRepl/publish.py` (see `StepRepl/README.md`). It wraps `dotnet publish` with the right flags and, on macOS targets, copies the build into the `BuildAssets/StepRepl.app` template. Run `python StepRepl/publish.py --help` for options; `--target all` builds every RID, `open` opens the output folder.

## Big-picture architecture

### Parsing pipeline (`Step/Parser/`)

A chain of transducers, documented in `Step/Parser/README.md`:

`TextReader` → **`TokenStream`** (chars → tokens) → **`ExpressionStream`** (tokens → flat tokens with nested `[ ... ]` groups packaged as arrays — like S-exprs but with brackets) → **`DefinitionStream`** (expressions → method definitions). `CSVFileTokenStreamcs.cs` is an alternate front-end that turns CSV rows into the same token shape.

### Loading and execution (`Step/Module.cs`)

`Module` is the top-level container — it holds global-variable values, compound-task definitions, and a parent pointer (`Module.Global` is the root and contains all built-ins). The host-facing entry points are `new Module(name, parent, ...sourceFiles)`, `LoadDirectory(path, recursive)`, `LoadDefinitions(path|TextReader)`, `AddDefinitions(...string)`, and `ParseAndExecute(code, state)`. `LoadDirectory` picks up `.step` and `.csv` files (skipping dotfiles).

Three special declarations are handled inside `LoadDefinitions`:
- `initially: ...` runs at load time and seeds the module's state.
- `require X` triggers `Module.RequireHook(packageName)` so hosts can supply package source from anywhere (filesystem, embedded resources, network). Each package is loaded once.
- `folder_structure` walks the directory tree next to the source file and synthesises pattern-matched methods for each folder, optionally weighted by `weight.txt`.

Cancellation: `Module.Cancel()` flips a static flag and forces the running interpreter to stop; `Module.DefaultSearchLimit` / `SearchLimit` cap how many steps a single Try can execute. The flag is process-wide static — keep that in mind when writing tests or hosting multiple modules.

### Interpreter (`Step/Interpreter/`)

The model is documented in `Step/Interpreter/README.md`. Key idea: backtracking is implemented by **never mutating state**. Every interpreter operation that changes things returns a *new* `BindingEnvironment` / `TextBuffer` / `State` and passes it to a continuation; on backtrack we just discard it. `BindingEnvironment` and `TextBuffer` are `struct`s to avoid heap traffic.

Two control flows for continuations:
- **Success continuation** is an explicit delegate parameter (`Task.Continuation k`), but only invoked at the end of a method's step chain. Within a chain, a step calls `Step.Continue()` which dispatches to the next step or to `k`.
- **Fail continuation** is just C# `return false` — backtracking is plain stack unwinding.

`Method` = a pattern + a chain of `Step` objects in `Step/Interpreter/Steps/` (`Call`, `EmitStep`, `BranchStep`, `AssignmentStep`, `AddStep`, `RemoveNextStep`, `FluentUpdateStep`, `ConjugateVerbStep`, `CoolStep`, `SequenceStep`, `BranchingStep`). A `CompoundTask` (`Step/Tasks/CompoundTask.cs`) is a list of `Method`s tried in order.

### Tasks (`Step/Tasks/`)

`Task` is the abstract base. `CompoundTask` holds methods written in Step. Everything else under `Tasks/Primitives/` is built-in C# tasks bound into `Module.Global` at startup — `Builtins.cs`, `HigherOrderBuiltins.cs`, `MetaTasks.cs`, `ReflectionBuiltins.cs`, plus the predicate/function adapter classes (`SimplePredicate`, `SimpleFunction`, `GeneralPredicate`, `GeneralPrimitive`, `TuplePredicate`, etc.) used to wrap C# delegates as Step tasks.

### Terms and binding (`Step/Terms/`, `Step/Binding/`)

Prolog-style logic variables (`LogicVariable`), pairs/cons cells (`Cons`, `Pair`), feature structures (`FeatureStructure`, `Feature`), and state-variable names (`StateVariableName`). `BindingEnvironment` ties together the local frame, unification binding list, the owning `Module`, and the dynamic `State` (backtrackable global-variable updates). See `Step/Terms/Term representation.md` for the term encoding.

### Output (`Step/Output/`)

`TextBuffer` is the append-only output. `Writer` plus the filter chain (`AOrAnFilter`, `VerbConjugationFilter`, `TokenFilter`) handle English-style post-processing (a/an, verb conjugation, capitalisation). `Inflection.cs` has the morphology tables.

### REPL support (`Step/ReplSupport/`)

`StepThread` runs a Step computation on a worker thread and surfaces it to the GUI/debugger via log events and warnings. Both `StepRepl` (Avalonia) and `StepWasmStandalone` (Blazor) consume this.

## Working with tests

`Tests/TestUtils.cs` has the helpers everything else uses:

- `TestUtils.Module(...defs)` — builds a fresh `Module` with the given source strings.
- `step.Expand(module)` — runs a step chain to completion and returns the produced text. Handy for unit-testing individual steps without going through `ParseAndExecute`.

Tests run sequentially (`[assembly: DoNotParallelize]`) because `Module` exposes process-wide static state (cancellation flags, search limits, `RequireHook`). Don't remove that attribute and don't introduce parallel test execution without addressing the static state first.
