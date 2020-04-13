using System;
using UITableViewForUnity;
using UnityEngine;
using UnityEngine.UI;

public class SampleTabCell : UITableViewCell
{
	[SerializeField]
	private Text _tab1Text;
	[SerializeField]
	private Text _tab2Text;

	private Action<int> _onTabClicked;

	public void UpdateData(int selectedTabIndex, Action<int> onTabClicked)
	{
		_onTabClicked = onTabClicked;
	}

	public void OnClickTab1Button()
	{
		_onTabClicked.Invoke(0);
	}

	public void OnClickTab2Button()
	{
		_onTabClicked.Invoke(1);
	}
}
