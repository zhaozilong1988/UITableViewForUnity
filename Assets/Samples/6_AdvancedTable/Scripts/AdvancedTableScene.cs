using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UIKit.Samples
{
	public class AdvancedTableScene : UITableView, IUITableViewDataSource, IUITableViewDelegate, IUITableViewMargin
	{
		[SerializeField] SampleImageCell _imageCellPrefab;
		[SerializeField] SampleTextCell _textCellPrefab;
		[SerializeField] SampleTabCell _tabCellPrefab;

		readonly List<SampleData> _dataList = new List<SampleData>();

		protected override void Awake()
		{
			base.Awake();
			// Prepare for data source
			_dataList.Add(CreateSampleForTab());
			_dataList.AddRange(CreateSamples(10));

			// Setup table view
			dataSource = this;
			marginDataSource = this;
			@delegate = this;
		}

		static SampleData CreateSampleForTab()
		{
			var data = new SampleData {
				sampleType = SampleData.SampleType.Tab,
				scalar = 120f
			};
			return data;
		}

		IEnumerable<SampleData> CreateSamples(int count)
		{
			for (var i = 0; i < count; i++) {
				var data = new SampleData();
				if (Random.Range(i, count) % 3 == 1) {
					data.sampleType = SampleData.SampleType.Text;
					data.scalar = 75f + Random.Range(0f, 100f);
					data.text = "https://www.freepik.com/free-photos-vectors/character";
				}
				else {
					data.sampleType = SampleData.SampleType.Image;
					data.scalar = 200f;
					data.rarity = Random.Range(1, 5);
					data.spriteIndex = Random.Range(0, 8);
					data.scalarBeforeExpand = data.scalar;
					data.scalarAfterExpand = data.scalar + 300;
					data.onExpand = Expand;
				}

				yield return data;
			}
		}

		void Expand(int index)
		{
			StartCoroutine(CoExpandOrClose(index));
		}

		IEnumerator CoExpandOrClose(int index)
		{
			var dataList = _dataList;
			var sampleData = dataList[index];
			var start = Time.time;

			var progress = 0f;
			while (!Mathf.Approximately(progress, 1f)) {
				yield return null;
				progress = Mathf.Min((Time.time - start) / 0.1f, 1f);
				dataList[index].scalar = sampleData.isExpanded
					? Mathf.Lerp(sampleData.scalarBeforeExpand, sampleData.scalarAfterExpand, progress)
					: Mathf.Lerp(sampleData.scalarAfterExpand, sampleData.scalarBeforeExpand, progress);
				RearrangeData();
				ScrollToCellAt(index, withMargin: false);
			}
		}

		#region IUITableViewDataSource

		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			var data = _dataList[index];
			switch (data.sampleType) {
				case SampleData.SampleType.Text:
					return tableView.ReuseOrCreateCell(_textCellPrefab);
				case SampleData.SampleType.Image:
					return tableView.ReuseOrCreateCell(_imageCellPrefab);
				case SampleData.SampleType.Tab:
					return tableView.ReuseOrCreateCell(_tabCellPrefab, UITableViewCellLifeCycle.RecycleWhenReloaded);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public int NumberOfCellsInTableView(UITableView tableView)
		{
			return _dataList.Count;
		}

		public float LengthForCellInTableView(UITableView tableView, int index)
		{
			return _dataList[index].scalar;
		}

		#endregion

		#region IUITableViewDelegate

		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
		{
			UITableViewCellLifeCycle lifeCycle;
			var data = _dataList[index];
			switch (data.sampleType) {
				case SampleData.SampleType.Text:
					var textCell = tableView.GetLoadedCell<SampleTextCell>(index);
					textCell.UpdateData(index, data.text);
					lifeCycle = textCell.lifeCycle;
					break;
				case SampleData.SampleType.Image:
					var imageCell = tableView.GetLoadedCell<SampleImageCell>(index);
					imageCell.UpdateData(index, data);
					lifeCycle = imageCell.lifeCycle;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			Debug.Log($"Cell at index:{index} is appeared. UITableViewLifeCycle is <color=green>{lifeCycle}</color>");
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
		{
			var data = _dataList[index];
			if (data.sampleType == SampleData.SampleType.Image) {
				var imageCell = tableView.GetLoadedCell<SampleImageCell>(index);
				imageCell.ClearUp();
			}

			Debug.Log($"cell at index:{index} will disappear.");
		}

		#endregion

		public float LengthForUpperMarginInTableView(UITableView tableView, int index)
		{
			if (tableView.direction.IsTopToBottomOrRightToLeft())
				return index == 0 ? 100f : 0f;
			return index == _dataList.Count - 1 ? 100f : 0f;
		}

		public float LengthForLowerMarginInTableView(UITableView tableView, int index)
		{
			if (tableView.direction.IsTopToBottomOrRightToLeft()) {
				return index == _dataList.Count - 1 ? 100f : 0f;
			}

			return index == 0 ? 100f : 0f;
		}
	}
}
