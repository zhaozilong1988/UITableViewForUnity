namespace UIKit
{
    /// <summary>
    /// Extend this interface when providing a data source for looping table views.
    /// It maps the table view index to your real data index so that the table
    /// view can present cells in a circular manner.
    /// </summary>
    public interface IUITableViewLoopDataSource : IUITableViewDataSource
    {
        /// <summary>
        /// Total number of cells presented by the table view including repeated loops.
        /// </summary>
        int NumberOfLoopCells(UITableView tableView);

        /// <summary>
        /// Map the index requested by the table view to the actual index in your data set.
        /// </summary>
        int MapLoopIndex(UITableView tableView, int loopIndex);
    }
}
