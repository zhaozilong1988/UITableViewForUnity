using UnityEngine;

namespace UIKit
{
	/// <summary>
	/// Implement this if you want a magnetic alignment when scrolling stops.
	/// </summary>
	public interface IUITableViewMagneticAlignment
	{
		/// <summary>
		/// When the scrolling speed is below this threshold, magnetic alignment will be triggered.
		/// </summary>
		/// <returns>Speed:n units/sec</returns>
		float SpeedOfTriggerMagneticAlignmentInTableView(UITableView tableView);
		/// <summary>
		/// The speed used during magnetic alignment completion.
		/// </summary>
		/// <returns>Speed:n units/sec</returns>
		float SpeedOfCompleteMagneticAlignmentInTableView(UITableView tableView);
		/// <summary>
		/// The calibration point for where the magnetic alignment will be aimed.
		/// </summary>
		/// <returns>Calibration point(Vector.zero: for top or rightmost ~ Vector.one: for bottom or leftmost)</returns>
		Vector2 CalibrationPointOfMagneticAlignmentInTableView(UITableView tableView);
		/// <summary>
		/// Notify when magnetic alignment state changed.
		/// </summary>
		/// <param name="tableView">tableView</param>
		/// <param name="ofCellIndex">Index of cell which is aimed by magnetic alignment</param>
		/// <param name="state">UITableViewMagneticState</param>
		void MagneticStateDidChangeInTableView(UITableView tableView, int ofCellIndex, UITableViewMagneticState state);
	}
}