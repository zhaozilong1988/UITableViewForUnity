using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UIKit.Samples;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace UIKit.Tests
{
    public class InfiniteLoopSampleTests
    {
        const string ScenePath = "Assets/UIKit/Samples/8_InfiniteLoop/8_InfiniteLoopScene.unity";
        static readonly int[] Counts = { 7, 2 };
        Scene _scene;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded) yield return SceneManager.UnloadSceneAsync(_scene);
        }

        [UnityTest]
        public IEnumerator AuthoredLoopSampleBindsRepeatedCellsWrapsAndAcceptsDrags([ValueSource(nameof(Counts))] int count)
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
#endif
            _scene = SceneManager.GetSceneByPath(ScenePath);
            yield return null; yield return null;
            InfiniteLoopTableScene controller = null;
            foreach (var root in _scene.GetRootGameObjects()) {
                controller = root.GetComponentInChildren<InfiniteLoopTableScene>();
                if (controller != null) break;
            }
            Assert.That(controller, Is.Not.Null);
            var table = controller.GetComponentInChildren<UITableView>();
            Assert.That(table.enableInfiniteLoop, Is.True);
            Assert.That(controller.NumberOfCellsInTableView(table), Is.EqualTo(7), "The authored sample defaults to seven items.");
            typeof(InfiniteLoopTableScene).GetField("_numberOfCells", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, count);
            table.ReloadData(); yield return null;
            var scroll = table.scrollRect;
            foreach (var boundary in new[] { 0f, 1f }) {
                scroll.verticalNormalizedPosition = boundary;
                yield return null; yield return null;
                scroll.StopMovement();
                Assert.That(scroll.verticalNormalizedPosition, Is.InRange(.001f, .999f), "The sample must wrap away from both physical ends.");
                AssertBoundCells(table, count);
            }
            if (count == 2) {
                var seen = new HashSet<int>(); var repeated = false;
                foreach (var cell in table.GetAllLoadedCells<InfiniteLoopCell>()) repeated |= !seen.Add(cell.index.Value);
                Assert.That(repeated, Is.True, "A short cycle must show multiple bound physical copies of the same logical cell.");
            }
            Canvas.ForceUpdateCanvases();
            var canvas = table.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            PointerEventData pointer = null; GameObject target = null;
            foreach (var cell in table.GetAllLoadedCells<InfiniteLoopCell>()) {
                var point = RectTransformUtility.WorldToScreenPoint(camera, cell.rectTransform.TransformPoint(cell.rectTransform.rect.center));
                if (!RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, point, camera)) continue;
                pointer = new PointerEventData(EventSystem.current) { position = point, pressPosition = point, button = PointerEventData.InputButton.Left };
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(hits[0].gameObject.transform.IsChildOf(table.transform), Is.True);
                pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
                target = ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject);
                break;
            }
            Assert.That(target, Is.EqualTo(table.gameObject), "The authored viewport must route real UI raycasts to the table's drag handler.");
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.beginDragHandler);
            var before = scroll.content.anchoredPosition;
            pointer.delta = Vector2.up * 60; pointer.position += pointer.delta;
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.endDragHandler);
            Assert.That(Vector2.Distance(before, scroll.content.anchoredPosition), Is.GreaterThan(1));
            yield return null;
            AssertBoundCells(table, count);
            var tableRect = table.GetComponent<RectTransform>();
            tableRect.offsetMax -= Vector2.right * 200;
            Canvas.ForceUpdateCanvases(); yield return null; yield return null;
            AssertBoundCells(table, count);
            foreach (var cell in table.GetAllLoadedCells<InfiniteLoopCell>())
                Assert.That(cell.rectTransform.rect.width, Is.EqualTo(scroll.viewport.rect.width).Within(.1f), "A viewport resize must reflow existing loop cells, not leave their icon outside the viewport.");
        }

        static void AssertBoundCells(UITableView table, int count)
        {
            var loaded = 0;
            foreach (var cell in table.GetAllLoadedCells<InfiniteLoopCell>()) {
                loaded++;
                Assert.That(cell.index.Value, Is.InRange(0, count - 1));
                Assert.That(cell.text.text, Is.EqualTo($"Loop Cell: {cell.index.Value}\nKeep scrolling"));
                Assert.That(cell.icon.sprite, Is.Not.Null);
                Assert.That(cell.rectTransform.rect.height, Is.GreaterThan(0));
                Assert.That(cell.text.rectTransform.rect.width, Is.GreaterThan(0));
                Assert.That(cell.text.preferredHeight, Is.LessThanOrEqualTo(cell.text.rectTransform.rect.height));
                var corners = new Vector3[4]; cell.icon.rectTransform.GetWorldCorners(corners);
                foreach (var corner in corners) {
                    var local = cell.rectTransform.InverseTransformPoint(corner);
                    Assert.That(local.x, Is.InRange(cell.rectTransform.rect.xMin - .01f, cell.rectTransform.rect.xMax + .01f));
                    Assert.That(local.y, Is.InRange(cell.rectTransform.rect.yMin - .01f, cell.rectTransform.rect.yMax + .01f));
                }
            }
            Assert.That(loaded, Is.GreaterThan(0));
        }
    }
}
