# Logging System (Log / TagLog)

`Log` (static, `Infrastructure.Utilities`) exposes named `TagLog` instances (`Log.Battle`, `Log.Loading`, …). `TagLog` wraps `Debug.unityLogger.Log(LogType, tag, msg)`.

## [HideInCallstack] only works with "Strip logging callstack" enabled
**Context:** Double-clicking console messages opened `TagLog.cs` instead of the call site.
**Finding:** The native Unity console honors `[HideInCallstack]` (jump-to-source skips marked frames) **only** when "Strip logging callstack" is toggled on in the Console window's ⋮ (kebab) menu. With it off, the attribute is ignored entirely.
**Why it matters:** It's a per-user editor setting — if jump-to-source breaks again, check the toggle before suspecting the code.

## Console Pro ignores [HideInCallstack] — use CPIGNORE
**Context:** Same jump-to-source problem in the Console Pro asset.
**Finding:** `ConsolePro.Editor.dll` contains no reference to `HideInCallstack` (verified by string-scanning the DLL). Its own mechanisms: a `CPIGNORE` marker anywhere in a file makes Console Pro skip that file's frames when jumping to source; alternatively right-click a stack entry → "Ignore Wrapper" (per-user, or "Shared Ignore" for team-wide settings).
**Why it matters:** `// CPIGNORE` is committed at the top of `TagLog.cs` — don't remove it; any future log-wrapper file needs the same marker.

## Caller-class prefix via [CallerFilePath] (zero runtime reflection)
**Context:** Wanted `[BATTLE] [CallerClassName] msg` with the class name colored.
**Finding:** Every `D`/`W`/`E` overload has a trailing `[CallerFilePath] string caller = ""` param — baked in at compile time, no stack-trace walking. `Prefixed()` maps path → colored `[ClassName] ` prefix, cached in a static `ConcurrentDictionary`. Relies on the one-class-per-file convention (file name == class name).
**Why it matters:** Reusable pattern for caller identity in logs; adding new TagLog overloads must include the trailing `caller` param or the prefix silently disappears.

## Color-key overloads + overload resolution gotcha
**Finding:** `D<T>(T colorKey, string msg)` colors the whole message via `msg.ColorBasedOnID(colorKey.ToString())` — used for per-entity log colors: `Log.Battle.D(entity, $"...")`. Resolution rule: **two strings always pick the non-generic `D(additionalTag, msg)` subtag overload** (non-generic beats generic), so a string can never be a color key.
**Why it matters:** Passing a string id expecting color silently becomes a subtag instead.

## PROD stripping & rich text caveats
**Finding:** `#if PROD` + `[Conditional("DUMMY_UNUSED_DEFINE")]` on `D` strips the call *including* argument evaluation (interpolation costs nothing in prod). `ColorBasedOnID` hashes the id string to a hue (HSV, 0.6 sat). `<color>` rich-text tags render only in editor consoles — player log files show raw tags (acceptable for `D`; reconsider if coloring `W`/`E` output matters in builds).
