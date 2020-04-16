namespace UITableViewForUnity
{
	public interface IUITableViewDataSource
	{
		UITableViewCell CellAtIndexInTableView(UITableView tableView, int index);
		int NumberOfCellsInTableView(UITableView tableView);
		float ScalarForCellInTableView(UITableView tableView, int index);
	}
}
