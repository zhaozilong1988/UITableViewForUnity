namespace UIKit
{
	public interface IUIGridViewDataSource : IUITableViewDataSource
	{
		/// <summary>
		/// Number of column at row in grid (table view).
		/// The return value should be greater than 0.
		/// </summary>
		int NumberOfCellsAtRowInGridView(UITableView gridView, int rowIndex);

		/// <summary>
		/// Alignment of cells at last row in grid view (table view).
		/// </summary>
		UITableViewAlignment AlignmentOfCellsAtRowInGridView(UITableView gridView, int rowIndex);

		/// <summary>
		/// Width for cell at row in grid view (table view).
		/// Use averageWidthAtRow if you want to use the average width of cells at the row.
		/// </summary>
		float WidthOfCellAtRowInGridView(UITableView gridView, int rowIndex, int columnIndex, float averageWidthAtRow);
	}
}
