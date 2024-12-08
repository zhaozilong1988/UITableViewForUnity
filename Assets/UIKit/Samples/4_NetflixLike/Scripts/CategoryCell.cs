using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class CategoryCell : UITableViewCell, IUITableViewDataSource, IUITableViewDelegate, IUITableViewMargin
	{
		[SerializeField] Text _categoryName;
		[SerializeField] UITableView _nestedTable;
		[SerializeField] NestedCell _cell;
		[SerializeField] Sprite[] _filmPosterSprites;
		[SerializeField] Sprite[] _appIconSprites;

		public Vector2 normalizedPos => _nestedTable.scrollRect.normalizedPosition;
		float _cellLength;
		int _nestedCellCount;
		static readonly string[] _categories = new string[] {
			"Adventure", "Comedy", "Drama", "Fantasy", "Horror", "Mystery", "Romance", "Sci-Fi", "Thriller"
		};

		public void UpdateData(Vector2 normalizedPos)
		{
			var cellIndex = this.index.GetValueOrDefault();
			_categoryName.text = cellIndex == 0 ? "Games" : _categories[cellIndex % _categories.Length];
			_nestedCellCount = cellIndex % 2 == 0 ? 10 : 20;
			_cellLength = cellIndex == 0 ? 200f : 333f;

			// Check useNestedScrollRect of UITableView component in the inspector to enable nested scrolling.
			_nestedTable.dataSource = this;
			_nestedTable.@delegate = this;
			_nestedTable.marginDataSource = this;
			_nestedTable.ReloadData(normalizedPos);
		}

		public void UnloadData()
		{
			_categoryName.text = string.Empty;
			_nestedTable.UnloadData();
		}

		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int cellIndex)
		{
			return tableView.ReuseOrCreateCell(_cell);
		}

		public int NumberOfCellsInTableView(UITableView tableView)
		{
			return _nestedCellCount;
		}

		public float LengthForCellInTableView(UITableView tableView, int nestedCellIndex)
		{
			return _cellLength;
		}

		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int nestedCellIndex)
		{
			var isApp = this.index.GetValueOrDefault() == 0;
			var icon = isApp 
				? _appIconSprites[nestedCellIndex % _appIconSprites.Length] 
				: _filmPosterSprites[(this.index.GetValueOrDefault() + nestedCellIndex) % _filmPosterSprites.Length];
			tableView.GetLoadedCell<NestedCell>(nestedCellIndex).UpdateData(isApp, icon);
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int nestedCellIndex)
		{
			tableView.GetLoadedCell<NestedCell>(nestedCellIndex).UnloadData();
		}

		public float LengthForUpperMarginInTableView(UITableView tableView, int rowIndex)
		{
			return 5f;
		}

		public float LengthForLowerMarginInTableView(UITableView tableView, int rowIndex)
		{
			return 5f;
		}
	}
}
