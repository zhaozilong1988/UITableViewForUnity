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

		public int NumberOfColumnsAtRowInGridView(UITableView gridView, int rowIndex)
		{
			switch (rowIndex % 3) {
				case 0: return 3;
				case 1: return 4;
				default: return 5;
			}
		}

		public UITableViewAlignment AlignmentOfCellsAtRowInGridView(UITableView gridView, int rowIndex)
		{
			switch (rowIndex % 3) {
				case 0: return UITableViewAlignment.Center;
				case 1: return UITableViewAlignment.LeftOrBottom;
				default: return UITableViewAlignment.RightOrTop;
			}
		}

		public float WidthOfCellAtRowInGridView(UITableView gridView, int rowIndex, int columnIndex, float averageWidthAtRow)
		{
			switch (rowIndex % 3) {
				case 0: return averageWidthAtRow;
				case 1: return columnIndex % 2 == 0 ? averageWidthAtRow / 2f : averageWidthAtRow;
				default: return averageWidthAtRow * 2f / 3f;
			}
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
