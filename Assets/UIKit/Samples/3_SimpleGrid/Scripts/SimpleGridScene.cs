using UnityEngine;

namespace UIKit.Samples
{
	public class SimpleGridScene : MonoBehaviour, IUIGridViewDataSource, IUITableViewDelegate
	{
		[SerializeField] UITableView _gridView;
		[SerializeField] UITableViewCell _gridCellPrefab;

		void Start()
		{
			// Tell the grid view that this class will provide that the data it needs
			_gridView.dataSource = this;
			// Tell the grid view that this class will respond to its delegate methods
			_gridView.@delegate = this;

			_gridView.ReloadData();
		}

		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			return tableView.ReuseOrCreateCell(_gridCellPrefab);
		}

		public int NumberOfCellsInTableView(UITableView tableView)
		{
			return 500;
		}

		public float LengthForCellInTableView(UITableView tableView, int index)
		{
			return 300f;
		}

		public int NumberOfColumnAtRowInGridView(UITableView gridView, int rowIndex)
		{
			if (rowIndex % 2 == 0) {
				return 4 ;
			}
			return 5;
		}

		public UITableViewAlignment AlignmentOfCellsAtRowInGridView(UITableView grid, int rowIndex)
		{
			return UITableViewAlignment.LeftOrBottom;
		}

		public float WidthForCellAtRowInGridView(UITableView gridView, int rowIndex, int columnIndex, float averageWidthAtRow)
		{
			if (rowIndex % 3 == 0) {
				return averageWidthAtRow;
			}
			return columnIndex % 2 == 0 ? 100f : 200f;
		}

		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
		{
			tableView.GetLoadedCell<SimpleGridCell>(index).UpdateData(index);
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
		{
		}
	}
}
