namespace UIKit
{
	public interface IUIGridViewDataSource : IUITableViewDataSource
	{
		/// <summary>
		/// Number of cells at row or column in grid (table view).
		/// The return value should be greater than 0.
		/// </summary>
		int NumberOfCellsAtRowOrColumnInGrid(UITableView grid);

		UIGridViewAlignment AlignmentOfCellAtRowOrColumnInGrid(UITableView grid);
	}
}
