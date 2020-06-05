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
		/// <returns>Scalar of margin</returns>
		float ScalarForUpperMarginInTableView(UITableView tableView, int index);
		/// <summary>
		/// Bottom or left margin for cell at index in tableview.
		/// </summary>
		/// <returns>Scalar of margin</returns>
		float ScalarForLowerMarginInTableView(UITableView tableView, int index);
	}
}
