# UITableView regression checks

## Run the reproduction scene

Open `8_RegressionTests.unity` and enter Play Mode. The dashboard fills the screen and stacks its panels on narrow windows.

- **Run all 9 checks** updates the PASS / FAIL / PENDING totals and every result row. Failed checks appear first.
- Select a result row to compare **EXPECTED** with **MEASURED AT COMPLETION**. Selection preserves the result and shows its retained table without rerunning it.
- **Run selected check** reruns only that case; the other results remain available.
- The read-only overview reveals all loaded cells, including those outside the tested viewport. The cyan outline marks the actual viewport; the measured table geometry is unchanged. Cell #10 is green when present. Previews are scaled to fit; the numerical measurements remain in the original table units. Empty cases show **EMPTY TABLE / 0 cells**.
- **Fit** shows the entire overview; **Zoom +** cycles through 2x, 4x, and Fit without changing the result.
- A failed check enables **Failure details**, with a scrollable exception and assertion message. The Console also receives each PASS / FAIL and its failure details.

The totals cover these **nine scene checks**, not every automated test in the repository. The direct PlayMode checks also exercise all four scroll directions, while EditMode results are reported in Test Runner.

The dashboard hierarchy is authored in the scene, and result rows use `Prefabs/RegressionResultCell.prefab` with the existing `UITableView` data source/delegate and cell recycling. Temporary reproduction tables use the existing Canvas / ScrollRect / Viewport / Content fixture. Completed interactive fixtures are retained until that case is rerun or the scene closes; direct automated `RunCase` calls dispose their fixtures immediately. It does not change Build Settings or require extra packages.

To regenerate the authored dashboard and row prefab, use **UIKit > Tests > Create Regression Scene**. This preserves the existing scene/prefab GUIDs. The builder creates the cell prefab after opening its authoring scene and checks its serialized reference before saving.

## Automated tests

In **Window > General > Test Runner > PlayMode**, run `UIKit.Tests`. Dashboard regressions cover the initial pending state, running all checks, selecting retained previews, rerunning one check, injected failure measurements/details/order, and scrolling/recycling the result rows.

The nine regression tests load this scene and run each check in all four scrolling directions. A separate test clicks the scene's **Run all cases** button. A smoke test also loads the eight existing sample scenes and checks that they initialize and load cells.

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

Asset Store Tools changes were subsequently excluded from the PR. With that package restored to the PR base, Unity `6000.6.0f1` compilation fails in `PackageExporter.cs` (`GUID`) and `CheckAudioClipping.cs` (obsolete `GetInstanceID()` / `GetAssetPath(int)` APIs), so tests cannot run in that configuration. The passing results above precede that exclusion.
