namespace UIKit
{
	/// <summary>
	/// Implement this method if you need to sort the sibling indices of cells' RectTransform in a table view.
	/// </summary>
	public interface IUITableViewSortable
	{
		/// <summary>
		/// Larger sibling orders correspond to older sibling indexes in the table view’s content.
		/// </summary>
		int SiblingOrderAtIndexInTableView(UITableView tableView, int cellIndex);
	}
}