using UnityEngine;

namespace UIKit.Samples
{
	public class SimpleTableScene : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate
	{
		[SerializeField] UITableView _tableView;
		[SerializeField] SimpleCell _cellPrefab;

		[SerializeField] Sprite[] _iconSprites;
		[SerializeField] Color[] _backgroundColors;

		void Start()
		{
			// Tell the table view that this class will provide that the data it needs
			_tableView.dataSource = this;
			// Tell the table view that this class will respond to its delegate methods
			_tableView.@delegate = this;

			// Reload the table view to refresh UI
			_tableView.ReloadData();
		}

		#region IUITableViewDataSource
		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			return _tableView.ReuseOrCreateCell(_cellPrefab);
		}

		public int NumberOfCellsInTableView(UITableView tableView)
		{
			return 5000;
		}

		public float LengthForCellInTableView(UITableView tableView, int index)
		{
			return index % 2 == 0 ? 150 : 200;
		}
		#endregion

		#region IUITableViewDelegate
		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
		{
			var cell = tableView.GetLoadedCell<SimpleCell>(index);
			cell.text.text = $"Cell Index: {index}";
			cell.text.color = index % _backgroundColors.Length == 0 ? Color.black : Color.white;
			cell.icon.sprite = _iconSprites[index % _iconSprites.Length];
			cell.background.color = _backgroundColors[index % _backgroundColors.Length];
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
		{
			var cell = tableView.GetLoadedCell<SimpleCell>(index);
			cell.text.text = string.Empty;
			cell.icon.sprite = null;
			cell.background.color = Color.white;
		}
		#endregion
	}
}
