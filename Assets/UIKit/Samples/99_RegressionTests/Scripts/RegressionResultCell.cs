using System;
using UnityEngine;

namespace UIKit.Samples
{
	public sealed class RegressionResultCell : UITableViewCell
	{
		[SerializeField] UnityEngine.UI.Text _title;
		[SerializeField] UnityEngine.UI.Text _status;
		[SerializeField] UnityEngine.UI.Image _background;
		[SerializeField] UnityEngine.UI.Image _stripe;
		[SerializeField] UnityEngine.UI.Button _select;
		public void Bind(string title, RegressionCaseState state, bool selected, bool interactable, Action select)
		{
			_title.text = title;
			_status.text = RegressionDashboard.StateLabel(state) + "  >";
			var color = RegressionDashboard.StateColor(state);
			_status.color = color;
			_stripe.color = color;
			_background.color = selected ? new Color(.16f, .25f, .37f) : new Color(.09f, .13f, .2f);
			_select.interactable = interactable;
			_select.onClick.RemoveAllListeners();
			_select.onClick.AddListener(() => select());
		}
	}
}
