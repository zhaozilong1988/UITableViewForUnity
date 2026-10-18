using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UIKit.Samples;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace UIKit.Tests
{
	public partial class UITableViewRegressionTests
	{
		RegressionDashboard Dashboard => _runner.GetComponentInChildren<RegressionDashboard>();

		[UnityTest]
		public IEnumerator InteractiveViewportAcceptsRaycastClicksAndDragsWithoutChangingResults([ValueSource(nameof(Directions))] UITableViewDirection direction)
		{
			while (_runner.testDirection != direction) _runner.CycleDirection();
			_runner.SelectCase(9); _runner.RunSelectedCase(); yield return WaitForRun();
			var saved = _runner.selectedPreview; var result = _runner.results[9];
			var savedPosition = saved.content.anchoredPosition; var measured = result.actual; var elapsed = result.milliseconds;
			var oldRecorder = (BasicTableViewFixture)saved.table.@delegate;
			var oldRequests = oldRecorder.cellRequests; var oldClicks = oldRecorder.clicks;
			yield return null; yield return null;
			var live = Dashboard.interactivePreview;
			Assert.That(live, Is.Not.Null, "A completed result must be interactive without pressing Interact."); Assert.That(live, Is.Not.SameAs(saved));
			AssertFrameMatchesViewport(live);
			Assert.That(live.table.dataSource, Is.Not.SameAs(saved.table.dataSource));
			Assert.That(saved.root.activeSelf, Is.False); Assert.That(saved.table.enabled, Is.False);
			Assert.That(live.table.enabled && live.table.scrollRect.enabled, Is.True);
			Assert.That(live.table.scrollRect.viewport.GetComponent<UnityEngine.UI.RectMask2D>().enabled, Is.False, "ResultView must keep its off-viewport cells visible.");
			Assert.That(live.root.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled, Is.True);
			var scale = live.root.transform.localScale;
			Assert.That(scale.x, Is.LessThan(1), "Exercise input through the dashboard's scaled nested canvas.");
			var recorder = (BasicTableViewFixture)live.table.@delegate;
			var clicked = ClickLiveCell(live);
			Assert.That(recorder.clicks, Is.EqualTo(1)); Assert.That(recorder.touched, Is.SameAs(clicked));
			Assert.That(clicked.GetComponent<UnityEngine.UI.Image>().color.r, Is.GreaterThan(.8f));
			Assert.That(Field<UnityEngine.UI.Text>(Dashboard, "_previewCaption").text, Does.Contain("click #"));
			var before = live.table.scrollRect.normalizedPosition;
			var pointer = LivePointer(live, out var target);
			ExecuteEvents.Execute(target, pointer, ExecuteEvents.initializePotentialDrag);
			ExecuteEvents.Execute(target, pointer, ExecuteEvents.beginDragHandler);
			pointer.delta = direction.IsVertical() ? Vector2.up * 40 : Vector2.right * 40;
			pointer.position += pointer.delta;
			ExecuteEvents.Execute(target, pointer, ExecuteEvents.dragHandler);
			ExecuteEvents.Execute(target, pointer, ExecuteEvents.endDragHandler);
			yield return null; yield return null;
			Assert.That(Vector2.Distance(live.table.scrollRect.normalizedPosition, before), Is.GreaterThan(.001f));
			Assert.That(live.root.transform.localScale, Is.EqualTo(scale), "Dragging must not refit the overview around moving cells.");
			AssertFrameMatchesViewport(live);
			live.ScrollTo(live.count - 1); yield return null;
			Assert.That(live.table.GetLoadedCell(live.count - 1), Is.Not.Null); ClickLiveCell(live);
			live.ScrollTo(0); yield return null; ClickLiveCell(live);
			Assert.That(recorder.clicks, Is.EqualTo(3));
			Assert.That(saved.content.anchoredPosition, Is.EqualTo(savedPosition));
			Assert.That(oldRecorder.cellRequests, Is.EqualTo(oldRequests)); Assert.That(oldRecorder.clicks, Is.EqualTo(oldClicks));
			Dashboard.Refresh();
			Assert.That(Dashboard.interactivePreview, Is.SameAs(live), "Ordinary refresh must preserve the directly manipulated overview.");
			Assert.That(_runner.selectedPreview, Is.SameAs(saved));
			Assert.That(_runner.results[9], Is.SameAs(result)); Assert.That(result.actual, Is.EqualTo(measured)); Assert.That(result.milliseconds, Is.EqualTo(elapsed));
			Assert.That(_runner.passedCount, Is.EqualTo(1)); Assert.That(_runner.failedCount, Is.Zero);
			_runner.SelectCase(8);
			Assert.That(Dashboard.interactivePreview, Is.Null); Assert.That(live.root.activeSelf, Is.False);
			yield return null; Assert.That(live.root == null, Is.True);
			_runner.SelectCase(9); yield return null;
			Assert.That(((BasicTableViewFixture)Dashboard.interactivePreview.table.@delegate).clicks, Is.Zero, "Selecting the completed result again creates fresh input state.");
		}

		static readonly int[] InteractionCopyCases = { 13, 17, 18 };
		[UnityTest]
		public IEnumerator InteractionCopiesMixedTypesManualSizesAndVariableGrid([ValueSource(nameof(InteractionCopyCases))] int index)
		{
			_runner.SelectCase(index); _runner.RunSelectedCase(); yield return WaitForRun();
			var saved = _runner.selectedPreview;
			yield return null;
			var live = Dashboard.interactivePreview;
			Assert.That(live.count, Is.EqualTo(saved.count));
			Assert.That(live.table.GetComponent<RectTransform>().rect.size, Is.EqualTo(saved.table.GetComponent<RectTransform>().rect.size));
			foreach (var original in saved.table.GetAllLoadedCells()) {
				var cell = live.table.GetLoadedCell(original.index.Value);
				if (cell == null) continue; // Retained offscreen cells are materialized when scrolled into the manual viewport.
				Assert.That(cell, Is.Not.SameAs(original)); Assert.That(cell.GetType(), Is.EqualTo(original.GetType()));
				Assert.That(cell.rectTransform.sizeDelta, Is.EqualTo(original.rectTransform.sizeDelta));
				Assert.That(cell.rectTransform.anchoredPosition, Is.EqualTo(original.rectTransform.anchoredPosition));
			}
			if (index == 17) Assert.That(live.table.GetLoadedCell(0).rectTransform.sizeDelta, Is.EqualTo(new Vector2(73, 81)));
			if (index == 18) Assert.That(new List<UITableViewCell>(live.table.GetAllLoadedCells()).Count, Is.EqualTo(9));
			if (index == 13) { live.ScrollTo(19); yield return null; ClickLiveCell(live); live.ScrollTo(0); yield return null; ClickLiveCell(live); }
		}

		[UnityTest]
		public IEnumerator InteractionModeDisposesClonesOnSelectionRunAndDirectionChanges()
		{
			Assert.That(Dashboard.interactivePreview, Is.Null);
			_runner.RunSelectedCase(); Assert.That(Dashboard.interactivePreview, Is.Null); yield return WaitForRun();
			var first = Dashboard.interactivePreview;
			_runner.SelectCase(1); Assert.That(Dashboard.interactivePreview, Is.Null); Assert.That(first.root.activeSelf, Is.False);
			Assert.That(Dashboard.interactivePreview, Is.Null);
			_runner.SelectCase(0); var second = Dashboard.interactivePreview;
			_runner.RunSelectedCase(); Assert.That(Dashboard.interactivePreview, Is.Null); Assert.That(second.root.activeSelf, Is.False);
			yield return WaitForRun(); var third = Dashboard.interactivePreview;
			_runner.CycleDirection(); Assert.That(Dashboard.interactivePreview, Is.Null); Assert.That(third.root.activeSelf, Is.False);
			Assert.That(_runner.pendingCount, Is.EqualTo(_runner.results.Count));
			yield return null;
			Assert.That(first.root == null && second.root == null && third.root == null, Is.True);
		}

		[UnityTest]
		public IEnumerator EmptyAndFailedCasesKeepInteractionAndFailureDetailsIndependent()
		{
			_runner.SelectCase(6); _runner.RunSelectedCase(); yield return WaitForRun();
			yield return null;
			Assert.That(Dashboard.interactivePreview.count, Is.Zero);
			Assert.That(Field<UnityEngine.UI.Text>(Dashboard, "_previewCaption").text, Does.Contain("empty table"));
			_runner.SelectCase(4); _runner.RunSelectedCase();
			foreach (var table in Object.FindObjectsOfType<RegressionTableView>())
				if (table.dataSource is UITableViewRegressionFixture fixture) fixture.margin = 30;
			yield return WaitForRun();
			Assert.That(_runner.failedCount, Is.EqualTo(1)); var savedFailure = _runner.results[4].failure;
			yield return null;
			Assert.That(Dashboard.interactivePreview, Is.Not.Null);
			Field<UnityEngine.UI.Button>(Dashboard, "_errorToggle").onClick.Invoke();
			Assert.That(Dashboard.interactivePreview, Is.Null);
			Assert.That(Field<GameObject>(Dashboard, "_errorPanel").activeInHierarchy, Is.True);
			Assert.That(_runner.results[4].failure, Is.EqualTo(savedFailure));
			Field<UnityEngine.UI.Button>(Dashboard, "_errorToggle").onClick.Invoke();
			Assert.That(Dashboard.interactivePreview, Is.Not.Null, "Closing failure details restores direct interaction.");
			AssertFrameMatchesViewport(Dashboard.interactivePreview);
		}

		[UnityTest]
		public IEnumerator InteractiveLoopWrapsAndClicksTheExactRepeatedCopy()
		{
			_runner.SelectCase(2); _runner.RunSelectedCase(); yield return WaitForRun();
			yield return null;
			var live = Dashboard.interactivePreview; var scale = live.root.transform.localScale;
			var np = live.table.scrollRect.normalizedPosition; np.y = 1;
			live.table.scrollRect.normalizedPosition = np; live.Pump(); yield return null;
			Assert.That(live.ScrollPosition, Is.GreaterThan(50));
			Assert.That(live.ScrollPosition, Is.LessThan(live.ContentLength - live.ViewportLength - 50));
			var cell = ClickLiveCell(live);
			Assert.That(((BasicTableViewFixture)live.table.@delegate).touched, Is.SameAs(cell));
			Assert.That(live.root.transform.localScale, Is.EqualTo(scale));
		}

		[UnityTest]
		public IEnumerator InteractiveFixtureIsDestroyedWithItsScene()
		{
			_runner.RunSelectedCase(); yield return WaitForRun();
			var live = Dashboard.interactivePreview;
			yield return SceneManager.UnloadSceneAsync(_scene); yield return null;
			Assert.That(live.root == null, Is.True);
		}

		static readonly int[] OverviewCases = { 5, 9 };
		[UnityTest]
		public IEnumerator ResultViewKeepsFullOverviewGeometryAndOffscreenCells([ValueSource(nameof(OverviewCases))] int index)
		{
			_runner.SelectCase(index); _runner.RunSelectedCase(); yield return WaitForRun();
			var saved = _runner.selectedPreview; var live = Dashboard.interactivePreview;
			Assert.That(live, Is.Not.Null);
			Assert.That(Vector3.Distance(live.root.transform.localScale, saved.root.transform.localScale), Is.LessThan(.0001f), "Direct input must retain ResultView's full overview scale, not refit only the viewport.");
			Assert.That(Vector2.Distance(live.root.GetComponent<RectTransform>().anchoredPosition, saved.root.GetComponent<RectTransform>().anchoredPosition), Is.LessThan(.01f));
			var hasOutsideCell = false;
			foreach (var cell in live.table.GetAllLoadedCells()) {
				var corners = new Vector3[4]; cell.rectTransform.GetWorldCorners(corners);
				foreach (var corner in corners) {
					var point = live.table.scrollRect.viewport.InverseTransformPoint(corner);
					if (!live.table.scrollRect.viewport.rect.Contains(point)) hasOutsideCell = true;
				}
				Assert.That(cell.GetComponent<CanvasRenderer>().cull, Is.False, "Loaded cells outside the blue frame must remain visible in the overview.");
			}
			Assert.That(hasOutsideCell, Is.True, "Exercise full ResultView context beyond the tested viewport.");
			foreach (var cell in saved.table.GetAllLoadedCells()) {
				var copy = live.table.GetLoadedCell(cell.index.Value);
				Assert.That(copy, Is.Not.Null, "Retained offscreen result cells must remain present in the interactive overview.");
				Assert.That(copy.rectTransform.anchoredPosition, Is.EqualTo(cell.rectTransform.anchoredPosition));
			}
			AssertFrameMatchesViewport(live);
			AssertRemovedPreviewControls();
			var position = live.content.anchoredPosition; Dashboard.Refresh();
			Assert.That(Dashboard.interactivePreview, Is.SameAs(live));
			Assert.That(live.content.anchoredPosition, Is.EqualTo(position));
		}

		void AssertRemovedPreviewControls()
		{
			foreach (var button in Dashboard.GetComponentsInChildren<UnityEngine.UI.Button>(true)) {
				Assert.That(button.name, Is.Not.EqualTo("ToggleInteraction"));
				Assert.That(button.name, Is.Not.EqualTo("PreviewFit"));
				Assert.That(button.name, Is.Not.EqualTo("PreviewZoom"));
			}
		}

		void AssertFrameMatchesViewport(UITableViewRegressionFixture fixture)
		{
			var frame = Field<RectTransform>(Dashboard, "_viewportFrame");
			Assert.That(frame.gameObject.activeInHierarchy, Is.True);
			foreach (var image in frame.GetComponentsInChildren<UnityEngine.UI.Image>()) Assert.That(image.raycastTarget, Is.False, "Frame decoration must not block direct input.");
			var expected = new Vector3[4]; var actual = new Vector3[4];
			fixture.table.scrollRect.viewport.GetWorldCorners(expected); frame.GetWorldCorners(actual);
			for (var i = 0; i < 4; i++) Assert.That(Vector3.Distance(actual[i], expected[i]), Is.LessThan(.1f), "The cyan frame must continue to identify the actual tested viewport.");
		}

		static RegressionCell ClickLiveCell(UITableViewRegressionFixture fixture)
		{
			var data = LivePointer(fixture, out var target);
			var cell = data.pointerCurrentRaycast.gameObject.GetComponentInParent<RegressionCell>();
			Assert.That(cell, Is.Not.Null);
			ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
			ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
			ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
			return cell;
		}
		static PointerEventData LivePointer(UITableViewRegressionFixture fixture, out GameObject target)
		{
			Canvas.ForceUpdateCanvases();
			var canvas = fixture.root.GetComponent<Canvas>().rootCanvas;
			var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
			foreach (var cell in fixture.table.GetAllLoadedCells<RegressionCell>()) {
				var point = RectTransformUtility.WorldToScreenPoint(camera, cell.rectTransform.TransformPoint(cell.rectTransform.rect.center));
				if (!RectTransformUtility.RectangleContainsScreenPoint(fixture.table.scrollRect.viewport, point, camera)) continue;
				var data = new PointerEventData(EventSystem.current) { position = point, pressPosition = point, button = PointerEventData.InputButton.Left };
				var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
				Assert.That(hits.Count, Is.GreaterThan(0));
				Assert.That(hits[0].gameObject.transform.IsChildOf(fixture.table.transform), Is.True, "Preview must receive the first real UI raycast, not a frozen cell/frame/other panel.");
				data.pointerCurrentRaycast = data.pointerPressRaycast = hits[0];
				target = ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject);
				Assert.That(target, Is.EqualTo(fixture.table.gameObject)); return data;
			}
			throw new System.InvalidOperationException("No visible cell center was available for manual preview input.");
		}
	}
}
