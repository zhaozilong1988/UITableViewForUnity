namespace UIKit
{
	/// <summary>
	/// Implement this if you want to add margins to cell.
	/// </summary>
	public interface IUITableViewMargin
	{
		/// <summary>
		/// Top or right margin for cell at index in tableview.
		/// </summary>
		/// <returns>Length of margin</returns>
		float LengthForUpperMarginInTableView(UITableView tableView, int rowIndex);
		/// <summary>
		/// Bottom or left margin for cell at index in tableview.
		/// </summary>
		/// <returns>Length of margin</returns>
		float LengthForLowerMarginInTableView(UITableView tableView, int rowIndex);
	}
}
