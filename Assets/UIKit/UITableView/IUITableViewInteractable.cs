using UnityEngine;

namespace UIKit
{
	public interface IUITableViewInteractable
	{
		/// <summary>
		/// Return a camara used on UITableView, null if on a canvas with render mode of overlay.
		/// </summary>
		Camera TableViewCameraForInteractive(UITableView tableView);
	}
}