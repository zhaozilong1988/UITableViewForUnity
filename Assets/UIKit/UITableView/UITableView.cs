using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UIKit
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(ScrollRect))]
	public class UITableView : UIBehaviour
	{
		private struct Range
		{
			public int from { get; }
			public int to { get; }
			public Range(int from, int to)
			{
				this.from = from;
				this.to = to;
			}
		}
		private class UITableViewCellHolder
		{
			public UITableViewCell loadedCell { get; set; }
			/// <summary> Height or width of the cell. </summary>
			public float scalar { get; set; }
			/// <summary> The position relative to scroll view's content without considering anchor. </summary>
			public float position { get; set; }
		}
		private enum Direction
		{
			/// <summary> Index of cell at the top is zero. </summary>
			TopToBottom = 0,
			/// <summary> Index of cell at the rightmost is zero. </summary>
			RightToLeft = 1,
		}

		public IUITableViewDataSource dataSource { get; set; }
		public IUITableViewDelegate @delegate { get; set; }

		private readonly List<UITableViewCellHolder> _holders = new List<UITableViewCellHolder>(); // all holders
		/// <summary> Appearing cells and those whose UITableViewLifeCycle is set to RecycleWhenReloaded.</summary>
		private readonly Dictionary<int, UITableViewCellHolder> _loadedHolders = new Dictionary<int, UITableViewCellHolder>();
		/// <summary> Helper for modifying dictionary of _loadedHolders.</summary>
		private readonly List<int> _swapper = new List<int>();
		private ScrollRect _scrollRect;
		private RectTransform _scrollRectTransform;
		private Coroutine _autoScroll;
		private readonly Dictionary<string, Queue<UITableViewCell>> _reusableCellQueues = new Dictionary<string, Queue<UITableViewCell>>();
		private Transform _cellsPoolTransform;
		private bool _isReloaded;
		private Vector2 _normalizedPositionWhenReloaded;

		[SerializeField]
		private Direction _direction = Direction.TopToBottom;
		/// <summary> Tag for distinguishing table view. </summary>
		[SerializeField]
		public int tag; 

		protected override void Awake()
		{
			InitializeScrollRect();
			InitializeCellsPool();
		}

		protected void Update()
		{
			// Read the WORKAROUND which is written in summary of Reload().
			if (_isReloaded)
			{
				if (_scrollRect.normalizedPosition == _normalizedPositionWhenReloaded)
					return;
				_isReloaded = false;
				OnScrollPositionChanged(_scrollRect.normalizedPosition);
			}
		}

		private void InitializeScrollRect()
		{
			if (_scrollRect != null)
				return;

			_scrollRect = GetComponent<ScrollRect>();
			_scrollRect.onValueChanged.AddListener(OnScrollPositionChanged);
			_scrollRectTransform = (RectTransform)_scrollRect.transform;
		}

		private void InitializeCellsPool()
		{
			if (_cellsPoolTransform != null) 
				return;

			var poolObject = new GameObject("ReusableCells");
			_cellsPoolTransform = poolObject.transform;
			_cellsPoolTransform.SetParent(_scrollRect.transform);
		}

		private Range RecalculateVisibleRange(Vector2 normalizedPosition)
		{
			var contentSize = _scrollRect.content.sizeDelta; 
			var viewportSize = _scrollRectTransform.sizeDelta;
			var startPosition = (Vector2.one - normalizedPosition) * (contentSize - viewportSize);
			var endPosition = startPosition + viewportSize;
			int startIndex, endIndex;
			switch (_direction)
			{
				case Direction.TopToBottom:
					startIndex = FindIndexOfCellAtPosition(startPosition.y);
					endIndex = FindIndexOfCellAtPosition(endPosition.y);
					break;
				case Direction.RightToLeft:
					startIndex = FindIndexOfCellAtPosition(startPosition.x);
					endIndex = FindIndexOfCellAtPosition(endPosition.x);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return new Range(startIndex, endIndex);
		}

		private int FindIndexOfCellAtPosition(float position)
		{
			return FindIndexOfCellAtPosition(position, 0, _holders.Count);
		}

		private int FindIndexOfCellAtPosition(float position, int startIndex, int length)
		{
			while (startIndex < length)
			{
				var midIndex = (startIndex + length) / 2;
				if (_holders[midIndex].position > position)
				{
					length = midIndex;
					continue;
				}
				startIndex = midIndex + 1;
			}
			return Math.Max(0, startIndex - 1);
		}

		private void ResizeContent(int numberOfCells)
		{
			var cumulativeScalar = 0f;
			for (var i = 0; i < numberOfCells; i++)
			{
				var holder = _holders[i];
				holder.position = cumulativeScalar;
				holder.scalar = dataSource.ScalarForCellInTableView(this, i);
				cumulativeScalar += holder.scalar;
			}

			var size = _scrollRect.content.sizeDelta;
			switch (_direction)
			{
				case Direction.TopToBottom:
					size.y = cumulativeScalar;
					break;
				case Direction.RightToLeft:
					size.x = cumulativeScalar;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			_scrollRect.content.sizeDelta = size;
		}

		private void OnScrollPositionChanged(Vector2 normalizedPosition)
		{
			ReloadCells(normalizedPosition, false);
		}

		private void ReloadCells(Vector2 normalizedPosition, bool alwaysRearrangeCell)
		{
			var range = RecalculateVisibleRange(normalizedPosition);
			UnloadUnusedCells(range); // recycle invisible cells except life cycle is RecycleWhenReload
			LoadCells(range, alwaysRearrangeCell); // reuse or create visible cells
#if UNITY_EDITOR
			_scrollRect.content.name = $"Content({range.from}~{range.to})";
#endif
		}

		private void LoadCells(Range range, bool alwaysRearrangeCell)
		{
			foreach (var kvp in _loadedHolders)
			{
				if (kvp.Key >= range.from && kvp.Key <= range.to)
					continue;

				RearrangeCell(kvp.Key);
			}

			for (var i = range.from; i <= range.to; i++)
			{
				_loadedHolders[i] = _holders[i];
				LoadCell(i, alwaysRearrangeCell);
			}
		}

		private void LoadCell(int index, bool alwaysRearrangeCell)
		{
			var holder = _holders[index];
			if (holder.loadedCell != null)
			{
				if (alwaysRearrangeCell)
					RearrangeCell(index);

				return;
			}
			holder.loadedCell = dataSource.CellAtIndexInTableView(this, index);
			holder.loadedCell.rectTransform.SetParent(_scrollRect.content);
			holder.loadedCell.gameObject.SetActive(true);
			RearrangeCell(index);
			@delegate?.CellAtIndexInTableViewWillAppear(this, index);
#if UNITY_EDITOR
			_cellsPoolTransform.name = $"ReusableCells({_cellsPoolTransform.childCount})";
			holder.loadedCell.gameObject.name = $"{index}_{holder.loadedCell.reuseIdentifier}";
#endif
		}

		private void UnloadUnusedCells(Range visibleRange)
		{
			foreach (var kvp in _loadedHolders)
			{
				if (kvp.Key >= visibleRange.from && kvp.Key <= visibleRange.to)
					continue;

				if (kvp.Value.loadedCell.lifeCycle == UITableViewCellLifeCycle.RecycleWhenReloaded)
					continue;

				UnloadCell(kvp.Key);
				_swapper.Add(kvp.Key);
			}

			foreach (var key in _swapper)
				_loadedHolders.Remove(key);

			_swapper.Clear();
		}

		private void UnloadAllCells()
		{
			foreach (var kvp in _loadedHolders)
			{
				UnloadCell(kvp.Key);
				_swapper.Add(kvp.Key);
			}

			foreach (var key in _swapper)
				_loadedHolders.Remove(key);

			_swapper.Clear();
		}

		private void UnloadCell(int index)
		{
			var holder = _holders[index];
			var cell = holder.loadedCell;
			Debug.Assert(cell != null, nameof(cell) + " != null");
			holder.loadedCell = null;
			@delegate?.CellAtIndexInTableViewDidDisappear(this, index);
			switch (cell.lifeCycle)
			{
				case UITableViewCellLifeCycle.RecycleWhenDisappeared:
				case UITableViewCellLifeCycle.RecycleWhenReloaded:
					var isExist = _reusableCellQueues.TryGetValue(cell.reuseIdentifier, out var cellsQueue);
					if (!isExist)
						throw new Exception("Queue is not existing."); 

					cellsQueue.Enqueue(cell); // enqueue if recyclable
					cell.transform.SetParent(_cellsPoolTransform);
					cell.gameObject.SetActive(false);
#if UNITY_EDITOR
					_cellsPoolTransform.name = $"ReusableCells({_cellsPoolTransform.childCount})";
					cell.gameObject.name = cell.reuseIdentifier;
#endif
					break;
				case UITableViewCellLifeCycle.DestroyWhenDisappeared:
					Destroy(cell.gameObject); // destroy if non-reusable
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void RearrangeCell(int index)
		{
			var holder = _holders[index];
			var cellRectTransform = holder.loadedCell.rectTransform;
			Vector2 anchoredPosition, sizeDelta = cellRectTransform.sizeDelta;
			switch (_direction)
			{
				case Direction.TopToBottom:
					anchoredPosition = new Vector2(0f, _scrollRect.content.sizeDelta.y * cellRectTransform.anchorMax.y - holder.position - (1f - cellRectTransform.pivot.y) * holder.scalar);
					sizeDelta.y = holder.scalar;
					break;
				case Direction.RightToLeft:
					anchoredPosition = new Vector2(_scrollRect.content.sizeDelta.x * cellRectTransform.anchorMax.x - holder.position - (1f - cellRectTransform.pivot.x) * holder.scalar, 0f);
					sizeDelta.x = holder.scalar;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			holder.loadedCell.rectTransform.anchoredPosition = anchoredPosition;
			if (holder.loadedCell.isAutoResize)
				holder.loadedCell.rectTransform.sizeDelta = sizeDelta;
		}

		private void ReloadDataInternal(int? startIndex)
		{
			if (dataSource == null)
				throw new Exception("DataSource can not be null!");
			if (startIndex < 0)
				throw new IndexOutOfRangeException("Start index must be more than zero.");

			UnloadAllCells();

			var oldCount = _holders.Count;
			var newCount = dataSource.NumberOfCellsInTableView(this);
			if (startIndex > newCount - 1)
				throw new IndexOutOfRangeException("Start index must be less than quantity of cell.");

			var deltaCount = Mathf.Abs(oldCount - newCount);
			for (var i = 0; i < deltaCount; i++)
			{
				if (oldCount > newCount)
					_holders.RemoveAt(0);
				else if (oldCount < newCount)
					_holders.Add(new UITableViewCellHolder());
			}

			ResizeContent(newCount);

			if (startIndex.HasValue)
			{
				ScrollToCellAtIndex(startIndex.Value);
			}
			else
			{
				_isReloaded = true;
				_normalizedPositionWhenReloaded = _scrollRect.normalizedPosition;
				OnScrollPositionChanged(_normalizedPositionWhenReloaded);
			}
		}

		private void StopAutoScroll(Action onScrollingFinished)
		{
			if (_autoScroll == null)
				return;

			StopCoroutine(_autoScroll);
			_autoScroll = null;
			onScrollingFinished?.Invoke();
		}

		private void StartAutoScroll(int index, float time, Action onScrollingFinished)
		{
			StopAutoScroll(onScrollingFinished);
			_autoScroll = StartCoroutine(AutoScroll(index, time, onScrollingFinished));
		}

		private IEnumerator AutoScroll(int index, float time, Action onScrollingFinished)
		{
			var from = _scrollRect.normalizedPosition;
			var to = GetNormalizedPositionOfCellAtIndex(index);
			var progress = 0f; 
			var startAt = Time.time;
			while (!Mathf.Approximately(progress, 1f))
			{
				yield return null;
				progress = Mathf.Min((Time.time - startAt) / time, 1f);
				var x = Mathf.Lerp(from.x, to.x, progress);
				var y = Mathf.Lerp(from.y, to.y, progress);
				_scrollRect.normalizedPosition = new Vector2(x, y);
			}
			_autoScroll = null;
			onScrollingFinished?.Invoke();
		}

		/// <summary> Resize and reposition cells without recycle or destroy them. </summary>
		public void RearrangeData()
		{
			if (dataSource == null)
				throw new Exception("DataSource can not be null!");

			var oldCount = _holders.Count;
			var newCount = dataSource.NumberOfCellsInTableView(this);
			if (oldCount != newCount)
				throw new Exception("Rearrange can not be called if count is changed");

			ResizeContent(newCount);
			ReloadCells(_scrollRect.normalizedPosition, true);
		}

		/// <summary> Recycle or destroy all loaded cells then reload them again. </summary>
		/// <param name="startIndex">Table view will be scrolled to start index after data reloaded.</param>
		public void ReloadData(int startIndex)
		{
			ReloadDataInternal(startIndex);
		}

		/// <summary>
		/// Recycle or destroy all loaded cells then reload them again with current normalized position.
		/// WORKAROUND:
		/// The normalized position may be set incorrectly at first frame after resizing scroll view's content.
		/// So, once Reload() is called, the cells may be not loaded correctly at same frame.
		/// Even though the cells will be correctly reloaded after second frame.
		/// It's recommended to call Reload(int startIndex) instead of Reload().
		/// Tested in Unity 2018.4.8f1.
		/// </summary>
		public void ReloadData()
		{
			ReloadDataInternal(null);
		}

		/// <summary> Append cells to table view without reload them. </summary>
		/// <exception cref="Exception">AppendData() can not be called if number of cells is decreased.</exception>
		public void AppendData()
		{
			if (dataSource == null)
				throw new Exception("DataSource can not be null!");

			var oldCount = _holders.Count;
			var newCount = dataSource.NumberOfCellsInTableView(this);
			if (oldCount > newCount)
				throw new Exception("AppendData() can not be called if number of cells is decreased");

			for (var i = 0; i < newCount - oldCount; i++)
				_holders.Add(new UITableViewCellHolder());

			var content = _scrollRect.content;
			var oldContentSize = content.sizeDelta;
			var oldAnchoredPosition = content.anchoredPosition;
			ResizeContent(newCount);
			content.anchoredPosition = oldAnchoredPosition - (content.sizeDelta - oldContentSize) * (Vector2.one - content.pivot);
			ReloadCells(_scrollRect.normalizedPosition, true);
		}

		/// <summary> Prepend cells to table view without reload them. </summary>
		/// <exception cref="Exception">PrependData() can not be called if number of cells is decreased.</exception>
		public void PrependData()
		{
			if (dataSource == null)
				throw new Exception("DataSource can not be null!");

			var oldCount = _holders.Count;
			var newCount = dataSource.NumberOfCellsInTableView(this);
			var deltaCount = newCount - oldCount;
			if (deltaCount < 0)
				throw new Exception("PrependData() can not be called if number of cells is decreased.");

			for (var i = 0; i < deltaCount; i++)
				_holders.Insert(0, new UITableViewCellHolder());

			_swapper.AddRange(_loadedHolders.Keys);
			_swapper.Sort(); // ex. 1,3,5,8
			_swapper.Reverse(); // ex. 8,5,3,1
			foreach (var key in _swapper)
			{
				_loadedHolders[key + deltaCount] = _loadedHolders[key];
				_loadedHolders.Remove(key);
			}
			_swapper.Clear();

			var content = _scrollRect.content;
			var oldContentSize = content.sizeDelta;
			var oldAnchoredPosition = content.anchoredPosition;
			ResizeContent(newCount);
			content.anchoredPosition = oldAnchoredPosition + (content.sizeDelta - oldContentSize) * content.pivot;
			ReloadCells(_scrollRect.normalizedPosition, true);
		}

		/// <summary> Get a cell from reuse pool or instantiate a new one. </summary>
		/// <param name="prefab">A prefab which one inherited from UITableView.</param>
		/// <param name="lifeCycle">How the cell will be when it disappeared from scroll view's viewport or data is reloaded.</param>
		/// <param name="isAutoResize">The cell will be resized when it appearing into scroll view's viewport if isAutoResize is true, or not if false.</param>
		/// <typeparam name="T">Type of cell</typeparam>
		/// <returns>Subclass of UITableViewCell</returns>
		public T ReuseOrCreateCell<T>(T prefab, UITableViewCellLifeCycle lifeCycle = UITableViewCellLifeCycle.RecycleWhenDisappeared, bool isAutoResize = true) where T : UITableViewCell
		{
			T cell;
			var reuseIdentifier = prefab.GetType().ToString();
			if (lifeCycle != UITableViewCellLifeCycle.DestroyWhenDisappeared)
			{
				var isExist = _reusableCellQueues.TryGetValue(reuseIdentifier, out var cellsQueue);
				if (!isExist)
				{
					cellsQueue = new Queue<UITableViewCell>();
					_reusableCellQueues.Add(reuseIdentifier, cellsQueue);
				}
				else if (cellsQueue.Count > 0)
				{
					cell = cellsQueue.Dequeue() as T;
					Debug.Assert(cell != null, nameof(cell) + " != null");
					return cell;
				}
			}
			cell = Instantiate(prefab);
			cell.reuseIdentifier = reuseIdentifier;
			cell.isAutoResize = isAutoResize;
			cell.lifeCycle = lifeCycle;
			return cell;
		}

		/// <summary> Scroll to cell at index with animation. </summary>
		/// <param name="index">Index of cell at</param>
		/// <param name="time">Animation time</param>
		/// <param name="onScrollingFinished">Will be called when animation is finished or interrupted.</param>
		/// <exception cref="ArgumentException">Time is negative</exception>
		public void ScrollToCellAtIndex(int index, float time, Action onScrollingFinished)
		{
			if (index > _holders.Count - 1 || index < 0)
				throw new IndexOutOfRangeException("Index must be less than cells' number and more than zero.");

			if (time < 0f)
				throw new ArgumentException("Time must be equal to or more than zero.");

			if (Mathf.Approximately(time, 0f))
				ScrollToCellAtIndex(index);
			else
				StartAutoScroll(index, time, onScrollingFinished);
		}

		/// <summary> Scroll to cell at index. </summary>
		/// <param name="index">Index of cell at</param>
		public void ScrollToCellAtIndex(int index)
		{
			if (index > _holders.Count - 1 || index < 0)
				throw new IndexOutOfRangeException("Index must be less than cells' number and more than zero.");

			_scrollRect.normalizedPosition = GetNormalizedPositionOfCellAtIndex(index);
			OnScrollPositionChanged(_scrollRect.normalizedPosition);
		}

		/// <summary> Return scroll view's normalized position of cell at index. </summary>
		/// <param name="index">Index of cell at</param>
		/// <returns>Normalized position of scroll view</returns>
		public Vector2 GetNormalizedPositionOfCellAtIndex(int index)
		{
			var normalizedPosition = _scrollRect.normalizedPosition;
			switch (_direction)
			{
				case Direction.TopToBottom:
					normalizedPosition.y = 1f - _holders[index].position / (_scrollRect.content.sizeDelta.y - _scrollRectTransform.sizeDelta.y);
					break;
				case Direction.RightToLeft:
					normalizedPosition.x = 1f - _holders[index].position / (_scrollRect.content.sizeDelta.x - _scrollRectTransform.sizeDelta.x);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			var x = Mathf.Clamp(0f, normalizedPosition.x, 1f);
			var y = Mathf.Clamp(0f, normalizedPosition.y, 1f);
			return new Vector2(x, y);
		}

		/// <summary> Return it if the cell at index is appearing or UITableViewCellLifeCycle is set to RecycleWhenReloaded. Null will be returned if not. </summary>
		/// <param name="index">Index of cell at</param>
		/// <typeparam name="T">Type of UITableViewCell</typeparam>
		/// <returns>The loaded cell or null</returns>
		/// <exception cref="IndexOutOfRangeException">Index is out of length of cells</exception>
		/// <exception cref="ArgumentException">Cell at index is not type of T</exception>
		public T GetLoadedCell<T>(int index) where T : UITableViewCell
		{
			if (index < 0 || _holders.Count - 1 < index)
				throw new IndexOutOfRangeException("Index is less than 0 or more than count of cells.");

			if (!_loadedHolders.TryGetValue(index, out var holder))
				return null;

			var cell = holder.loadedCell as T;
			if (cell == null)
				throw new ArgumentException($"Cell at index:{index} is not type of {typeof(T)}");

			return cell;
		}

		/// <summary> Return all appearing cells and those whose UITableViewCellLifeCycle is set to RecycleWhenReloaded. </summary>
		/// <returns>All loaded cells</returns>
		public IEnumerable<UITableViewCell> GetAllLoadedCells()
		{
			foreach (var kvp in _loadedHolders)
			{
				Debug.Assert(kvp.Value.loadedCell != null, nameof(kvp.Value.loadedCell) + " != null");
				yield return kvp.Value.loadedCell;
			}
		}

	}
}
