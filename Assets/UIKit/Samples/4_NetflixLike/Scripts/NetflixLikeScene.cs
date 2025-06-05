using System.Collections.Generic;
using UnityEngine;

namespace UIKit.Samples
{
	public class NetflixLikeScene : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate, IUITableViewMargin
	{
		[SerializeField] UITableView _tableView;
		[SerializeField] CategoryCell _categoryCellPrefab;

		// remember the scrolled position for reused rows.
		readonly Dictionary<int, Vector2> _normalizedPosDict = new Dictionary<int, Vector2>();

		void Start()
		{
			_tableView.dataSource = this;
			_tableView.margin = this;
			_tableView.@delegate = this;
			_tableView.ReloadData();
		}

		#region IUITableViewDataSource
		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			return tableView.ReuseOrCreateCell(_categoryCellPrefab);
		}

		public int NumberOfCellsInTableView(UITableView tableView)
		{
			return 50;
		}

		public float LengthOfCellAtIndexInTableView(UITableView tableView, int index)
		{
			if (index == 0)
				return 265f;
			return 515;
		}

		#endregion

		#region IUITableViewDelegate
		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
		{
			_normalizedPosDict.TryGetValue(index, out var normalizedPos);
			tableView.GetLoadedCell<CategoryCell>(index).UpdateData(normalizedPos);
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
		{
			var cell = tableView.GetLoadedCell<CategoryCell>(index);
			_normalizedPosDict[index] = cell.normalizedPos;
			cell.UnloadData();
		}
		#endregion

		public float LengthForUpperMarginInTableView(UITableView tableView, int index)
		{
			return 5f;
		}

		public float LengthForLowerMarginInTableView(UITableView tableView, int index)
		{
			return 5f;
		}
	}
}
