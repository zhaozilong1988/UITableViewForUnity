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
			return 500000;
		}

		public float LengthForCellInTableView(UITableView tableView, int index)
		{
			return 300f;
		}

		public int NumberOfColumnPerRow(UITableView tableView, int rowIndex)
		{
			if (rowIndex % 2 == 0) {
				return 4 ;
			}
			return 5;
		}

		public UITableViewAlignment AlignmentOfCellsAtLastRow(UITableView grid)
		{
			return UITableViewAlignment.LeftOrBottom;
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
