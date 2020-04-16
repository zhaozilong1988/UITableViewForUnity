namespace UITableViewForUnity
{
	public interface IUITableViewDelegate
	{
		void CellAtIndexInTableViewDidAppear(UITableView tableView, int index, bool isReused);
		void CellAtIndexInTableViewWillDisappear(UITableView tableView, int index, bool willBeRecycled);
	}
}
