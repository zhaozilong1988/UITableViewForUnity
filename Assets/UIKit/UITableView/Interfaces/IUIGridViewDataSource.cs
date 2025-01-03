namespace UIKit
{
	public interface IUIGridViewDataSource : IUITableViewDataSource
	{
		/// <summary>
		/// Number of column at row in grid (table view).
		/// The return value should be greater than 0.
		/// </summary>
		int NumberOfColumnPerRow(UITableView grid, int rowIndex);

		/// <summary>
		/// Alignment of cells at last row in grid view (table view).
		/// </summary>
		UITableViewAlignment AlignmentOfCellsAtLastRow(UITableView grid);
	}
}
