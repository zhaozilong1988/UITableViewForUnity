namespace UIKit
{
	/// <summary>
	/// Implement this if you want receive events when table view is flicked.
	/// </summary>
	public interface IUITableViewFlickable : IUITableViewInteractable
	{
		/// <summary>
		/// The flick will not be triggered if flicked distance outside of this range.
		/// </summary>
		/// <returns>Flick trigger range: units/sec</returns>
		(float lower, float upper) FlickDistanceRangeOfTriggerFlickInTableView(UITableView tableView);
		/// <summary>
		/// The flick will not be triggered if flicked time outside of this range.
		/// </summary>
		/// <returns>Flick trigger time: sec</returns>
		float FlickTimeOfTriggerFlickInTableView(UITableView tableView);
		/// <summary>
		/// The flick is triggered.
		/// </summary>
		/// <param name="tableView">tableView</param>
		/// <param name="indexOfFlickedCell">flicked cell</param>
		/// <param name="direction">flicked direction</param>
		void TableViewOnDidFlick(UITableView tableView, int? indexOfFlickedCell, UITableViewDirection direction);
	}
}
