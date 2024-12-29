using UnityEngine;

namespace UIKit.Samples
{
	public class SnappingTableScene : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate, IUITableViewMargin, IUITableViewFlickable, IUITableViewMagneticExtra
	{
		[SerializeField] UITableView _tableView;
		[SerializeField] SnappingTableCell _cellPrefab;
		
		const int CELL_COUNT = 100;
		float _cellWidth;
		readonly Vector2 _calibrationPoint = new Vector2(0.5f, 0.5f);

		void Start()
		{
			_cellWidth = ((RectTransform)_tableView.scrollRect.transform).rect.width;
			_tableView.dataSource = this;
			_tableView.@delegate = this;
			_tableView.marginDataSource = this;
			_tableView.magnetic = this;
			_tableView.flickable = this;
			_tableView.ReloadData();
		}

		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			return tableView.ReuseOrCreateCell(_cellPrefab);
		}

		public int NumberOfCellsInTableView(UITableView tableView)
		{
			return CELL_COUNT;
		}

		public float LengthForCellInTableView(UITableView tableView, int index)
		{
			return _cellWidth;
		}

		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
		{
			tableView.GetLoadedCell<SnappingTableCell>(index).UpdateData();
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
		{
		}

		public Camera TableViewCameraForInteractive(UITableView tableView)
		{
			return null; // Overlay canvas don't need a camera.
		}

		public float LengthForUpperMarginInTableView(UITableView tableView, int rowIndex)
		{
			return 40f;
		}

		public float LengthForLowerMarginInTableView(UITableView tableView, int rowIndex)
		{
			return 40f;
		}

		public void TableViewOnDidFlick(UITableView tableView, int? indexOfFlickedCell, UITableViewDirection direction)
		{
			if (indexOfFlickedCell.HasValue) {
				var toIndex = direction.IsTopToBottomOrRightToLeft() ? indexOfFlickedCell.Value + 1 : indexOfFlickedCell.Value - 1;
				toIndex = Mathf.Clamp(toIndex, 0, CELL_COUNT - 1);
				_tableView.ScrollToCellAt(toIndex, 0.5f);
			}
		}

		public Vector2 MagneticCalibrationPointInTableView(UITableView tableView)
		{
			return _calibrationPoint;
		}

		public void MagneticStateDidChangeInTableView(UITableView tableView, int ofCellIndex, UITableViewMagneticState state)
		{
			Debug.Log("MagneticStateDidChangeInTableView: " + ofCellIndex + ", state: " + state);
		}

		public float MagneticWillBeTriggeredWhenScrollingSpeedBelow(UITableView tableView)
		{
			return 300f;
		}

		public float WhenMagneticIsTriggeredScrollingSpeedWillBeChangedTo(UITableView tableView)
		{
			return 50000f;
		}
	}
}