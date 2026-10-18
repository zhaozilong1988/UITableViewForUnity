using System;
using System.Collections;
using System.Collections.Generic;
using UIKit.Helper;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIKit.Samples
{
	// Each case uses real UITableView APIs; failures are caught and displayed by the scene runner.
	public static class BasicTableViewChecks
	{
		public static readonly string[] Titles = {
			"Reload and start positions", "Rearrange variable cell lengths", "Reload one cell and refresh bindings",
			"Unload data and clear cached cells", "Look up and enumerate loaded cells", "Recycle disappearing cells",
			"Retain offscreen cells", "Destroy disappearing cells", "Load every cell and preserve manual size",
			"Grid widths and row alignment", "Scroll alignment, margins and displacement", "Animated scroll and interruption",
			"Reach and leave both edges", "Pointer callbacks and camera coordinates", "Movable and nonmovable dragging",
			"Preserve clicks during dragging", "Flick direction and trigger limits", "Magnetic snapping and state callbacks",
			"Sibling order and intersection queries", "Loop reload and exact callback copies", "Loop wrapping and drag continuity",
			"Nested routing and invalid API calls"
		};
		public static readonly string[] Names = Titles;
		public static readonly string[] Descriptions = {
			"Reload at an index, a cell location, and a normalized position; verify the requested item is reachable.",
			"Change per-item lengths and rearrange without replacing or rebinding existing cells.",
			"Reload one visible item, then refresh its presentation without requesting new data cells.",
			"Unload all cells, destroy pooled GameObjects, then reload the table successfully.",
			"Compare typed, untyped, filtered and invalid-index loaded-cell queries.",
			"Scroll away and back with RecycleWhenDisappeared; verify reuse and rebound indices.",
			"Scroll with RecycleWhenReloaded; verify the original offscreen object remains available.",
			"Scroll with DestroyWhenDisappeared; verify the old GameObject is destroyed and not reused.",
			"Toggle ignoreCellLifeCycle and exercise ReuseOrCreateCell with automatic resizing disabled.",
			"Use alternating column counts, reduced widths and all three row alignments.",
			"Scroll a middle item to each alignment with margins and an explicit displacement.",
			"Complete a timed scroll, then stop a second scroll; callbacks must report the correct outcome once.",
			"Visit both physical edges and the middle; repeated value notifications must not duplicate edge events.",
			"Send down/up/click to a visible item in overlay and camera canvases; reject an outside point.",
			"Move the touched cell, then use a nonmovable drag to track the cell under the pointer.",
			"Begin a drag between pointer down and click with click preservation disabled and enabled.",
			"Flick along both axis directions; reject distances and durations outside configured limits.",
			"Snap to a calibration point and verify Start, Completed and Interrupted states with custom thresholds.",
			"Apply custom sibling ordering and compare typed/untyped overlap and maximum-area queries.",
			"Touch a repeated copy, reload with changed data, and disable looping while validating callback identity.",
			"Wrap from either side, preserve velocity and continue dragging from the wrapped position.",
			"Route off-axis nested drags to the parent, keep local-axis drags local, and reject invalid public calls."
		};
		public static readonly string[] Expectations = {
			"Each requested start item loads; normalized reload is preserved.", "Positive geometry; changed lengths; same loaded objects.",
			"Only target rebinds on reload; refresh makes no data requests.", "No loaded or cached cells after cleanup; reload works.",
			"Typed/untyped queries agree; filters and invalid indices work.", "Old object returns through pool with its correct new index.",
			"Original offscreen cell remains the same instance.", "Old GameObject is destroyed; replacement is distinct.",
			"All items load; manual cell size stays unchanged.", "Configured widths, column counts and alignment are applied.",
			"Applied scroll positions match measured cell geometry.", "Completion=false; stop=true; each callback exactly once.",
			"Each edge transition fires once without duplicate notifications.", "Correct item and callback order in both canvas modes.",
			"Only touched cell moves; nonmovable cells stay in place.", "Click suppressed by default and delivered when preserved.",
			"Two correct directions; no callbacks outside trigger limits.", "Snapped center aligns; state and speed gates are respected.",
			"Sibling order is correct; overlap queries agree on item and area.", "Callbacks resolve their exact physical copy across reload/toggle.",
			"Wrap stays in the central band; velocity and drag delta survive.", "Parent receives only off-axis gestures; invalid calls throw."
		};

		public static UITableViewRegressionFixture CreateFixture(int index, UITableViewDirection direction)
		{
			if (index < 0 || index >= Names.Length) throw new ArgumentOutOfRangeException(nameof(index));
			var grid = index == 9;
			var fixture = new UITableViewRegressionFixture(direction, index == 19 ? 600 : 300, grid);
			fixture.count = index == 9 ? 9 : index == 19 ? 1 : 20;
			fixture.columns = grid ? 3 : 1;
			new BasicTableViewFixture(fixture, grid);
			return fixture;
		}
		public static IEnumerator Run(int index, UITableViewRegressionFixture fixture, Action<string> measure)
		{
			var source = (BasicTableViewFixture)fixture.table.@delegate;
			yield return null;
			fixture.table.ReloadData(0);
			switch (index) {
				case 0: Reload(fixture, source, measure); break;
				case 1: Rearrange(fixture, source, measure); break;
				case 2: ReloadAndRefresh(fixture, source, measure); break;
				case 3: yield return Unload(fixture, source, measure); break;
				case 4: Queries(fixture, source, measure); break;
				case 5: Recycle(fixture, source, measure); break;
				case 6: Retain(fixture, source, measure); break;
				case 7: yield return Destroy(fixture, source, measure); break;
				case 8: AllAndManualSize(fixture, source, measure); break;
				case 9: Grid(fixture, source, measure); break;
				case 10: Align(fixture, source, measure); break;
				case 11: yield return Animation(fixture, source, measure); break;
				case 12: Reach(fixture, source, measure); break;
				case 13: yield return Pointer(fixture, source, measure); break;
				case 14: Drag(fixture, source, measure); break;
				case 15: PreserveClick(fixture, source, measure); break;
				case 16: yield return Flick(fixture, source, measure); break;
				case 17: yield return Magnetic(fixture, source, measure); break;
				case 18: SortAndIntersect(fixture, source, measure); break;
				case 19: LoopCallbacks(fixture, source, measure); break;
				case 20: LoopWrap(fixture, source, measure); break;
				case 21: NestedAndInvalid(fixture, source, measure); break;
				default: throw new ArgumentOutOfRangeException(nameof(index));
			}
		}

		static void Reload(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.table.ReloadData(7); Require(f.table.GetLoadedCell(7) != null, "Index reload must load item 7.");
			var location = new UITableViewCellLocation(9, UITableViewAlignment.Center, false, 0);
			f.table.ReloadData(location); Require(f.table.GetLoadedCell(9) != null, "Location reload must load item 9.");
			var position = f.table.GetNormalizedPositionOfCellAt(location);
			f.table.ReloadData(position);
			Require(Vector2.Distance(f.table.scrollRect.normalizedPosition, position) < .001f, "Normalized reload must preserve the requested position.");
			f.table.ReloadData(); Require(f.table.GetLoadedCell(9) != null, "Current-position reload must retain the visible item.");
			measure("Start indices 7 / 9 loaded\nNormalized position preserved");
		}
		static void Rearrange(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			var cell = f.table.GetLoadedCell(0); var requests = s.cellRequests;
			s.variableLengths = true; f.table.RearrangeData();
			measure("Item #0 length: " + Length(f, cell) + "\nData requests during rearrange: " + (s.cellRequests - requests));
			Require(f.table.GetLoadedCell(0) == cell && s.cellRequests == requests, "Rearranging visible cells must preserve their objects without requesting new cells.");
			foreach (var loaded in f.table.GetAllLoadedCells()) Near(Length(f, loaded), s.Length(loaded.index.Value), "Applied variable item length");
			Require(f.ContentLength > 0 && !float.IsNaN(f.ContentLength), "Content geometry must remain finite and positive.");
		}
		static void ReloadAndRefresh(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			var other = f.table.GetLoadedCell(1); var requests = s.cellRequests; var appears = s.appears;
			s.revision++; f.table.ReloadDataAt(0);
			Require(s.cellRequests == requests + 1 && s.appears == appears + 1, "ReloadDataAt must request and bind only the target cell.");
			Require(f.table.GetLoadedCell(1) == other, "ReloadDataAt must preserve the unrelated cell.");
			requests = s.cellRequests; s.revision++; f.table.RefreshAllLoadedCells();
			Require(s.cellRequests == requests, "RefreshAllLoadedCells must not request cells from the datasource.");
			foreach (var cell in f.table.GetAllLoadedCells<RegressionCell>()) Require(cell.label.text.EndsWith("v2"), "Every loaded label must rebind to the new revision.");
			measure("Target reload requests: 1\nRefresh data requests: 0; labels v2");
		}
		static IEnumerator Unload(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			var pooled = f.table.GetLoadedCell(0);
			f.table.UnloadData();
			var cache = pooled.transform.parent;
			Require(Cells(f).Count == 0 && cache != f.content && cache.childCount > 0, "UnloadData must empty the table and pool recyclable objects.");
			f.table.DestroyCachedReusableCells(); yield return null;
			Require(cache.childCount == 0, "DestroyCachedReusableCells must destroy pooled GameObjects.");
			f.table.ReloadData(0); Require(f.table.GetLoadedCell(0) != null, "Reload must work after cache destruction.");
			measure("Loaded after unload: 0\nCached after destroy: 0; reload OK");
		}
		static void Queries(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			s.mixedTypes = true; f.table.ReloadData(0);
			var all = Cells(f); var typed = new List<RegressionCell>(f.table.GetAllLoadedCells<RegressionCell>());
			Require(all.Count > 0 && typed.Count == all.Count, "Typed/untyped enumeration counts must agree.");
			foreach (var cell in all) {
				Require(f.table.GetLoadedCell<RegressionCell>(cell.index.Value) == cell, "Typed lookup must resolve the same object.");
				Require(f.table.TryGetLoadedCell<RegressionCell>(cell.index.Value, out var found) && found == cell, "TryGetLoadedCell must resolve the same object.");
			}
			var evens = 0;
			foreach (var cell in f.table.GetAllLoadedCells<RegressionCell>(i => i % 2 == 0)) { Require(cell.index % 2 == 0, "Filtered enumeration returned an odd index."); evens++; }
			Require(evens > 0 && !f.table.TryGetLoadedCell<RegressionCell>(-1, out _) && !f.table.TryGetLoadedCell<RegressionCell>(f.count, out _), "Invalid indices must return false.");
			Require(!f.table.TryGetLoadedCell<BasicAlternateCell>(0, out _) && f.table.GetLoadedCell(19) == null, "Wrong-type and offscreen queries must not return an unrelated cell.");
			Throws<ArgumentException>(() => f.table.GetLoadedCell<BasicAlternateCell>(0), "Strict typed lookup of a different cell subclass");
			Require(new List<BasicAlternateCell>(f.table.GetAllLoadedCells<BasicAlternateCell>()).Count > 0, "The table must contain both cell subclasses.");
			f.ScrollTo(19); f.ScrollTo(0);
			foreach (var cell in f.table.GetAllLoadedCells<RegressionCell>()) {
				var alternate = cell.index.Value % 2 != 0;
				Require((cell is BasicAlternateCell) == alternate && cell.reuseIdentifier == (alternate ? "BasicAlternateCell" : "BasicRegressionCell"), "Recycling must keep cell subclasses segregated by reuse identifier.");
				Require(cell.label.text.StartsWith(cell.index.Value + " /"), "Recycled mixed cells must rebind the correct index.");
			}
			measure("Typed / untyped: " + typed.Count + " / " + all.Count + "\n2 cell types / reuse IDs remain segregated");
		}
		static void Recycle(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			var before = Cells(f); f.ScrollTo(19);
			Require(f.table.GetLoadedCell(0) == null, "Disappearing cells must no longer be accessible at their old index.");
			var reused = false;
			foreach (var cell in Cells(f)) if (before.Contains(cell)) reused = true;
			Require(reused, "The destination viewport must reuse a pooled cell.");
			f.ScrollTo(0); Require(f.table.GetLoadedCell(0).index == 0 && s.disappears > 0, "Recycled cells must rebind index and disappearance callbacks.");
			measure("Pooled cell reused: True\nDisappear callbacks: " + s.disappears);
		}
		static void Retain(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.lifeCycle = UITableViewCellLifeCycle.RecycleWhenReloaded; f.table.ReloadData(0);
			var first = f.table.GetLoadedCell(0); f.ScrollTo(19);
			Require(f.table.GetLoadedCell(0) == first && first.gameObject.activeSelf, "Retained offscreen cells must remain alive and accessible.");
			measure("Offscreen item #0 retained: True\nLoaded objects: " + Cells(f).Count);
			f.ScrollTo(0);
		}
		static IEnumerator Destroy(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.lifeCycle = UITableViewCellLifeCycle.DestroyWhenDisappeared; f.table.ReloadData(0);
			var first = f.table.GetLoadedCell(0); f.ScrollTo(19); yield return null;
			Require(first == null, "DestroyWhenDisappeared must destroy the cell GameObject after the frame.");
			f.ScrollTo(0); Require(f.table.GetLoadedCell(0) != null, "A new cell must load when returning.");
			measure("Original GameObject destroyed: True\nReturning item #0 recreated");
		}
		static void AllAndManualSize(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.table.ignoreCellLifeCycle = true; Require(Cells(f).Count == f.count, "ignoreCellLifeCycle must load every item.");
			f.table.ignoreCellLifeCycle = false; s.autoResize = false; f.table.ReloadData(0);
			var cell = f.table.GetLoadedCell(0); cell.rectTransform.sizeDelta = new Vector2(73, 81); f.table.RearrangeData();
			Require(cell.rectTransform.sizeDelta == new Vector2(73, 81), "isAutoResize=false must preserve manually authored size.");
			Require(!cell.isAutoResize && cell.reuseIdentifier == "BasicRegressionCell", "Reuse identifier and resize option must reach the created cell.");
			measure("All-item mode: " + f.count + " loaded\nManual size retained: 73 x 81");
		}
		static void Grid(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			s.variableColumns = true; s.widthRatio = .75f; f.table.ignoreCellLifeCycle = true;
			foreach (UITableViewAlignment alignment in Enum.GetValues(typeof(UITableViewAlignment))) {
				s.alignment = alignment; f.table.ReloadData(0);
				var rowStart = 0; var row = 0;
				while (rowStart < f.count) {
					var columns = s.Columns(row); var used = Math.Min(columns, f.count - rowStart);
					var cross = f.table.direction.IsVertical() ? f.content.rect.width : f.content.rect.height;
					var width = cross / columns * s.widthRatio;
					var empty = cross - used * width;
					var inset = alignment == UITableViewAlignment.Center ? empty / 2 : alignment == UITableViewAlignment.RightOrTop ? empty : 0;
					for (var column = 0; column < used; column++) {
						var cell = f.table.GetLoadedCell(rowStart + column);
						Near(CrossLength(f, cell), width, "Configured grid width");
						Near(CrossStart(f, cell), inset + column * width, "Applied grid alignment");
					}
					rowStart += used; row++;
				}
			}
			measure("Alternating columns: 2 / 3\nWidths 75%; all 3 alignments applied");
		}
		static void Align(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			s.upperMargin = 3; s.lowerMargin = 7; f.table.ReloadData(6);
			var cell = f.table.GetLoadedCell(6); var leading = Leading(f, cell);
			var before = f.table.direction.IsTopToBottomOrRightToLeft() ? s.Upper : s.Lower;
			var after = f.table.direction.IsTopToBottomOrRightToLeft() ? s.Lower : s.Upper;
			foreach (UITableViewAlignment alignment in Enum.GetValues(typeof(UITableViewAlignment))) {
				var margin = alignment != UITableViewAlignment.Center;
				var expected = alignment == UITableViewAlignment.RightOrTop ? leading - before : alignment == UITableViewAlignment.LeftOrBottom ? leading - f.ViewportLength + Length(f, cell) + after : leading - (f.ViewportLength - Length(f, cell)) / 2;
				// The public displacement sign is physical, while ScrollPosition follows data order.
				expected += f.table.direction.IsTopToBottomOrRightToLeft() ? 6 : -6;
				f.table.ScrollToCellAt(6, alignment, margin, 6); f.table.Tick(); f.Pump();
				Near(f.ScrollPosition, expected, "Scroll alignment / margin / displacement");
				f.table.ScrollToCellAt(new UITableViewCellLocation(6, alignment, margin, 6)); f.table.Tick(); f.Pump();
				Near(f.ScrollPosition, expected, "Cell-location scroll overload");
			}
			measure("3 alignments verified\nMargins 3 / 7; displacement 6");
		}
		static IEnumerator Animation(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			var completed = 0; var interrupted = 0;
			f.table.ScrollToCellAt(10, .08f, onScrollingStopped: stop => { if (stop) interrupted++; else completed++; });
			yield return Wait(() => completed == 1 || interrupted != 0, "Timed scroll completion");
			f.Pump(); Require(completed == 1 && interrupted == 0 && f.table.GetLoadedCell(10) != null, "Timed scroll must complete once at its target.");
			f.table.ScrollToCellAt(0, 1, onScrollingStopped: stop => { if (stop) interrupted++; else completed++; });
			yield return null; f.table.StopScroll(); f.table.StopScroll();
			Require(completed == 1 && interrupted == 1, "Stopping a running scroll twice must notify one interruption only.");
			measure("Completed callbacks: 1\nInterrupted callbacks: 1");
		}
		static void Reach(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.table.reachable = s; f.table.ReloadData(new Vector2(.5f, .5f));
			f.table.scrollRect.normalizedPosition = Vector2.one; f.Pump(); f.Pump();
			f.table.scrollRect.normalizedPosition = Vector2.one * .5f; f.Pump();
			f.table.scrollRect.normalizedPosition = Vector2.zero; f.Pump(); f.Pump();
			f.table.scrollRect.normalizedPosition = Vector2.one * .5f; f.Pump();
			measure("Reached top / bottom: " + s.reachedTop + " / " + s.reachedBottom + "\nLeft top / bottom: " + s.leftTop + " / " + s.leftBottom);
			Require(s.reachedTop == 1 && s.reachedBottom == 1 && s.leftTop == 1 && s.leftBottom == 1, "Each physical edge transition must notify once.");
		}
		static IEnumerator Pointer(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.table.clickable = s; var cell = Visible(f); var data = At(cell, null);
			Click(f.table, data); Require(s.clicks == 1 && s.touched == cell && s.events[0].StartsWith("down") && s.events[1].StartsWith("up") && s.events[2].StartsWith("click"), "Overlay pointer callbacks must target the touched item in down/up/click order.");
			var outside = new PointerEventData(EventSystem.current) { position = new Vector2(-10000, -10000) };
			Click(f.table, outside); Require(s.clicks == 1, "Pointer input outside the viewport must be rejected.");
			// The canvas depends on its camera's transform, so the temporary camera must not be its child.
			var cameraObject = new GameObject("Basic camera", typeof(Camera));
			var camera = cameraObject.GetComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = Screen.height * .5f; camera.enabled = false;
			var canvas = f.root.GetComponent<Canvas>();
			try {
				canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; s.camera = camera;
				Canvas.ForceUpdateCanvases(); yield return null;
				cell = Visible(f, camera); data = At(cell, camera); Click(f.table, data);
				Require(s.clicks == 2 && s.touched == cell, "Camera-space pointer callbacks must target the touched item.");
			} finally {
				s.camera = null; canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
				UnityEngine.Object.Destroy(cameraObject); Canvas.ForceUpdateCanvases();
			}
			measure("Overlay / camera clicks: 1 / 1\nOutside viewport: rejected");
		}
		static void Drag(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.table.draggable = s; var cell = Visible(f); var data = At(cell, null); f.table.OnBeginDrag(data);
			data.position += new Vector2(12, 8); f.table.OnDrag(data);
			Require(s.touched == cell && Vector3.Distance(cell.rectTransform.localPosition, f.content.InverseTransformPoint(data.position)) < .1f, "The exact touched cell must follow a movable drag.");
			f.table.OnEndDrag(data); f.table.RearrangeData();
			s.movable = false; cell = Visible(f); var before = cell.rectTransform.localPosition; data = At(cell, null);
			f.table.OnBeginDrag(data); f.table.OnDrag(data); f.table.OnEndDrag(data);
			Require(cell.rectTransform.localPosition == before && s.touched == cell && s.events[s.events.Count - 1].StartsWith("end"), "Nonmovable drag must report the touched item without moving it.");
			measure("Movable drag: exact cell follows\nNonmovable drag: geometry retained");
		}
		static void PreserveClick(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.table.clickable = s; var data = At(Visible(f), null);
			f.table.OnPointerDown(data); f.table.OnBeginDrag(data); f.table.OnEndDrag(data); f.table.OnPointerUp(data); f.table.OnPointerClick(data);
			Require(s.clicks == 0, "Beginning a drag must suppress a click by default.");
			f.table.preserveClickEvenDragBegins = true;
			f.table.OnPointerDown(data); f.table.OnBeginDrag(data); f.table.OnEndDrag(data); f.table.OnPointerUp(data); f.table.OnPointerClick(data);
			Require(s.clicks == 1, "preserveClickEvenDragBegins must permit the click.");
			measure("Default drag click: suppressed\nPreserved drag click: delivered once");
		}
		static IEnumerator Flick(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.table.flickable = s; var center = At(Visible(f), null).position;
			var axis = f.table.direction.IsVertical() ? Vector2.up : Vector2.right;
			foreach (var sign in new[] { 1, -1 }) { var data = new PointerEventData(EventSystem.current) { position = center - axis * 15 * sign }; f.table.OnBeginDrag(data); data.position = center + axis * 15 * sign; f.table.OnEndDrag(data); }
			Require(s.flickDirections.Count == 2 && s.flickDirections[0] == (f.table.direction.IsVertical() ? UITableViewDirection.BottomToTop : UITableViewDirection.LeftToRight) && s.flickDirections[1] == (f.table.direction.IsVertical() ? UITableViewDirection.TopToBottom : UITableViewDirection.RightToLeft), "Flick callbacks must report both physical directions correctly.");
			var small = new PointerEventData(EventSystem.current) { position = center }; f.table.OnBeginDrag(small); small.position += axis * 5; f.table.OnEndDrag(small);
			var large = new PointerEventData(EventSystem.current) { position = center }; f.table.OnBeginDrag(large); large.position += axis * 60; f.table.OnEndDrag(large);
			s.flickDuration = .01f; var slow = new PointerEventData(EventSystem.current) { position = center }; f.table.OnBeginDrag(slow); yield return new WaitForSecondsRealtime(.03f); slow.position += axis * 30; f.table.OnEndDrag(slow);
			Require(s.flickDirections.Count == 2, "Small, large and slow gestures must not trigger flick callbacks.");
			measure("Flick directions: 2 correct\nSmall / large / slow: rejected");
		}
		static IEnumerator Magnetic(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.table.magnetic = s; f.table.ScrollToCellAt(6, .05f);
			yield return Wait(() => s.magneticStates.Contains(UITableViewMagneticState.Completed), "Magnetic completion");
			f.Pump(); var cell = f.table.GetLoadedCell(6);
			var cellPoint = f.table.scrollRect.viewport.InverseTransformPoint(cell.rectTransform.TransformPoint(cell.rectTransform.rect.center));
			Near(f.table.direction.IsVertical() ? cellPoint.y : cellPoint.x, 0, "Magnetic center calibration");
			Require(s.magneticStates.Count == 2 && s.magneticStates[0] == UITableViewMagneticState.Start, "Magnetic completion must emit Start then Completed.");
			f.table.ScrollToCellAt(0, 1); f.table.StopScroll();
			Require(s.magneticStates[s.magneticStates.Count - 1] == UITableViewMagneticState.Interrupted, "Stopping magnetic motion must emit Interrupted.");
			var count = s.magneticStates.Count; f.table.OnEndDrag(new PointerEventData(EventSystem.current));
			f.table.scrollRect.velocity = new Vector2(500, 500); f.table.Tick();
			Require(s.magneticStates.Count == count, "Custom speed threshold must delay snapping while scrolling fast.");
			f.table.scrollRect.velocity = Vector2.zero; f.table.Tick();
			yield return Wait(() => s.magneticStates.Count >= count + 2, "Magnetic speed-gated snap");
			measure("Center calibration: aligned\nStart / Completed / Interrupted observed");
		}
		static void SortAndIntersect(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.table.sortable = s; f.table.RearrangeData();
			var first = f.table.GetLoadedCell(0); var second = f.table.GetLoadedCell(1);
			Require(first.transform.GetSiblingIndex() > second.transform.GetSiblingIndex(), "Custom sibling order must be applied to the loaded objects.");
			second.rectTransform.localPosition = first.rectTransform.localPosition + Vector3.right * 10;
			var untyped = new List<UITableViewCell>(f.table.GetAllIntersectedCells(0));
			var typed = new List<RegressionCell>(f.table.GetAllIntersectedCells<RegressionCell>(0));
			Require(untyped.Contains(second) && typed.Contains((RegressionCell)second) && typed.Count == untyped.Count, "Typed and untyped intersection enumeration must agree.");
			Require(f.table.TryFindMostIntersectedCell(0, out var index, out var area) && index == 1 && area > 0, "Maximum-overlap lookup must identify the moved neighbor.");
			Require(f.table.TryFindMostIntersectedCell<RegressionCell>(0, out var typedIndex, out var typedArea) && typedIndex == index && Mathf.Abs(typedArea - area) < .1f, "Typed maximum-overlap lookup must report the same item and area.");
			measure("Sibling order: descending item ID\nMost overlap: #" + index + ", area " + area.ToString("0"));
			f.table.RearrangeData();
		}
		static void LoopCallbacks(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.table.enableInfiniteLoop = true; f.table.ignoreCellLifeCycle = true; f.table.clickable = s;
			var first = f.table.GetLoadedCell(0); UITableViewCell other = null;
			foreach (var cell in Cells(f)) if (cell != first && RectTransformUtility.RectangleContainsScreenPoint(f.table.scrollRect.viewport, At(cell, null).position)) { other = cell; break; }
			Require(other != null, "The loop fixture must expose a non-first visible copy.");
			Click(f.table, At(other, null)); Require(s.touched == other, "Loop pointer callback must resolve the exact touched physical copy.");
			var disappeared = s.disappears; f.count = 3; f.table.ReloadData();
			Require(s.disappears > disappeared, "Changing loop data must unload old cells with valid callback identities.");
			f.table.enableInfiniteLoop = false; Require(Cells(f).Count == f.count, "Disabling the loop must leave only logical cells.");
			measure("Touched repeated copy resolved\nChanged count / disable callbacks valid");
		}
		static void LoopWrap(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			f.count = 5; f.table.enableInfiniteLoop = true; f.table.ReloadData(0);
			SetPosition(f, 0); f.table.scrollRect.velocity = new Vector2(40, 40); f.Pump(); Near(f.ScrollPosition, 500, "Lower wrap position");
			Near(f.table.direction.IsVertical() ? f.table.scrollRect.velocity.y : f.table.scrollRect.velocity.x, 40, "Velocity after lower wrap");
			SetPosition(f, 900); f.table.scrollRect.velocity = new Vector2(40, 40); f.Pump(); Near(f.ScrollPosition, 400, "Upper wrap position");
			Near(f.table.direction.IsVertical() ? f.table.scrollRect.velocity.y : f.table.scrollRect.velocity.x, 40, "Velocity after wrap");
			var data = At(Visible(f), null); data.button = PointerEventData.InputButton.Left;
			f.table.scrollRect.OnBeginDrag(data); f.table.OnBeginDrag(data);
			SetPosition(f, 900); f.Pump(); var before = f.content.anchoredPosition;
			var axis = f.table.direction.IsVertical() ? Vector2.up : Vector2.right;
			data.position += axis * 10; f.table.scrollRect.OnDrag(data);
			Near(Vector2.Dot(f.content.anchoredPosition - before, axis), 10, "Drag delta after loop wrap");
			f.table.OnEndDrag(data); f.table.scrollRect.OnEndDrag(data);
			measure("Wrapped positions: 500 / 400\nVelocity 40; next drag delta 10");
		}
		static void NestedAndInvalid(UITableViewRegressionFixture f, BasicTableViewFixture s, Action<string> measure)
		{
			var probe = f.table.gameObject.AddComponent<BasicNestedProbe>();
			var nestedObject = new GameObject("Nested routing probe", typeof(RectTransform)); nestedObject.transform.SetParent(f.table.transform, false);
			var rect = nestedObject.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(100, 100);
			var child = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>(); child.SetParent(rect, false); child.sizeDelta = new Vector2(200, 200);
			var nested = nestedObject.AddComponent<NestedScrollRect>(); nested.viewport = rect; nested.content = child;
			nested.horizontal = f.table.direction.IsVertical(); nested.vertical = !nested.horizontal;
			var parentAxis = f.table.direction.IsVertical() ? Vector2.up : Vector2.right;
			var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = At(Visible(f), null).position, delta = parentAxis * 20 };
			nested.OnInitializePotentialDrag(data); nested.OnBeginDrag(data); nested.OnDrag(data); nested.OnEndDrag(data);
			Require(probe.initialized == 1 && probe.began == 1 && probe.dragged == 1 && probe.ended == 1, "Off-axis nested gestures must route each phase to the parent.");
			data.delta = f.table.direction.IsVertical() ? Vector2.right * 20 : Vector2.up * 20;
			nested.OnBeginDrag(data); nested.OnDrag(data); nested.OnEndDrag(data);
			Require(probe.began == 1 && probe.dragged == 1 && probe.ended == 1, "Local-axis nested gestures must stay local.");
			nestedObject.SetActive(false); UnityEngine.Object.Destroy(nestedObject);
			Throws<IndexOutOfRangeException>(() => f.table.ScrollToCellAt(-1), "Negative scroll index");
			Throws<IndexOutOfRangeException>(() => f.table.ScrollToCellAt(f.count), "Past-end scroll index");
			f.count--; Throws<Exception>(f.table.AppendData, "Append with decreased count", "number of cells is decreased"); f.count++; f.table.ReloadData(0);
			var source = f.table.dataSource; f.table.dataSource = null; Throws<Exception>(f.table.ReloadData, "Missing datasource", "DataSource can not be null"); f.table.dataSource = source;
			measure("Nested phases routed once / local kept\nInvalid API calls rejected");
		}

		static List<UITableViewCell> Cells(UITableViewRegressionFixture f) => new List<UITableViewCell>(f.table.GetAllLoadedCells());
		static float Length(UITableViewRegressionFixture f, UITableViewCell cell) => f.table.direction.IsVertical() ? cell.rectTransform.rect.height : cell.rectTransform.rect.width;
		static float CrossLength(UITableViewRegressionFixture f, UITableViewCell cell) => f.table.direction.IsVertical() ? cell.rectTransform.rect.width : cell.rectTransform.rect.height;
		static Rect InContent(UITableViewRegressionFixture f, UITableViewCell cell)
		{
			var corners = new Vector3[4]; cell.rectTransform.GetWorldCorners(corners);
			var min = (Vector2)f.content.InverseTransformPoint(corners[0]); var max = min;
			foreach (var corner in corners) { var p = (Vector2)f.content.InverseTransformPoint(corner); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
			return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
		}
		static float Leading(UITableViewRegressionFixture f, UITableViewCell cell)
		{
			var rect = InContent(f, cell);
			if (f.table.direction.IsVertical()) return f.table.direction == UITableViewDirection.TopToBottom ? f.content.rect.yMax - rect.yMax : rect.yMin - f.content.rect.yMin;
			return f.table.direction == UITableViewDirection.RightToLeft ? f.content.rect.xMax - rect.xMax : rect.xMin - f.content.rect.xMin;
		}
		static float CrossStart(UITableViewRegressionFixture f, UITableViewCell cell)
		{
			var rect = InContent(f, cell);
			return f.table.direction.IsVertical() ? rect.xMin - f.content.rect.xMin : f.content.rect.yMax - rect.yMax;
		}
		static UITableViewCell Visible(UITableViewRegressionFixture f, Camera camera = null)
		{
			foreach (var cell in Cells(f)) if (RectTransformUtility.RectangleContainsScreenPoint(f.table.scrollRect.viewport, At(cell, camera).position, camera)) return cell;
			throw new InvalidOperationException("The fixture has no cell center inside its viewport.");
		}
		static PointerEventData At(UITableViewCell cell, Camera camera) => new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = RectTransformUtility.WorldToScreenPoint(camera, cell.rectTransform.TransformPoint(cell.rectTransform.rect.center)) };
		static void Click(UITableView view, PointerEventData data) { view.OnPointerDown(data); view.OnPointerUp(data); view.OnPointerClick(data); }
		static void SetPosition(UITableViewRegressionFixture f, float position)
		{
			var value = position / (f.ContentLength - f.ViewportLength); var np = f.table.scrollRect.normalizedPosition;
			if (f.table.direction.IsVertical()) np.y = f.table.direction.IsTopToBottomOrRightToLeft() ? 1 - value : value;
			else np.x = f.table.direction.IsTopToBottomOrRightToLeft() ? 1 - value : value;
			f.table.scrollRect.normalizedPosition = np;
		}
		static IEnumerator Wait(Func<bool> ready, string description)
		{
			var deadline = Time.realtimeSinceStartup + 3;
			while (!ready() && Time.realtimeSinceStartup < deadline) yield return null;
			Require(ready(), description + " did not settle within 3 seconds.");
		}
		static void Throws<T>(Action action, string description, string message = null) where T : Exception
		{
			try { action(); } catch (Exception error) {
				Require(error.GetType() == typeof(T) && (message == null || error.Message.Contains(message)), description + " threw an unexpected exception: " + error);
				return;
			}
			throw new InvalidOperationException(description + " must throw.");
		}
		static void Near(float actual, float expected, string description) => Require(!float.IsNaN(actual) && Mathf.Abs(actual - expected) < .1f, description + ": expected " + expected.ToString("0.##") + ", measured " + actual.ToString("0.##"));
		static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
	}
}
