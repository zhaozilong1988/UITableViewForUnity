namespace UIKit
{
	/// <summary>
	/// Implement this if you want to receive events when the topmost/rightmost or bottommost/leftmost is reached. 
	/// </summary>
	public interface IUITableViewReachable
	{
		/// <summary>
		/// Will be fired once if the table view has reached the topmost pr rightmost.
		/// Will not be fired when the table view is bouncing back.
		/// </summary>
		void TableViewReachedTopmostOrRightmost(UITableView tableView);
		/// <summary>
		/// Will be fired once if the table view has reached the bottommost pr leftmost.
		/// Will not be fired when the table view is bouncing back.
		/// </summary>
		void TableViewReachedBottommostOrLeftmost(UITableView tableView);
		/// <summary>
		/// Will be fired once if the table view has left the topmost or rightmost.
		/// Will not be fired when the table view is stretching.
		/// </summary>
		void TableViewLeftTopmostOrRightmost(UITableView tableView);
		/// <summary>
		/// Will be fired once if the table view has left the bottommost or leftmost.
		/// Will not be fired when the table view is stretching.
		/// </summary>
		void TableViewLeftBottommostOrLeftmost(UITableView tableView);
		/// <summary>
		/// The tolerance for detecting table view's topmost/bottommost or rightmost/leftmost boundary.
		/// Ex. If the tolerance is set to 1.0f, the TableViewReachedTopmostOrRightmost(UITableView) will be fired when the delta distance of content's upper boundary and view port's upper boundary is less than 1f.
		/// The default tolerance is UITableView.DEFAULT_REACHABLE_EDGE_TOLERANCE.
		/// </summary>
		float TableViewReachableEdgeTolerance(UITableView tableView);
	}
}
