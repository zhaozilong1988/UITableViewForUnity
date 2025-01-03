using UnityEngine;

namespace UIKit
{
	/// <summary>
	/// Implement this if you want a magnetic alignment when scrolling stops.
	/// </summary>
	public interface IUITableViewMagnetic
	{
		/// <summary>
		/// The calibration point for where the magnetic alignment will be aimed.
		/// </summary>
		/// <returns>Calibration point(Vector.zero: for top or rightmost ~ Vector.one: for bottom or leftmost)</returns>
		Vector2 MagneticCalibrationPointInTableView(UITableView tableView);
		/// <summary>
		/// Notify when magnetic alignment state changed.
		/// </summary>
		/// <param name="tableView">tableView</param>
		/// <param name="ofCellIndex">Index of cell which is aimed by magnetic alignment</param>
		/// <param name="state">UITableViewMagneticState</param>
		void MagneticStateDidChangeInTableView(UITableView tableView, int ofCellIndex, UITableViewMagneticState state);
	}
}