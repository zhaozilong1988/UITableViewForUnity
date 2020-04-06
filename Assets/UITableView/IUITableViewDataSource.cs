namespace UITableViewForUnity
{
	public interface IUITableViewDataSource
	{
		UITableViewCell CellAtIndexInTableView(UITableView tableView, int index);
		int NumberOfCellsInTableView(UITableView tableView);
		float LengthForCellInTableView(UITableView tableView, int index);
	}
}
