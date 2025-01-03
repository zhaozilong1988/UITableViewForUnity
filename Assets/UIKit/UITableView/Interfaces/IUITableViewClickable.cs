using UnityEngine.EventSystems;

namespace UIKit
{
	/// <summary>
	/// Implement this if you want to receive events when cell is clicked. 
	/// </summary>
	public interface IUITableViewClickable : IUITableViewInteractable
	{
		/// <summary>
		/// Called when a pointer is pressed on the UITableViewCell at index
		/// </summary>
		void TableViewOnPointerDownCellAt(UITableView tableView, int index, PointerEventData eventData);
		/// <summary>
		/// Called when a pointer is pressed and released on the UITableViewCell at same index
		/// </summary>
		void TableViewOnPointerClickCellAt(UITableView tableView, int index, PointerEventData eventData);
		/// <summary>
		/// Called when a pointer is released (called on the UITableViewCell at index that the pointer is clicking)
		/// </summary>
		void TableViewOnPointerUpCellAt(UITableView tableView, int index, PointerEventData eventData);
	}
}