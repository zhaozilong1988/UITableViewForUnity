using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace UIKit.Samples
{
	public class AdvancedTableScene : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate, IUITableViewMargin
	{
		[SerializeField] UITableView _tableView;
		[SerializeField] AdvancedMonsterCell _monsterCellPrefab;
		[SerializeField] AdvancedTextCell _textCellPrefab;

		readonly List<AdvancedCellData> _dataList = new List<AdvancedCellData>();

		void Start()
		{
			// Prepare for data source
			_dataList.AddRange(CreateSamples(10));

			// Setup table view
			_tableView.dataSource = this;
			_tableView.marginDataSource = this;
			_tableView.@delegate = this;

			_tableView.ReloadData();
		}

		IEnumerable<AdvancedCellData> CreateSamples(int count)
		{
			for (var i = 0; i < count; i++) {
				var data = new AdvancedCellData();
				if (Random.Range(i, count) % 3 == 1) {
					data.cellType = AdvancedCellData.CellType.Text;
					data.scalar = 75f + Random.Range(0f, 100f);
					data.text = "https://www.freepik.com/free-photos-vectors/character";
				}
				else {
					data.cellType = AdvancedCellData.CellType.Monster;
					data.scalar = 200f;
					data.rarity = Random.Range(1, 5);
					data.spriteIndex = Random.Range(0, 4);
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
				_tableView.RearrangeData();
				_tableView.ScrollToCellAt(index, withMargin: false);
			}
		}

		#region IUITableViewDataSource

		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			var data = _dataList[index];
			switch (data.cellType) {
				case AdvancedCellData.CellType.Text:
					return tableView.ReuseOrCreateCell(_textCellPrefab);
				case AdvancedCellData.CellType.Monster:
					return tableView.ReuseOrCreateCell(_monsterCellPrefab);
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
			switch (data.cellType) {
				case AdvancedCellData.CellType.Text:
					var textCell = tableView.GetLoadedCell<AdvancedTextCell>(index);
					textCell.UpdateData(index, data.text);
					lifeCycle = textCell.lifeCycle;
					break;
				case AdvancedCellData.CellType.Monster:
					var imageCell = tableView.GetLoadedCell<AdvancedMonsterCell>(index);
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
			if (data.cellType == AdvancedCellData.CellType.Monster) {
				var imageCell = tableView.GetLoadedCell<AdvancedMonsterCell>(index);
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
