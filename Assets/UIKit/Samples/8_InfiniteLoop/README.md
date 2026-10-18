# Infinite-loop table sample

Open `8_InfiniteLoopScene.unity` and enter Play Mode. Drag or scroll in either direction: seven logical cells (0–6) repeat continuously. Each cell displays its logical index, so the wrap is visible. `Number Of Cells` on `InfiniteLoopTableScene` controls the logical item count; the table creates the physical copies needed for its viewport.

`InfiniteLoopTableScene` sets `UITableView.enableInfiniteLoop = true` before `ReloadData()`. It uses the same data source/delegate and `ReuseOrCreateCell` pattern as SimpleTable, with its own serialized scene, cell prefab and image references. Recycled cells are fully rebound in `CellAtIndexInTableViewWillAppear`. A viewport-size change calls the existing `ReloadData()` API to recalculate cell widths and required loop copies while preserving the normalized scroll position. This sample does not modify `0_SimpleTable` or require importing it.

The automated smoke and loop-interaction checks are in `99_RegressionTests/Tests/PlayMode`. They check both the original finite SimpleTable and this independent loop scene, including initialization, repeated logical indices, both wrap boundaries and pointer dragging. These are Editor checks; physical mouse/touch and player builds are not covered.
