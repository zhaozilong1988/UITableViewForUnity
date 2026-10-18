# UITableView feature and regression checks

## Run the reproduction scene

Open `8_RegressionTests.unity` and enter Play Mode. The dashboard fills the screen and stacks its panels on narrow windows.

- **Run all checks** updates the PASS / FAIL / PENDING totals and every result row. Failed checks appear first.
- Select a result row to compare **EXPECTED** with **MEASURED AT COMPLETION**. Selection preserves the result and shows its retained table without rerunning it.
- **Run selected** reruns only that case; the other results remain available.
- The read-only overview reveals all loaded cells, including those outside the tested viewport. The cyan outline marks the actual viewport; the measured table geometry is unchanged. Cell #10 is green when present. Previews are scaled to fit; the numerical measurements remain in the original table units. Empty cases show **EMPTY TABLE / 0 cells**.
- **Interact** opens a separate live table with the selected result's data/configuration. Drag to scroll and click a cell to highlight it; the caption reports the input event. Empty/short-content cases explain why no scrolling is available. **Result view** discards manual changes and restores the original frozen preview. Selection, rerun, direction changes and scene close also discard the live table.
- **Fit** shows the entire overview; **Zoom +** cycles through 2x, 4x, and Fit without changing the result.
- Fit/Zoom apply to the saved result overview. Interaction mode fits the viewport once and keeps its scale stable while scrolling; offscreen cells are clipped as in a normal table.

A failed check enables **Failure details**, with a scrollable exception and assertion message. The Console also receives each PASS / FAIL and its failure details.

Use the **direction button** at the upper right to choose TopToBottom, BottomToTop, LeftToRight, or RightToLeft. Changing direction clears previous results/previews so measurements from different directions are never mixed. The direction button is disabled during execution.

The totals cover the scene checks below, not every automated test in the repository. The direct PlayMode checks exercise each case in all four scroll directions; EditMode results remain in Test Runner. Pointer tests send deterministic EventSystem events to actual cell geometry; they do not claim real-device touch or physical mouse coverage.

The dashboard hierarchy is authored in the scene, and result rows use `Prefabs/RegressionResultCell.prefab` with the existing `UITableView` data source/delegate and cell recycling. Temporary reproduction tables use the existing Canvas / ScrollRect / Viewport / Content fixture. Completed result fixtures are retained until that case is rerun or the scene closes; direct automated `RunCase` calls dispose their fixtures immediately. It does not change Build Settings or require extra packages.

To regenerate the authored dashboard and row prefab, use **UIKit > Tests > Create Regression Scene**. This preserves the existing scene/prefab GUIDs. The builder creates the cell prefab after opening its authoring scene and checks its serialized reference before saving.

## Basic feature coverage

The dashboard contains **31 checks: 9 original regressions + 22 basic-feature checks**. Every entry has assertions, expected values, a measured summary and a retained table preview. Each basic entry groups related APIs; it is not a test of every possible platform, input device or data combination.

| Dashboard row | Basic feature / public API |
| --- | --- |
| 10 | Full reload; index, `UITableViewCellLocation`, normalized and current-position reloads |
| 11 | Variable item lengths; `RearrangeData` preserves existing cell objects |
| 12 | `ReloadDataAt`; `RefreshAllLoadedCells` rebinds without new data requests |
| 13 | `UnloadData`; `DestroyCachedReusableCells`; reload after cleanup |
| 14 | Typed/untyped/filtered lookup and enumeration; mixed cell subclasses and reuse identifiers |
| 15 | `RecycleWhenDisappeared`; reuse and rebind after scrolling away/back |
| 16 | `RecycleWhenReloaded`; retain offscreen object identity |
| 17 | `DestroyWhenDisappeared`; destroy and recreate after a frame |
| 18 | `ignoreCellLifeCycle`; manual size with `isAutoResize=false` |
| 19 | Grid column counts, custom widths, all three row alignments, incomplete final row |
| 20 | All scroll alignments; asymmetric margins and displacement |
| 21 | Animated scroll completion; `StopScroll` and one interruption callback |
| 22 | Reach/leave both edges; suppress duplicate boundary notifications |
| 23 | Pointer down/up/click order; overlay/camera coordinates; outside-viewport rejection |
| 24 | Movable and nonmovable dragging; exact touched cell and geometry |
| 25 | `preserveClickEvenDragBegins` disabled/enabled |
| 26 | Flick directions; custom distance/duration trigger limits |
| 27 | Magnetic calibration; speed gate; Start/Completed/Interrupted callbacks |
| 28 | Custom sibling sorting; typed/untyped intersection and maximum-overlap queries |
| 29 | Infinite-loop physical callback identity; changed-count reload; loop disable |
| 30 | Loop wrapping at both boundaries; velocity and continued-drag displacement |
| 31 | Nested scroll routing; invalid index/count/datasource calls |

The saved preview shows the final state of each check. The interactive table uses independent cells/callbacks/scroll state and preserves the saved expected/measured values and PASS/FAIL. It copies the final fixture configuration (including variable grids, cell types, manual sizes and loop options); it does not replay the test steps or change the automatic verdict. Animated-scroll, callback and lifecycle outcomes are recorded in the measured summary; the final preview does not replay those events. Dashboard rows 1-9 retain the original regression IDs and assertions.

## Automated tests

In **Window > General > Test Runner > PlayMode**, run `UIKit.Tests`. Dashboard regressions cover the initial pending state, running all checks, selecting retained previews, rerunning one check, injected failure measurements/details/order, and scrolling/recycling the result rows.

The original nine regressions and the basic-feature cases load this scene and run in all four scrolling directions. Separate tests exercise the scene's **Run all checks** and direction buttons. A smoke test also loads the eight existing sample scenes and checks that they initialize and load cells.

With Unity CLI (close this project in the Editor, or use a separate project copy):

```powershell
unity test . --mode PlayMode --filter UIKit.Tests --output ./Temp/uitableview-tests.xml --timeout 300
```

## Reassessment of the seven review findings

Baseline: `958bfeb`, Unity `6000.6.0f1`, Windows Editor PlayMode. The runtime implementation was left unchanged until the scene-based tests reproduced all seven findings (nine tests failed).

| Finding | Reproduced behavior before the fix | Correction |
| --- | --- | --- |
| 1. Cell count after append/prepend | After 10 → 11 cells, scrolling to index 10 throws `IndexOutOfRangeException` in both operations. | Update logical cell/row counts after adding data. |
| 2. Infinite loop shorter than viewport | For a 100-unit cycle and 1000-unit viewport, the position clamps to the physical end: 300 of 300 units. | Allocate enough copies to keep the central wrap region inside the scrollable range. |
| 3. Incomplete final grid row in a loop | With 5 cells / 3 columns, copies of logical cell 0 occupy different columns. | End each copy at its own logical final row, preserving the configured column width. |
| 4. Grid prepend row information | Prepending 3 cells to 6 cells / 3 columns with 10-unit upper/lower margins produces 400 units instead of 360. | Rebuild row information instead of inserting it before stale rows. |
| 5. Prepending zero cells | An offscreen cell retained by `RecycleWhenReloaded` becomes inaccessible through `GetLoadedCell(0)`. | Return immediately when the count is unchanged. |
| 6. Empty table | Rearranging an empty table and ending a magnetic drag both access a nonexistent holder. | Skip cell loading and magnetic targeting when no holders exist. |
| 7. Equal content and viewport size | The normalized position returned for cell 0 is `(0, NaN)`. | Return a finite origin when there is no scrollable distance. |

Finding 5 is narrower than the initial review suggested: visible cells are registered again by `ReloadCells()`. The confirmed failure concerns **retained offscreen cells**. The reproduction explicitly checks retention before invoking the zero-count prepend.

At commit `2acdf02`, the post-fix run passed **11 tests with no failures**: the nine regression tests (36 direction/case combinations), the scene's Run All button (nine checks), and initialization/cell loading in all eight existing samples. These checks cover runtime behavior and initialization; they are not a target-player build or a visual layout audit of every sample.

The Asset Store Tools removal is now a separate [PR #46](https://github.com/zhaozilong1988/UITableViewForUnity/pull/46). This branch retains the embedded package and lock entry exactly as on `master`, so the UITableView PR does not include that removal. The Unity 6 results below were recorded with the package removed and without vendor compatibility patches. Restoring its original 11.4.4 sources reproduces five vendor compiler errors on Unity `6000.6.0f1`; apply PR #46 before Unity 6 compilation/testing. The historical counts above describe the original regression slice, not the current expanded suite.

## Validation of the expanded dashboard

Validated with Unity `6000.6.0f1` in an isolated project copy: **53 PlayMode tests** (all 31 cases in four directions, Run All in four directions, dashboard behavior, interactive raycast/click/drag and cleanup, and sample smoke coverage) and **19 EditMode tests** (library regressions plus the authored scene/prefab contract). A temporary render-capture check also verified all result names and detail labels at 1280x900, 800x1200 and 960x600; result rows allow two lines for long names. The capture helper is not part of the shipped sample.

These are Editor/PlayMode checks. Unity 2019.4 and target-player/device execution were not run for this slice. Reopen the saved scene after updating scripts/assets so an older in-memory dashboard hierarchy does not override the new authored references.
