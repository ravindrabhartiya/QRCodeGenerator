# AGENTS.md — Working agreement for the QR Code Generator

This file defines mandatory conventions for anyone (human or agent) working in this repo.

## Project shape
- Layout: `src/` (production), `test/` (xUnit), `docs/` (living spec).
- Language/runtime: C# on .NET (SDK 9.x present; see `docs/spec.md` decision log).
- Product code must **not** depend on any QR-code library — the generator is built from scratch.

## Workflow (must follow)
1. **Spec-first & living spec.** `docs/spec.md` is the single source of truth. Update it at the
   end of **every** increment: mark requirement rows Done, record test ids/counts, and log any
   findings/decisions. An increment is not done until code + tests + spec are consistent.
2. **Approval gates.** Wait for the user's explicit acknowledgment **before starting any
   increment/step**. After finishing an increment, stop and report, then wait for ack.
3. **Strict TDD.** Red → Green → Refactor. No production code before a failing test exists.
   Run the full suite (`dotnet test`) before moving on.
4. **Small increments.** Build and test each unit in isolation, then integration/E2E.
5. **Ask first.** Clarify ambiguities with the user before implementing.
6. **Incremental CLI.** Update the `QrGen.Cli` entry point in **every** increment so there is an
   end-to-end runnable artifact at each step — never defer all CLI work to the end. Keep CLI
   logic in a testable `CliRunner.Run(args, stdout, stderr)` method and add CLI tests.

## Documentation rules
- Every public method and property carries a **concise** XML doc comment — just enough to
  explain intent and non-obvious behavior; not verbose. (`GenerateDocumentationFile` is on.)
- Comment only code that needs clarification; avoid narrating obvious code.

## Self code review (required before closing the final increment)
- Perform a self code review of your own changes using **3 different models**.
- Each review must cover: **security, performance, logging, maintainability, logic, and
  test completeness**.
- Present the consolidated findings to the user and ask which to **fix now** vs **add to the
  backlog**. Do not silently fix or silently defer.

## Testing rules
- Prefer table-driven `[Theory]` tests with named reference vectors; assert exact values.
- If too many overlapping/redundant tests accumulate, **flag them for backlog cleanup** rather
  than leaving noise.
- E2E: generated QR PNGs must round-trip decode (ZXing.Net, test-only) back to the input for
  every mode × every EC level.

## Backlog policy
- When a bug or limitation is discovered, **add it to the backlog** in `docs/spec.md` (Findings /
  Backlog section) — do not just fix or mention it in passing.

## Build & test commands
- Build: `dotnet build QrGen.sln`
- Test:  `dotnet test QrGen.sln`
