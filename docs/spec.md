# QR Code Generator — Living Spec & Progress Tracker

> **This document is the single source of truth.** It is updated at the end of every
> increment (mark requirement rows Done, record tests, log findings/decisions). An increment
> is not "done" until production code, tests, and the matching rows here are all consistent.

---

## 1. Functional Spec

A from-scratch QR Code generator (no QR libraries in product code), built as a learning
exercise following the 8-step challenge.

- **Target:** QR **Version 4** only (33×33 modules).
- **Error-correction levels:** L, M, Q, H (all four).
- **Encoding modes:** Numeric, Alphanumeric, Byte (ISO-8859-1), Kanji (Shift JIS).
- **Masking:** full — evaluate all 8 masks with the 4 penalty rules, pick lowest score.
- **Output:** PNG file (configurable module size) **and** an ASCII/console preview.
- **Shape:** reusable library `QrGen` + thin CLI `QrGen.Cli`
  (`qrgen "<text>" --ec <L|M|Q|H> --out <file.png>`).

### Out of scope (candidate backlog)
- Versions 1–3 and 5–40; automatic version selection.
- ECI, structured-append, mixed-mode segments, micro-QR.

---

## 2. Dev Design

Pipeline (each stage is an isolated, independently tested component):

```
input ─▶ ModeDetector ─▶ DataEncoder ─▶ ReedSolomon+BlockInterleaver
        (Step 1)        (Step 2)        (Step 3)
      ─▶ MatrixBuilder ─▶ MaskSelector ─▶ FormatInfo ─▶ Renderer ─▶ PNG + ASCII
        (Step 4)         (Step 5)        (Step 6)       (Step 7)
```

| Component            | Responsibility (step)                                             |
|----------------------|-------------------------------------------------------------------|
| `ModeDetector`       | Pick simplest sufficient mode (1)                                 |
| `DataEncoder`        | Mode + count + data bits, terminator, padding (2)                 |
| `GaloisField`        | GF(256) log/antilog tables, arithmetic (3)                        |
| `ReedSolomon`        | Generator polynomials, EC codeword computation (3)                |
| `BlockInterleaver`   | Split into blocks, interleave data+EC, append remainder bits (3)  |
| `MatrixBuilder`      | Function patterns + zig-zag data placement (4)                    |
| `MaskSelector`       | 8 masks + 4 penalty rules, choose lowest (5)                      |
| `FormatInfo`         | 15-bit BCH format info placement (6)                              |
| `Renderer`           | Quiet zone + PNG (ImageSharp) + ASCII (7)                         |
| `Version4Tables`     | v4 constants (capacities, block layout, alignment, remainder)     |
| `QrGenerator`        | Public facade orchestrating all steps                             |

---

## 3. Version 4 Reference Tables

- **Matrix size:** 33 × 33. **Total codewords:** 100 (data + EC) for all EC levels.
- **Remainder bits:** 7 (added after interleaved codewords during placement).
- **Alignment pattern center:** (row 26, col 26) — single pattern for v4.
- **Character-count indicator width (v1–9):** Numeric 10, Alphanumeric 9, Byte 8, Kanji 8.
- **Mode indicators:** Numeric `0001`, Alphanumeric `0010`, Byte `0100`, Kanji `1000`.
- **Pad bytes:** alternating `0xEC`, `0x11`.

### 3.1 Data capacity — data codewords per EC level (v4)
| EC | Data codewords | Bits  |
|----|----------------|-------|
| L  | 80             | 640   |
| M  | 64             | 512   |
| Q  | 48             | 384   |
| H  | 36             | 288   |

### 3.2 Error-correction block layout (v4)
| EC | Blocks | EC codewords/block | Data codewords/block |
|----|--------|--------------------|----------------------|
| L  | 1      | 20                 | 80                   |
| M  | 2      | 18                 | 32                   |
| Q  | 2      | 26                 | **24**               |
| H  | 4      | 16                 | 9                    |

> ⚠️ The challenge's Step-3 table lists Q as "29 data codewords/block", which is inconsistent
> (2 × (26+29) = 110 ≠ 100). The ISO-correct value is **24** (2 × (26+24) = 100). We use 24.
> Tracked in §6 Findings.

### 3.3 Max input characters (v4)
| EC | Numeric | Alphanumeric | Byte | Kanji |
|----|---------|--------------|------|-------|
| L  | 187     | 114          | 78   | 48    |
| M  | 149     | 90           | 62   | 38    |
| Q  | 111     | 67           | 46   | 28    |
| H  | 82      | 50           | 34   | 21    |

### 3.4 Format information
- 5 data bits = 2-bit EC indicator (L=`01`, M=`00`, Q=`11`, H=`10`) + 3-bit mask (0–7).
- BCH(15,5), generator `0x537`; XOR mask `0x5412`. Placed in two copies.
- Version information modules: **N/A for v4** (only version ≥ 7).

---

## 4. Increments

0. Environment & scaffolding
1. Mode detection (Step 1)
2. Data encoding to bit string (Step 2)
3. Reed–Solomon error correction (Step 3)
4. Matrix construction (Step 4)
5. Data masking (Step 5)
6. Format information (Step 6)
7. Rendering (Step 7)
8. CLI + end-to-end verification (Step 8)

---

## 5. Requirements Checklist & Progress Tracker

Status legend: ⬜ Not started · 🟡 In progress · ✅ Done

| Step  | Requirement                                                        | Status | Tests | Notes |
|-------|--------------------------------------------------------------------|:------:|-------|-------|
| 0     | .NET SDK available; solution + 3 projects; packages; harness green | ✅ | `HarnessTests` (1) | SDK 9.0.316 used; ImageSharp pinned 2.1.10 |
| 0     | `AGENTS.md` + living `docs/spec.md` created                        | ✅ | — | — |
| 1     | Detect Numeric mode                                                | ✅ | `ModeDetectorTests` (5) | order Numeric→Alpha→Byte→Kanji |
| 1     | Detect Alphanumeric mode                                           | ✅ | `ModeDetectorTests` (8) | 45-char set |
| 1     | Detect Byte (ISO-8859-1) mode                                      | ✅ | `ModeDetectorTests` (6) | code points ≤ 0xFF |
| 1     | Detect Kanji (Shift JIS) mode                                      | ✅ | `ModeDetectorTests` (4) | CodePages 932; ranges 0x8140-0x9FFC, 0xE040-0xEBBF |
| 1     | Reject/handle invalid & edge inputs (empty, mixed)                 | ✅ | `ModeDetectorTests` (4) | null→ArgNull; empty/non-encodable→ArgException |
| 2.1   | Select EC level (L/M/Q/H)                                          | ✅ | `EcLevel` enum | consumed by `Version4`/`DataEncoder` |
| 2.2   | Determine version (fixed v4) + capacity guard                      | ✅ | `DataEncoderTests` (5) | `Version4` tables; Encode throws over capacity |
| 2.3   | Mode indicator (4 bits)                                            | ✅ | covered by segment goldens | 0001/0010/0100/1000 |
| 2.4   | Character count indicator (mode-specific width)                    | ✅ | covered by segment goldens | Num10/Alpha9/Byte8/Kanji8 |
| 2.5   | Encode data per mode (num/alpha/byte/kanji)                        | ✅ | `DataEncoderTests` (6) | num/alpha/byte/kanji goldens |
| 2.5   | Golden test: `HELLO CC WORLD` bit string                          | ✅ | `DataEncoderTests` (1) | exact challenge string |
| 2.6   | Terminator + byte alignment + 0xEC/0x11 padding to capacity        | ✅ | `DataEncoderTests` (5) + `BitBufferTests` (6) | `PadToCapacity` branch-tested |
| 3     | GF(256) arithmetic (log/antilog)                                   | ✅ | `GaloisFieldTests` (6) | primitive 0x11D, gen 2; Multiply(2,128)==29 |
| 3     | Reed–Solomon generator polynomials + EC codewords                  | ✅ | `ReedSolomonTests` (6) | gen(2)==[1,3,2]; Thonky golden EC10 verified |
| 3     | Block split + interleave (per EC layout) + remainder bits          | ✅ | `BlockInterleaverTests` (7) | column-interleave data then EC; remainder bits deferred to Step 4 |
| 4.1   | Allocate 33×33 matrix + function-module map                        | ✅ | `QrMatrixTests` (4) | `QrMatrix` dark/function grids + counts |
| 4.2   | Finder patterns (3 corners)                                        | ✅ | `MatrixBuilderTests` (3) | 7×7 border+centre |
| 4.3   | Separators around finders                                          | ✅ | `MatrixBuilderTests` (1) | light L-strips, reserved |
| 4.4   | Alignment pattern at (26,26)                                       | ✅ | `MatrixBuilderTests` (1) | 5×5 ring+centre |
| 4.5   | Timing patterns                                                    | ✅ | `MatrixBuilderTests` (1) | row 6 / col 6, even→dark |
| 4.6   | Dark module                                                        | ✅ | `MatrixBuilderTests` (1) | (25,8)=(size-8,8) |
| 4.7   | Reserve format (version info N/A for v4)                           | ✅ | `MatrixBuilderTests` (1) | strips reserved, timing preserved |
| 4.8   | Zig-zag data placement skipping function modules                   | ✅ | `MatrixBuilderTests` (3) | 807 data modules; MSB-first; bottom-right start |
| 5     | Apply 8 mask patterns (data modules only)                          | ⬜ | — | — |
| 5     | 4 penalty rules + lowest-score selection                          | ⬜ | — | — |
| 6     | 15-bit BCH format info + placement (2 copies)                      | ⬜ | — | — |
| 7     | Add 4-module quiet zone                                            | ⬜ | — | — |
| 7     | Render PNG (configurable module size)                              | ⬜ | — | — |
| 7     | ASCII/console preview                                              | ⬜ | — | — |
| 8     | CLI arg parsing + output                                           | 🟡 | `CliRunnerTests` (9) | testable `CliRunner.Run`; now shows matrix counts + unmasked ASCII preview |
| 8     | E2E round-trip decode (every mode × every EC level)                | ⬜ | — | — |
| 8     | 3-model self code review; user triage                             | ⬜ | — | — |

---

## 6. Findings / Backlog

- **[spec-typo] v4-Q data codewords:** challenge says 29/block; ISO-correct is **24/block**
  (total 100). Implementation uses 24. (Discovered during planning.)
- **[dep-license] ImageSharp 4.x requires a paid build-time license key** and fails the build
  without one. Pinned to **ImageSharp 2.1.10 (Apache-2.0)** which has no such requirement.
  Backlog: revisit if a newer permissive imaging option is preferred.
- **[scope] Version-only (v4)**: extending to versions 1–40 with auto-selection is future work.

---

## 7. Decision Log

- **Language/runtime:** C# on **.NET SDK 9.0.316** (already installed and working). Original
  plan assumed no SDK; a capable SDK was found, so no new install was needed. .NET 9 emits a
  classic `QrGen.sln` (not `.slnx`).
- **Imaging:** SixLabors.ImageSharp **2.1.10** (Apache-2.0) for PNG output.
- **Test-only decode:** ZXing.Net (0.16.11) for round-trip verification; product code has no
  QR dependency. Pixels are fed to ZXing directly (no ImageSharp binding needed).
- **Modes:** all four, including Kanji (requires `CodePagesEncodingProvider` for Shift-JIS).
- **Masking:** full penalty-based selection.
- **Approval gates:** wait for user ack before starting each increment.
- **Incremental CLI:** the `QrGen.Cli` entry point is grown in every increment (walking
  skeleton) via a testable `CliRunner.Run`, rather than deferring all CLI work to Step 8. As of
  Increment 2 the CLI detects mode and prints the encoded segment bits + data codewords; later
  increments add EC codewords, matrix, mask, and PNG/ASCII rendering.
- **Mode detection (Step 1) behavior:** `ModeDetector.Detect` throws `ArgumentNullException`
  for null, and `ArgumentException` for empty input or input encodable by no single supported
  mode (e.g., emoji, or mixed Latin+Kanji since Byte is scoped to ISO-8859-1). Byte is checked
  before Kanji; the two never overlap (Byte ≤ 0xFF, Kanji is double-byte).
