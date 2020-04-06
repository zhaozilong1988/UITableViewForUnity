namespace UITableViewForUnity
{
	public interface IUITableViewLifecycle
	{
		void CellAtIndexInTableViewDidAppear(UITableView tableView, int index, bool isReused);
		void CellAtIndexInTableViewWillDisappear(UITableView tableView, int index, bool willBeRecycled);
	}
}
