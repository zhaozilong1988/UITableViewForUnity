namespace UIKit.Samples
{
	// Exposes the normal Update path so empty-table failures can be tested synchronously.
	public class RegressionTableView : UITableView
	{
		public void Tick() => base.Update();
	}
}
