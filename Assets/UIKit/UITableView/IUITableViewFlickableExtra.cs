namespace UIKit
{
	/// <summary>
	/// Consider implementing this if additional settings are needed.
	/// </summary>
	public interface IUITableViewFlickableExtra : IUITableViewFlickable
	{
		/// <summary>
		/// The flick will be triggered if the flick distance falls within this range.
		/// </summary>
		/// <returns>Flick trigger range: units/sec</returns>
		(float lower, float upper) FlickWillBeTriggeredIfFlickDistanceFallsWithinRange(UITableView tableView);
		/// <summary>
		/// The flick will be triggered if the flick duration is below this time.
		/// </summary>
		/// <returns>Flick trigger time: sec</returns>
		float FlickWillBeTriggeredIfFlickDurationBelowTime(UITableView tableView);
	}
}
