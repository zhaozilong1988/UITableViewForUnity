namespace UIKit
{
	/// <summary>
	/// Consider implementing this if additional settings for magnetic alignment are required.
	/// </summary>
	public interface IUITableViewMagneticExtra : IUITableViewMagnetic
	{
		/// <summary>
		/// Magnetic alignment will be triggered when the scrolling speed falls below this threshold.
		/// </summary>
		/// <returns>Speed:n units/sec</returns>
		float MagneticWillBeTriggeredWhenScrollingSpeedBelow(UITableView tableView);
		/// <summary>
		/// The speed used during magnetic alignment completion.
		/// </summary>
		/// <returns>Speed:n units/sec</returns>
		float WhenMagneticIsTriggeredScrollingSpeedWillBeChangedTo(UITableView tableView); 
	}
}