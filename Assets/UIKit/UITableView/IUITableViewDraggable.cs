using UnityEngine.EventSystems;

namespace UIKit
{
	/// <summary>
	/// Implement this if you want to receive events when cell is dragged.
	/// </summary>
	public interface IUITableViewDraggable : IUITableViewInteractable
	{
		/// <summary>
		/// Called on the drag UITableViewCell at index when dragging is about to begin
		/// </summary>
		bool TableViewOnBeginDragCellAt(UITableView tableView, int draggedIndex, PointerEventData eventData);
		/// <summary>
		/// Called on the drag UITableViewCell at index when a drag is happening
		/// </summary>
		void TableViewOnDragCellAt(UITableView tableView, int draggedIndex, PointerEventData eventData);
		/// <summary>
		/// Called on the drag UITableViewCell at index when a drag finishes
		/// </summary>
		void TableViewOnEndDragCellAt(UITableView tableView, int draggedIndex, PointerEventData eventData);
	}
}
