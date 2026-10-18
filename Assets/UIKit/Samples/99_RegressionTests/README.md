# UITableView feature and regression checks

## Run the reproduction scene

Open `99_RegressionTests.unity` and enter Play Mode. The dashboard fills the screen and stacks its panels on narrow windows.

- **Run all checks** updates the PASS / FAIL / PENDING totals and every result row. Failed checks appear first.
- Select a completed result to compare **EXPECTED** with **MEASURED AT COMPLETION** and operate the result overview directly. Selection does not rerun the check.
- **Run selected** reruns only that case; the other recorded results remain available.
- The ResultView overview keeps all loaded cells visible, including cells outside the blue frame. The blue frame marks the actual tested viewport. Original result geometry determines the overview's scale and position; dragging does not refit or zoom it. Outer card bounds still clip the drawing.
- Drag to scroll or move a cell according to the selected check; click a cell inside the blue frame to highlight it. The caption reports input. Empty and short-content cases explain when no scrolling is available. There are no Interact/Result view switches, Fit or Zoom buttons.
- Manual input uses an independent copy of the table's data/configuration and callbacks. Recorded expected/measured values, elapsed time and PASS/FAIL remain unchanged. Selecting another result, rerunning, changing direction or closing the scene disposes the manipulated copy.

A failed check enables **Failure details**, with a scrollable exception and assertion message. Closing those details returns to the interactive result overview. The Console also receives each PASS / FAIL and its failure details.

Use the **direction button** at the upper right to choose TopToBottom, BottomToTop, LeftToRight, or RightToLeft. Changing direction clears previous results/previews so measurements from different directions are never mixed. The direction button is disabled during execution.

The totals cover the scene checks below, not every automated test in the repository. Direct PlayMode checks exercise each case in all four directions; EditMode results remain in Test Runner. Pointer tests send deterministic EventSystem events to actual cell geometry; they do not claim real-device touch or physical mouse coverage.

The dashboard hierarchy is authored in the scene, and result rows use `Prefabs/RegressionResultCell.prefab` with the existing UITableView data source/delegate and cell recycling. Reproduction tables use the existing Canvas / ScrollRect / Viewport / Content fixture. The unmodified measured fixture is retained internally while an independent copy receives input. It does not change Build Settings or require extra packages.

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

The result overview starts at the final state of each check. The automatically interactive table uses independent cells/callbacks/scroll state and preserves the saved expected/measured values and PASS/FAIL. It copies the final fixture configuration (including variable grids, cell types, manual sizes and loop options); it does not replay the test steps or change the automatic verdict. Animated-scroll, callback and lifecycle outcomes are recorded in the measured summary; the overview does not replay those events. Dashboard rows 1-9 retain the original regression IDs and assertions.

## Folder layout

The regression scene, fixtures, authoring tool and automated tests are kept together in this sample:

```text
99_RegressionTests/
  99_RegressionTests.unity
  Scripts/          Dashboard and reproduction fixtures
  Prefabs/          Authored result row
  Editor/           Scene/prefab authoring tool (UIKit.TestSceneBuilder)
  Tests/
    Editor/         EditMode library and authored-asset tests (UIKit.RegressionTests)
    PlayMode/       Runtime, interaction and sample smoke tests (UIKit.PlayModeTests)
```

EditMode and PlayMode retain separate test assemblies so Unity Test Runner discovers them in the appropriate mode and test code stays separate from the runtime assembly. The authoring tool has its own Editor assembly so the regeneration menu remains available independently of test inclusion. Existing asset and assembly GUIDs are preserved.

## Automated tests

In **Window > General > Test Runner > PlayMode**, run `UIKit.Tests`. Dashboard regressions cover the initial pending state, running all checks, selecting retained previews, rerunning one check, injected failure measurements/details/order, and scrolling/recycling the result rows.

The original nine regressions and the basic-feature cases load this scene and run in all four scrolling directions. Separate tests exercise the scene's **Run all checks** and direction buttons. A smoke test also loads all nine example scenes and checks that they initialize and load cells. It pins the original SimpleTable to 200 finite cells and the independent InfiniteLoop sample to seven looping cells. Two additional loop-scene checks cover the default dataset and a short two-cell cycle, both wrap boundaries, complete recycled-cell binding and raycast-routed pointer dragging.

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

The Asset Store Tools removal was merged separately in [PR #46](https://github.com/zhaozilong1988/UITableViewForUnity/pull/46), and this branch includes the updated `master`. The Unity 6 results below use that package configuration without vendor compatibility patches. The historical counts above describe the original regression slice, not the current expanded suite.

## Validation of the expanded dashboard

Validated with Unity `6000.6.0f1` in an isolated project copy: **53 PlayMode tests** (all 31 cases in four directions, Run All in four directions, dashboard behavior, interactive raycast/click/drag and cleanup, and sample smoke coverage) and **19 EditMode tests** (library regressions plus the authored scene/prefab contract). A temporary render-capture check also verified all result names and detail labels at 1280x900, 800x1200 and 960x600; result rows allow two lines for long names. The capture helper is not part of the shipped sample.

These are Editor/PlayMode checks. Unity 2019.4 and target-player/device execution were not run for this slice. Reopen the saved scene after updating scripts/assets so an older in-memory dashboard hierarchy does not override the new authored references.

## Sample separation and regression-folder rename

`0_SimpleTable/Scripts/SimpleTableScene.cs` matches `master` exactly and keeps the original 200-cell finite table. The independent `8_InfiniteLoop/8_InfiniteLoopScene.unity` example enables infinite looping with seven logical cells, its own controller/cell prefab/images, and responsive cell content. The package sample list includes the new example. It reuses the SimpleTable layout and existing UITableView data source/delegate/recycling pattern without depending on SimpleTable sample assets.

This regression folder and its scene are now named `99_RegressionTests`; existing folder, script, assembly, scene and prefab GUIDs are preserved. The authoring menu, scene loader and prefab contract use the new paths. EditMode and PlayMode retain their existing assembly boundaries.

Validation for this reorganization uses the same isolated Unity `6000.6.0f1` setup: **55 PlayMode tests** and **20 EditMode tests**, including the new example's seven-cell and short two-cell cycles, both physical wrap boundaries, raycast-routed drag, icon containment, complete cell binding and cell-width reflow after viewport changes. After adjusting only the new cell prefab's layout, the two loop checks and sample smoke check are rerun. Temporary render checks cover 800x1200 and 1280x900; their capture helper is removed from the shipped sample. Unity 2019.4 and player/device execution remain unverified.

## Interactive ResultView validation

The result overview preserves the frozen result's full bounds, scale and position, including off-viewport and retained cells, while accepting input directly. Its viewport mask is disabled to keep the surrounding context visible. The blue frame remains aligned with the actual viewport and does not intercept raycasts. There are no view-mode or fit/zoom controls in the authored scene or scene builder.

Validation: 57 PlayMode tests and 20 EditMode tests in isolated Unity `6000.6.0f1`, including direct raycast click/drag in all four directions, full ResultView scale/position, visible off-frame cells, retained offscreen-cell materialization, stable frame during drag, immutable saved results and failure-details/lifecycle handling. A temporary capture check covers 1280x900, 800x1200 and 960x600, and is removed before the final integrated compile. Unity 2019.4 and player/device execution remain unverified.
