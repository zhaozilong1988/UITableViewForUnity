using UnityEngine;

namespace UIKit.Samples
{
	public class InfiniteLoopTableScene : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate
	{
		[SerializeField] UITableView _tableView;
		[SerializeField] InfiniteLoopCell _cellPrefab;

		[SerializeField] Sprite[] _iconSprites;
		[SerializeField] Color[] _backgroundColors;
		[SerializeField, Min(1)] int _numberOfCells = 7;
		Vector2 _viewportSize;

		void Start()
		{
			// Tell the table view that this class will provide that the data it needs
			_tableView.dataSource = this;
			// Tell the table view that this class will respond to its delegate methods
			_tableView.@delegate = this;
			_tableView.enableInfiniteLoop = true;

			// Reload the table view to refresh UI
			_tableView.ReloadData();
			_viewportSize = _tableView.scrollRect.viewport.rect.size;
		}

		void LateUpdate()
		{
			// Recalculate cell widths and loop copies when the Game View or device rotates.
			var size = _tableView.scrollRect.viewport.rect.size;
			if (size == _viewportSize || size.x <= 0 || size.y <= 0) return;
			_viewportSize = size;
			_tableView.ReloadData(); // Preserves the current normalized position.
		}

		#region IUITableViewDataSource
		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			return _tableView.ReuseOrCreateCell(_cellPrefab);
		}

		public int NumberOfCellsInTableView(UITableView tableView)
		{
			return _numberOfCells;
		}

		public float LengthForCellInTableView(UITableView tableView, int index)
		{
			return index % 2 == 0 ? 150 : 200;
		}
		#endregion

		#region IUITableViewDelegate
		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
		{
			var cell = tableView.GetLoadedCell<InfiniteLoopCell>(index);
			cell.text.text = $"Loop Cell: {index}\nKeep scrolling";
			cell.text.color = index % _backgroundColors.Length == 0 ? Color.black : Color.white;
			cell.icon.sprite = _iconSprites[index % _iconSprites.Length];
			cell.background.color = _backgroundColors[index % _backgroundColors.Length];
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
		{
			// Every recycled cell is fully rebound in WillAppear.
		}
		#endregion
	}
}
