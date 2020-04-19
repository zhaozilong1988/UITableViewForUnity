namespace UIKit
{
	public interface IUITableViewDelegate
	{
		/// <summary>
		/// Will be called if cell at index in table view is appearing to scroll view's viewport.
		/// If isAutoSize of cell is set to TRUE, method will be called after rect transform of cell is resized.
		/// </summary>
		/// <param name="tableView"></param>
		/// <param name="index">Index of cell at</param>
		void CellAtIndexInTableViewWillAppear(UITableView tableView, int index);
		/// <summary>
		/// Will be called if cell at index in table view is disappeared from scroll view's viewport.
		/// </summary>
		/// <param name="tableView"></param>
		/// <param name="index">Index of cell at</param>
		void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index);
	}
}
