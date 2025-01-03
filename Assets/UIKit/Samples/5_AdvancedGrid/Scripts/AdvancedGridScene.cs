using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;

namespace UIKit.Samples
{
	public class AdvancedGridScene : MonoBehaviour, IUIGridViewDataSource, IUITableViewDelegate, IUITableViewDraggable, IUITableViewClickable
	{
		[SerializeField] UITableView _table;
		[SerializeField] UITableViewCell _gridCell;
		[SerializeField] UITableViewCell _emptyCell;
		[SerializeField] Slider _slider;
		[SerializeField] Text _sliderLabel;
		[SerializeField] Text _buttonLabel;

		readonly List<int> _dataList = new List<int>();
		int _columnNumber;
		Mode _mode;
		void Start()
		{
			// Prepare for data source
			for (var i = 0; i < 100; i++) {
				_dataList.Add(i);
			}

			_slider.value = 3;

			// Setup table view
			_table.dataSource = this;
			_table.@delegate = this;
			_table.clickable = this;
			_buttonLabel.text = "Normal Grid";
		}

		void Update()
		{
			var newColumnNumber = Mathf.FloorToInt(_slider.value);
			if (newColumnNumber == _columnNumber)
				return;
			_columnNumber = newColumnNumber;
			_sliderLabel.text = $"Even line: {Mathf.Max(_columnNumber / 2, 1)}, Odd line:{_columnNumber}";
			_table.ReloadData();
		}

		public void OnButtonClicked()
		{
			switch (_mode) {
				case Mode.Normal:
					_mode = Mode.Deletable;
					_table.scrollRect.vertical = true;
					_table.draggable = null;
					_table.ReloadData();
					_buttonLabel.text = "Deletable Grid";
					break;
				case Mode.Draggable:
					_mode = Mode.Normal;
					_table.scrollRect.vertical = true;
					_table.draggable = null;
					_table.ReloadData();
					_buttonLabel.text = "Normal Grid";
					break;
				case Mode.Deletable:
					_mode = Mode.Draggable;
					_table.scrollRect.vertical = false;
					_table.draggable = this;
					_table.ReloadData();
					_buttonLabel.text = "Draggable Grid";
					break;
				
			}
		}

		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			if (_dataList[index] < 0) {
				return tableView.ReuseOrCreateCell(_emptyCell);
			}
			return tableView.ReuseOrCreateCell(_gridCell);
		}

		public int NumberOfCellsInTableView(UITableView tableView)
		{
			return _dataList.Count;
		}

		public float LengthForCellInTableView(UITableView tableView, int index)
		{
			return 200f;
		}

		public int NumberOfColumnPerRow(UITableView tableView, int rowIndex)
		{
			return rowIndex % 2 == 0 ? Mathf.Max(_columnNumber / 2, 1) : _columnNumber;
		}

		public UITableViewAlignment AlignmentOfCellsAtLastRow(UITableView grid)
		{
			return UITableViewAlignment.Center;
		}

		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
		{
			if (_dataList[index] >= 0) {
				tableView.GetLoadedCell<AdvancedGridCell>(index).UpdateData(_dataList[index], _mode);
			}
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
		{
		}

		public Camera TableViewCameraForInteractive(UITableView tableView)
		{
			return null;
		}

		public bool TableViewDragCellMovable(UITableView tableView)
		{
			return true;
		}

		public void TableViewOnBeginDrag(UITableView tableView, int? draggedIndex, PointerEventData eventData)
		{
			if (!draggedIndex.HasValue)
				return;
			if (_dataList[draggedIndex.Value] >= 0 && _mode == Mode.Draggable)
				tableView.GetLoadedCell(draggedIndex.Value).rectTransform.SetAsLastSibling();
		}

		public void TableViewOnDrag(UITableView tableView, int? draggedIndex, PointerEventData eventData)
		{
			if (!draggedIndex.HasValue)
				return;
			foreach (var cell in _table.GetAllLoadedCells<AdvancedGridCell>())
				cell.SetMergeable(false);
			if (tableView.TryFindMostIntersectedCell<AdvancedGridCell>(draggedIndex.Value, out var mostIntersectedCellIndex, out var maxArea) && maxArea > 150f)
				tableView.GetLoadedCell<AdvancedGridCell>(mostIntersectedCellIndex).SetMergeable(true);
		}

		public void TableViewOnEndDrag(UITableView tableView, int? draggedIndex, PointerEventData eventData)
		{
			if (!draggedIndex.HasValue) {
				return;
			}
			foreach (var cell in _table.GetAllLoadedCells<AdvancedGridCell>()) {
				cell.SetMergeable(false);
			}
			if (tableView.TryFindMostIntersectedCell<AdvancedGridCell>(draggedIndex.Value, out var mostIntersectedCellIndex, out var maxArea) && maxArea > 150f) {
				var swap = _dataList[mostIntersectedCellIndex];
				_dataList[mostIntersectedCellIndex] = _dataList[draggedIndex.Value];
				_dataList[draggedIndex.Value] = swap;
				_table.ReloadDataAt(mostIntersectedCellIndex);
				_table.ReloadDataAt(draggedIndex.Value);
			}
			_table.RefreshAllLoadedCells();
		}

		public void TableViewOnPointerDownCellAt(UITableView tableView, int index, PointerEventData eventData)
		{
		}

		public void TableViewOnPointerClickCellAt(UITableView tableView, int index, PointerEventData eventData)
		{
			if (_mode != Mode.Deletable || _dataList[index] < 0)
				return;
			_dataList[index] = -1;
			_table.ReloadDataAt(index);
		}

		public void TableViewOnPointerUpCellAt(UITableView tableView, int index, PointerEventData eventData)
		{
		}

		public enum Mode
		{
			Normal,
			Draggable,
			Deletable,
		}
	}
}
