namespace UIKit
{
	public interface IUITableViewDataSource
	{
		/// <summary>
		/// Will be called if cell at index in table view is appearing to scroll view's viewport,
		/// before calling IUITableViewDelegate.CellAtIndexInTableViewWillAppear(UITableView, int).
		/// Use UITableView.ReuseOrCreateCell(T, UITableViewCellLifeCycle, bool) to obtain a cell.
		/// </summary>
		/// <param name="tableView"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		UITableViewCell CellAtIndexInTableView(UITableView tableView, int index);
		/// <summary>
		/// Will be called once after Reload(), Reload(int), RearrangeData(), AppendData() or PrependData() is called, for getting number of cells in table view.
		/// </summary>
		/// <param name="tableView"></param>
		/// <returns></returns>
		int NumberOfCellsInTableView(UITableView tableView);
		/// <summary>
		/// Will be called once after Reload(), Reload(int), RearrangeData(), AppendData() or PrependData() is called, for getting height or width of cells in table view.
		/// </summary>
		/// <param name="tableView"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		float LengthForCellInTableView(UITableView tableView, int index);
	}
}
