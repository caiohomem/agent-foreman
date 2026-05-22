# Test Runner and Safety Check Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `ITestRunner`/`ISafetyChecker` abstractions, `ConfiguredCommandTestRunner` + `GitSafetyChecker` implementations, and the `verify <workItemId>` CLI command.

**Architecture:** Follows the established Core-abstractions / Infrastructure-implementations / CLI-orchestration pattern. Safety checker uses the existing `IGitRepository.GetStatusAsync`. Test runner uses `ICommandRunner` with a simple command-string tokenizer (no shell).

**Tech Stack:** .NET 10, xUnit, existing `ICommandRunner`, `IGitRepository`.

---

## File Map

**New Core files:**
- `src/AgentForeman.Core/Testing/TestCommandResult.cs`
- `src/AgentForeman.Core/Testing/TestRunRequest.cs`
- `src/AgentForeman.Core/Testing/TestRunResult.cs`
- `src/AgentForeman.Core/Testing/ITestRunner.cs`
- `src/AgentForeman.Core/Safety/SafetyViolation.cs`
- `src/AgentForeman.Core/Safety/SafetyCheckResult.cs`
- `src/AgentForeman.Core/Safety/ISafetyChecker.cs`

**New Infrastructure files:**
- `src/AgentForeman.Infrastructure/Testing/ConfiguredCommandTestRunner.cs`
- `src/AgentForeman.Infrastructure/Safety/GitSafetyChecker.cs`

**Modified:**
- `src/AgentForeman.Core/State/MissionStatus.cs` — add `TestsPassed`, `TestsFailed`
- `src/AgentForeman.Cli/CliApplication.cs` — add `verify` command + inject `ITestRunner`/`ISafetyChecker`
- `src/AgentForeman.Cli/HelpText.cs` — add `verify` entry
- `tests/AgentForeman.Tests/CliHelpTests.cs` — update expected help text

**New test files:**
- `tests/AgentForeman.Tests/ConfiguredCommandTestRunnerTests.cs`
- `tests/AgentForeman.Tests/GitSafetyCheckerTests.cs`
- `tests/AgentForeman.Tests/CliVerifyTests.cs`

---

### Task 1: Core testing abstractions

**Files:**
- Create: `src/AgentForeman.Core/Testing/TestCommandResult.cs`
- Create: `src/AgentForeman.Core/Testing/TestRunRequest.cs`
- Create: `src/AgentForeman.Core/Testing/TestRunResult.cs`
- Create: `src/AgentForeman.Core/Testing/ITestRunner.cs`

- [ ] Write all four files
- [ ] Run `dotnet build` — expected: success

### Task 2: Core safety abstractions

**Files:**
- Create: `src/AgentForeman.Core/Safety/SafetyViolation.cs`
- Create: `src/AgentForeman.Core/Safety/SafetyCheckResult.cs`
- Create: `src/AgentForeman.Core/Safety/ISafetyChecker.cs`

- [ ] Write all three files
- [ ] Run `dotnet build` — expected: success

### Task 3: MissionStatus additions

**Files:**
- Modify: `src/AgentForeman.Core/State/MissionStatus.cs`

- [ ] Add `TestsPassed`, `TestsFailed` values

### Task 4: ConfiguredCommandTestRunner

**Files:**
- Create: `src/AgentForeman.Infrastructure/Testing/ConfiguredCommandTestRunner.cs`

- [ ] Implement `ITestRunner` — tokenize command strings, run each via `ICommandRunner`, stop on first failure, save `tests.log`

### Task 5: GitSafetyChecker

**Files:**
- Create: `src/AgentForeman.Infrastructure/Safety/GitSafetyChecker.cs`

- [ ] Implement `ISafetyChecker` — call `IGitRepository.GetStatusAsync`, check maxFilesChanged, check forbidden paths (exact + directory prefix)

### Task 6: CLI verify command + help

**Files:**
- Modify: `src/AgentForeman.Cli/CliApplication.cs`
- Modify: `src/AgentForeman.Cli/HelpText.cs`
- Modify: `tests/AgentForeman.Tests/CliHelpTests.cs`

- [ ] Add `ITestRunner?` and `ISafetyChecker?` params to main `Execute` overload
- [ ] Add `RunVerifyCommand` method
- [ ] Update help text and its test

### Task 7: Tests

**Files:**
- Create: `tests/AgentForeman.Tests/ConfiguredCommandTestRunnerTests.cs`
- Create: `tests/AgentForeman.Tests/GitSafetyCheckerTests.cs`
- Create: `tests/AgentForeman.Tests/CliVerifyTests.cs`

- [ ] Write all tests, `dotnet test` — all pass
