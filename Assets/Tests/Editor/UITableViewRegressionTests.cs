using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UIKit;
using UIKit.Helper;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITableViewRegressionTests
{
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    GameObject root;
    GameObject prefabObject;
    UITableView table;
    ScrollRect scroll;
    Source source;

    static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, PrivateInstance).Invoke(target, args);
    object Field(string name) => typeof(UITableView).GetField(name, PrivateInstance).GetValue(table);
    void SetField(string name, object value) => typeof(UITableView).GetField(name, PrivateInstance).SetValue(table, value);
    static T Property<T>(object holder, string name) => (T)holder.GetType().GetProperty(name).GetValue(holder);

    void Create(int count, float viewportLength, bool loop, bool grid = false, bool nested = false)
    {
        root = new GameObject("Regression table", typeof(RectTransform));
        root.SetActive(false);
        var viewport = (RectTransform)root.transform;
        viewport.sizeDelta = new Vector2(300, viewportLength);
        var content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
        content.SetParent(viewport, false);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0, 1);
        content.sizeDelta = new Vector2(300, 0);
        scroll = (ScrollRect)root.AddComponent(nested ? typeof(NestedScrollRect) : typeof(ScrollRect));
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Unrestricted;
        table = root.AddComponent<UITableView>();
        prefabObject = new GameObject("Cell template", typeof(RectTransform), typeof(RegressionCell));
        var prefab = prefabObject.GetComponent<UITableViewCell>();
        source = grid ? new GridSource(prefab) : new Source(prefab);
        source.count = count;
        table.dataSource = source;
        SetField("_enableInfiniteLoop", loop);
        // EditMode does not invoke MonoBehaviour.Awake automatically.
        Call(table, "Awake");
        root.SetActive(true);
        table.ReloadData();
    }

    [TearDown]
    public void Cleanup()
    {
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        if (prefabObject != null) UnityEngine.Object.DestroyImmediate(prefabObject);
    }

    [TestCase(100f)]
    [TestCase(1000f)]
    [TestCase(1050f)]
    public void ShortLoopHasRoomForTheViewportAtBothWrapThresholds(float viewportLength)
    {
        Create(1, viewportLength, true);
        var copies = (int)Field("_infiniteLoopCopyCount");
        var segment = (float)Field("_infiniteLoopSegmentLength");
        var upper = segment * (copies / 2 + .5f);
        Assert.That(upper + viewportLength, Is.LessThanOrEqualTo(scroll.content.rect.height));
        foreach (var position in new[] { 0f, upper + 25f }) {
            var np = (Vector2)Call(table, "GetNormalizedPositionAtScrollPosition", position, Vector2.zero);
            var args = new object[] { np, Vector2.zero };
            Assert.That((bool)Call(table, "TryGetWrappedInfiniteLoopNormalizedPosition", args), Is.True);
            var wrapped = (Vector2)args[1];
            var wrappedPosition = (float)Call(table, "GetScrollPosition", wrapped);
            Assert.That(wrappedPosition, Is.InRange(segment * (copies / 2 - .5f) - .01f, upper + .01f));
            Assert.That(Mathf.Abs((wrappedPosition - position) / segment - Mathf.Round((wrappedPosition - position) / segment)), Is.LessThan(.001f));
        }
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void IncrementalInsertUpdatesPublicIndexRange(bool prepend, bool grid)
    {
        Create(5, 1000, false, grid);
        source.count = 7;
        if (prepend) table.PrependData(); else table.AppendData();
        Assert.That((int)Field("_logicalCellCount"), Is.EqualTo(7));
        Assert.DoesNotThrow(() => table.ScrollToCellAt(6));
        Assert.That(table.TryGetLoadedCell<UITableViewCell>(6, out var cell), Is.True);
        Assert.That(cell.index, Is.EqualTo(6));
    }

    [Test]
    public void PartialGridRowsRepeatWithoutConsumingTheNextCopy()
    {
        Create(5, 200, true, true);
        var holders = (IList)Field("_holders");
        var segment = (float)Field("_infiniteLoopSegmentLength");
        for (var i = 0; i < holders.Count; i++) {
            var logical = i % 5;
            Assert.That(Property<int>(holders[i], "rowIndex"), Is.EqualTo(i / 5 * 2 + logical / 3));
            Assert.That(Property<int>(holders[i], "columnIndex"), Is.EqualTo(logical % 3));
            Assert.That(Property<float>(holders[i], "columnWidth"), Is.EqualTo(100f).Within(.01f));
            Assert.That(Property<float>(holders[i], "rowPosition"), Is.EqualTo(i / 5 * segment + logical / 3 * 100f).Within(.01f));
        }
        Assert.That((int)Call(table, "GetRowCellCount", 3, 1), Is.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DragContinuesFromWrappedPosition(bool nested)
    {
        Create(5, 200, true, false, nested);
        var data = new PointerEventData(null) {
            button = PointerEventData.InputButton.Left,
            position = new Vector2(100, 100),
            delta = Vector2.up
        };
        scroll.OnBeginDrag(data);
        table.OnBeginDrag(data);
        var np = (Vector2)Call(table, "GetNormalizedPositionAtScrollPosition", 800f, Vector2.zero);
        scroll.normalizedPosition = np;
        Assert.That((bool)Call(table, "TryWrapInfiniteLoopNormalizedPosition", np), Is.True);
        var before = scroll.content.anchoredPosition;
        data.position += Vector2.up * 10f;
        scroll.OnDrag(data);
        Assert.That(scroll.content.anchoredPosition.y - before.y, Is.EqualTo(10f).Within(.01f));
        table.OnEndDrag(data);
        scroll.OnEndDrag(data);
        Assert.That(Field("_scrollDragEventData"), Is.Null);
    }

    class Source : IUITableViewDataSource
    {
        readonly UITableViewCell prefab;
        public int count;
        public Source(UITableViewCell prefab) { this.prefab = prefab; }
        public int NumberOfCellsInTableView(UITableView view) => count;
        public float LengthForCellInTableView(UITableView view, int index) => 100f;
        public UITableViewCell CellAtIndexInTableView(UITableView view, int index)
        {
            Assert.That(index, Is.InRange(0, count - 1));
            var cell = view.ReuseOrCreateCell("RegressionCell", prefab);
            Call(cell, "Awake");
            return cell;
        }
    }

    class GridSource : Source, IUIGridViewDataSource
    {
        public GridSource(UITableViewCell prefab) : base(prefab) { }
        public int NumberOfColumnsAtRowInGridView(UITableView view, int row) => 3;
        public UITableViewAlignment AlignmentOfCellsAtRowInGridView(UITableView view, int row) => UITableViewAlignment.LeftOrBottom;
        public float WidthOfCellAtRowInGridView(UITableView view, int row, int column, float average) => average;
    }
}

[ExecuteAlways]
public class RegressionCell : UITableViewCell { }
