namespace UIKit
{
	/// <summary>
	/// Implement this if you want to receive events when table view is flicked.
	/// </summary>
	public interface IUITableViewFlickable : IUITableViewInteractable
	{
		/// <summary>
		/// The flick is triggered.
		/// </summary>
		/// <param name="tableView">tableView</param>
		/// <param name="indexOfFlickedCell">flicked cell</param>
		/// <param name="direction">flicked direction</param>
		void TableViewOnDidFlick(UITableView tableView, int? indexOfFlickedCell, UITableViewDirection direction);
	}
}
