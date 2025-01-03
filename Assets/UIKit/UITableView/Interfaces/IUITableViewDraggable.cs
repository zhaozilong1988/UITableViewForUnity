using UnityEngine.EventSystems;

namespace UIKit
{
	/// <summary>
	/// Implement this if you want to receive events when cell is dragged.
	/// </summary>
	public interface IUITableViewDraggable : IUITableViewInteractable
	{
		/// <summary>
		/// The cell will be dragged to move position if this returns true.
		/// </summary>
		bool TableViewDragCellMovable(UITableView tableView);
		/// <summary>
		/// Called on the drag UITableViewCell at index when dragging is about to begin
		/// </summary>
		void TableViewOnBeginDrag(UITableView tableView, int? draggedIndex, PointerEventData eventData);
		/// <summary>
		/// Called on the drag UITableViewCell at index when a drag is happening
		/// </summary>
		void TableViewOnDrag(UITableView tableView, int? draggedIndex, PointerEventData eventData);
		/// <summary>
		/// Called on the drag UITableViewCell at index when a drag finishes
		/// </summary>
		void TableViewOnEndDrag(UITableView tableView, int? draggedIndex, PointerEventData eventData);
	}
}
