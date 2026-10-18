using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIKit.Samples
{
	// Manual interaction owns fresh cells, callbacks and scroll state; the measured fixture stays frozen.
	public sealed class RegressionInteractivePreview : IDisposable
	{
		public readonly UITableViewRegressionFixture source;
		public readonly UITableViewRegressionFixture fixture;
		public readonly BasicTableViewFixture recorder;
		readonly Action<string> _status;
		readonly Dictionary<int, Vector2> _manualSizes = new Dictionary<int, Vector2>();
		int? _selected;
		float _displayScale = 1;
		public RegressionInteractivePreview(UITableViewRegressionFixture source, Action<string> status)
		{
			this.source = source;
			_status = status;
			var grid = source.table.dataSource is IUIGridViewDataSource;
			fixture = new UITableViewRegressionFixture(source.table.direction, source.ViewportLength, grid);
			try {
				fixture.root.name = "Interactive table";
				fixture.count = source.count; fixture.columns = source.columns; fixture.cellLength = source.cellLength;
				fixture.margin = source.margin; fixture.lifeCycle = source.lifeCycle;
				fixture.table.GetComponent<RectTransform>().sizeDelta = source.table.GetComponent<RectTransform>().rect.size;
				recorder = new BasicTableViewFixture(fixture, grid) { recordEvidence = false };
				if (source.table.@delegate is BasicTableViewFixture original) {
					recorder.variableLengths = original.variableLengths; recorder.variableColumns = original.variableColumns;
					recorder.mixedTypes = original.mixedTypes; recorder.movable = original.movable; recorder.autoResize = original.autoResize;
					recorder.widthRatio = original.widthRatio; recorder.upperMargin = original.upperMargin; recorder.lowerMargin = original.lowerMargin;
					recorder.flickDuration = original.flickDuration; recorder.flickLower = original.flickLower; recorder.flickUpper = original.flickUpper;
					recorder.magneticThreshold = original.magneticThreshold; recorder.magneticSpeed = original.magneticSpeed;
					recorder.alignment = original.alignment; recorder.calibration = original.calibration; recorder.revision = original.revision;
				}
				foreach (var cell in source.table.GetAllLoadedCells())
					if (!cell.isAutoResize && cell.index.HasValue) _manualSizes[cell.index.Value] = cell.rectTransform.sizeDelta;
				recorder.cellBound = BindCell;
				recorder.interaction = OnInteraction;
				var table = fixture.table;
				var scroll = table.scrollRect;
				scroll.horizontal = source.table.scrollRect.horizontal; scroll.vertical = source.table.scrollRect.vertical;
				scroll.inertia = source.table.scrollRect.inertia; scroll.decelerationRate = source.table.scrollRect.decelerationRate;
				scroll.movementType = source.table.scrollRect.movementType; scroll.elasticity = source.table.scrollRect.elasticity;
				table.preserveClickEvenDragBegins = source.table.preserveClickEvenDragBegins;
				table.clickable = recorder;
				table.draggable = source.table.draggable != null ? recorder : null;
				table.flickable = source.table.flickable != null ? recorder : null;
				table.magnetic = source.table.magnetic != null ? recorder : null;
				table.reachable = source.table.reachable != null ? recorder : null;
				table.sortable = source.table.sortable != null ? recorder : null;
				// Movable cell dragging and scrolling must not move both targets in one gesture.
				if (table.draggable != null && recorder.movable) scroll.horizontal = scroll.vertical = false;
				table.enableInfiniteLoop = source.table.enableInfiniteLoop;
				table.ignoreCellLifeCycle = source.table.ignoreCellLifeCycle;
				table.ReloadData(source.table.scrollRect.normalizedPosition);
				ShowHint();
			} catch { fixture.Dispose(); throw; }
		}
		public void ShowHint()
		{
			_status(fixture.count == 0 ? "LIVE / empty table; no items to drag" :
				!fixture.table.enableInfiniteLoop && fixture.ContentLength <= fixture.ViewportLength ? "LIVE / content fits; click a cell" :
				fixture.table.draggable != null && recorder.movable ? "LIVE / drag a cell; click to select" : "LIVE / drag to scroll; click a cell");
		}
		public void SetDisplayScale(float scale)
		{
			_displayScale = scale;
			foreach (var cell in fixture.table.GetAllLoadedCells()) BindCell(cell);
		}
		void BindCell(UITableViewCell cell)
		{
			if (cell.index.HasValue && _manualSizes.TryGetValue(cell.index.Value, out var size)) cell.rectTransform.sizeDelta = size;
			var image = cell.GetComponent<UnityEngine.UI.Image>();
			image.color = cell.index == _selected ? new Color(.9f, .59f, .14f) : cell.index == 10 ? new Color(.12f, .65f, .44f) :
				cell.index % 2 == 0 ? new Color(.16f, .36f, .56f) : new Color(.12f, .25f, .39f);
			if (cell is RegressionCell textCell) {
				// Keep item IDs readable in a reduced viewport; revisions remain in the saved result preview.
				textCell.label.text = cell.index.ToString();
				var length = fixture.table.direction.IsVertical() ? cell.rectTransform.rect.height : cell.rectTransform.rect.width;
				textCell.label.fontSize = Mathf.RoundToInt(Mathf.Min(22, length * _displayScale * .6f) / _displayScale);
			}
		}
		void OnInteraction(string name, int? index)
		{
			if (name == "click") {
				_selected = index;
				foreach (var cell in fixture.table.GetAllLoadedCells()) BindCell(cell);
			}
			_status("LIVE / " + name + (index.HasValue ? " #" + index.Value : "") + "  |  result saved");
		}
		public void Dispose()
		{
			recorder.cellBound = null; recorder.interaction = null;
			fixture.Dispose();
		}
	}
}
