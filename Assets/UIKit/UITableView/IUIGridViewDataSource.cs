namespace UIKit
{
	public interface IUIGridViewDataSource : IUITableViewDataSource
	{
		/// <summary>
		/// Number of cells at row or column in grid (table view).
		/// The return value should be greater than 0.
		/// </summary>
		int NumberOfCellsAtRowOrColumnInGrid(UITableView grid);

		/// <summary>
		/// Alignment of cells at last row or column in grid view (table view).
		/// </summary>
		UITableViewCellAlignment AlignmentOfCellsAtRowOrColumnInGrid(UITableView grid);
	}
}
