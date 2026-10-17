# UITableView regression checks

## Run the reproduction scene

Open `8_RegressionTests.unity`, enter Play Mode, and click **Run all cases** or an individual case. Results appear in the scene and Console. `PASS` means the expected behavior holds; the same scene checks fail against the original implementation.

The scene builds a Canvas / ScrollRect / Viewport / Content hierarchy and supplies cells through `IUITableViewDataSource` / `IUITableViewDelegate`, following the existing samples. It does not change Build Settings or require extra packages.

## Automated tests

In **Window > General > Test Runner > PlayMode**, run `UIKit.Tests`.

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
