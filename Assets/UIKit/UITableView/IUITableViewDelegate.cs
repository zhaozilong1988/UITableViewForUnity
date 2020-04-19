namespace UIKit
{
	public interface IUITableViewDelegate
	{
		void CellAtIndexInTableViewDidAppear(UITableView tableView, int index);
		void CellAtIndexInTableViewWillDisappear(UITableView tableView, int index);
	}
}
