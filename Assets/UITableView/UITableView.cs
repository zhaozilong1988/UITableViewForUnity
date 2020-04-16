using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = System.Diagnostics.Debug;

namespace UITableViewForUnity
{
	[RequireComponent(typeof(ScrollRect))]
	public class UITableView : MonoBehaviour
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
			public float scalar { get; set; }
			public float position { get; set; }
		}
		private enum Direction
		{
			Vertical = 0,
			Horizontal = 1,
		}

		public IUITableViewDataSource dataSource { get; set; }
		public IUITableViewDelegate @delegate { get; set; }

		private readonly List<UITableViewCellHolder> _holders = new List<UITableViewCellHolder>();
		private readonly Dictionary<int, UITableViewCellHolder> _loadedHolders = new Dictionary<int, UITableViewCellHolder>();
		private readonly List<int> _swapper = new List<int>(); // swapper for loadedHolder
		private ScrollRect _scrollRect;
		private RectTransform _scrollRectTransform;
		private Coroutine _autoScroll;

		private readonly Dictionary<string, Queue<UITableViewCell>> _reusableCellQueues = new Dictionary<string, Queue<UITableViewCell>>();
		private Transform _cellsPoolTransform;

		[SerializeField]
		private Direction _direction = Direction.Vertical;
		[SerializeField]
		public int tag = 0;

		protected virtual void Awake()
		{
			InitializeScrollRect();
			InitializeCellsPool();
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
				case Direction.Vertical:
					startIndex = FindIndexOfRowAtPosition(startPosition.y);
					endIndex = FindIndexOfRowAtPosition(endPosition.y);
					break;
				case Direction.Horizontal:
					startIndex = FindIndexOfRowAtPosition(startPosition.x);
					endIndex = FindIndexOfRowAtPosition(endPosition.x);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return new Range(startIndex, endIndex);
		}

		private int FindIndexOfRowAtPosition(float position)
		{
			return FindIndexOfRowAtPosition(position, 0, _holders.Count);
		}

		private int FindIndexOfRowAtPosition(float position, int startIndex, int length)
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
				holder.scalar = this.dataSource.ScalarForCellInTableView(this, i);
				cumulativeScalar += holder.scalar;
			}

			var size = _scrollRect.content.sizeDelta;
			switch (_direction)
			{
				case Direction.Vertical:
					size.y = cumulativeScalar;
					break;
				case Direction.Horizontal:
					size.x = cumulativeScalar;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			_scrollRect.content.sizeDelta = size;
		}

		private void RecycleOrDestroyCell(UITableViewCell cell, int index)
		{
			if (cell == null)
				return;

			if (cell.isReusable)
			{
				// enqueue
				var isExist = _reusableCellQueues.TryGetValue(cell.reuseIdentifier, out var cellsQueue);
				if (!isExist) 
					throw new Exception("Queue is not existing."); 

				this.@delegate?.CellAtIndexInTableViewWillDisappear(this, index, true);
				cellsQueue.Enqueue(cell);
				cell.transform.SetParent(_cellsPoolTransform);
				cell.gameObject.SetActive(false);

#if UNITY_EDITOR
				_cellsPoolTransform.name = $"ReusableCells({_cellsPoolTransform.childCount})";
				cell.gameObject.name = cell.reuseIdentifier;
#endif
			}
			else
			{
				this.@delegate?.CellAtIndexInTableViewWillDisappear(this, index, false);
				// destroy cell if non-reusable
				Destroy(cell.gameObject);
			}
		}

		private void RecycleOrDestroyCells(Range outOfRange)
		{
			foreach (var kvp in _loadedHolders)
			{
				if (kvp.Key >= outOfRange.from && kvp.Key <= outOfRange.to)
					continue;

				RecycleOrDestroyCell(kvp.Value.loadedCell, kvp.Key);
				kvp.Value.loadedCell = null;
				_swapper.Add(kvp.Key);
			}

			foreach (var key in _swapper)
			{
				_loadedHolders.Remove(key);
			}
			_swapper.Clear();
		}

		private void OnScrollPositionChanged(Vector2 normalizedPosition)
		{
			ReloadVisibleCells(normalizedPosition, false);
		}

		private void ReloadVisibleCells(bool alwaysRearrangeCell)
		{
			ReloadVisibleCells(_scrollRect.normalizedPosition, alwaysRearrangeCell);
		}

		private void ReloadVisibleCells(Vector2 normalizedPosition, bool alwaysRearrangeCell)
		{
			var visibleRange = RecalculateVisibleRange(normalizedPosition);
			ReloadVisibleCells(visibleRange, alwaysRearrangeCell);
		}

		private void ReloadVisibleCells(Range range, bool alwaysRearrangeCell)
		{
			// recycle invisible cells
			RecycleOrDestroyCells(range);

			// reuse or create visible cells
			for (var i = range.from; i <= range.to; i++)
			{
				ReuseOrCreateCell(i, alwaysRearrangeCell);
				_loadedHolders[i] = _holders[i];
			}

#if UNITY_EDITOR
			_scrollRect.content.name = $"Content({range.from}~{range.to})";
#endif
		}

		private void ReuseOrCreateCell(int index, bool alwaysRearrangeCell)
		{
			if (index > _holders.Count - 1 || index < 0)
				throw new IndexOutOfRangeException("Index must be less than cells' count and more than zero.");

			var holder = _holders[index];
			var isReusedOrCreatedCell = false;
			if (holder.loadedCell == null)
			{
				holder.loadedCell = this.dataSource.CellAtIndexInTableView(this, index);
				holder.loadedCell.rectTransform.SetParent(_scrollRect.content);
				isReusedOrCreatedCell = true;
			}

			var cell = holder.loadedCell;
			if (isReusedOrCreatedCell || alwaysRearrangeCell)
			{
				var cellRectTransform = cell.rectTransform;
				Vector2 dstAnchoredPosition, dstSizeDelta = cellRectTransform.sizeDelta;
				switch (_direction)
				{
					case Direction.Vertical:
						dstAnchoredPosition = new Vector2(0f, _scrollRect.content.sizeDelta.y * cellRectTransform.anchorMax.y - holder.position - (1f - cellRectTransform.pivot.y) * holder.scalar);
						dstSizeDelta.y = holder.scalar;
						break;
					case Direction.Horizontal:
						dstAnchoredPosition = new Vector2(_scrollRect.content.sizeDelta.x * cellRectTransform.anchorMax.x - holder.position - (1f - cellRectTransform.pivot.x) * holder.scalar, 0f);
						dstSizeDelta.x = holder.scalar;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (cell.isAutoResize)
				{
					cell.rectTransform.sizeDelta = dstSizeDelta;
				}
				cell.rectTransform.anchoredPosition = dstAnchoredPosition;
			}

			if (isReusedOrCreatedCell)
			{
				cell.gameObject.SetActive(true);
				this.@delegate?.CellAtIndexInTableViewDidAppear(this, index, cell.isReused);
#if UNITY_EDITOR
				_cellsPoolTransform.name = $"ReusableCells({_cellsPoolTransform.childCount})";
				cell.gameObject.name = $"{index}_{cell.reuseIdentifier}";
#endif
			}
		}

		/// <summary>
		/// Resize and reposition cells without recycle or destroy them.
		/// </summary>
		public void RearrangeData()
		{
			if (this.dataSource == null)
				throw new Exception("DataSource can not be null!");

			var oldCount = _holders.Count;
			var newCount = this.dataSource.NumberOfCellsInTableView(this);
			if (oldCount != newCount)
				throw new Exception("Rearrange can not be called if count is changed");

			ResizeContent(newCount);
			ReloadVisibleCells(true);
		}

		/// <summary>
		/// Recycle or destroy all appearing cells then reposition them.
		/// </summary>
		/// <exception cref="Exception">When data source is null.</exception>
		public void ReloadData()
		{
			if (this.dataSource == null)
				throw new Exception("DataSource can not be null!");

			RecycleOrDestroyCells(new Range(int.MinValue, int.MinValue));

			var oldCount = _holders.Count;
			var newCount = this.dataSource.NumberOfCellsInTableView(this);
			var deltaCount = Mathf.Abs(oldCount - newCount);
			for (int i = 0; i < deltaCount; i++)
			{
				if (oldCount > newCount)
					_holders.RemoveAt(0);
				else if (oldCount < newCount)
					_holders.Add(new UITableViewCellHolder());
			}

			ResizeContent(newCount);
			ReloadVisibleCells(false);
		}

		/// <summary>
		/// Append cells to table view.
		/// </summary>
		public void AppendData()
		{
			if (this.dataSource == null)
				throw new Exception("DataSource can not be null!");

			var oldCount = _holders.Count;
			var newCount = this.dataSource.NumberOfCellsInTableView(this);
			if (oldCount > newCount)
				throw new Exception("Increase can not be called if count is decreased");

			for (var i = 0; i < newCount - oldCount; i++)
				_holders.Add(new UITableViewCellHolder());

			var oldAnchoredPosition = _scrollRect.content.anchoredPosition;
			ResizeContent(newCount);
			_scrollRect.content.anchoredPosition = oldAnchoredPosition;
			ReloadVisibleCells(true);
		}

		/// <summary>
		/// Prepend cells to table view
		/// </summary>
		public void PrependData()
		{
			if (this.dataSource == null)
				throw new Exception("DataSource can not be null!");

			var oldCount = _holders.Count;
			var newCount = this.dataSource.NumberOfCellsInTableView(this);
			if (oldCount > newCount)
				throw new Exception("Increase can not be called if count is decreased");

			for (var i = 0; i < newCount - oldCount; i++)
				_holders.Insert(0, new UITableViewCellHolder());

			var content = _scrollRect.content;
			var oldContentSize = content.sizeDelta;
			var oldAnchoredPosition = content.anchoredPosition;
			ResizeContent(newCount);
			_scrollRect.content.anchoredPosition = oldAnchoredPosition + content.sizeDelta - oldContentSize;
			ReloadVisibleCells(true);
		}

		/// <summary>
		/// Dequeue a caching cell with reuse identifier, or instantiate a new one.
		/// </summary>
		/// <param name="cellPrefab">Cell's prefab that inherit from UITableView</param>
		/// <param name="reuseIdentifier">The cell will be put into reuse queue if reuse identifier is not null, or will be destroyed when cell is disappeared.</param>
		/// <param name="isAutoResize">The cell will be expanded when appearing if isAutoResize is true, or not if false.</param>
		/// <typeparam name="T">Type of cell</typeparam>
		/// <returns></returns>
		public T DequeueOrCreateCell<T>(T cellPrefab, string reuseIdentifier, bool isAutoResize) where T : UITableViewCell
		{
			T cell;
			var isReusable = !string.IsNullOrEmpty(reuseIdentifier);
			if (isReusable)
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
					cell.isReused = true;
					return cell;
				}
			}

			cell = Instantiate(cellPrefab);
			cell.reuseIdentifier = reuseIdentifier;
			cell.isAutoResize = isAutoResize;
			cell.isReused = false;

			return cell;
		}

		/// <summary>
		/// Move to cell at index
		/// </summary>
		/// <param name="index">Index of cell at</param>
		/// <param name="time">Time of scrolling to</param>
		/// <exception cref="IndexOutOfRangeException"></exception>
		public void MoveToCellAtIndex(int index, float time)
		{
			if (index > _holders.Count - 1 || index < 0)
				throw new IndexOutOfRangeException("Index must be less than cells' count and more than zero.");

			if (time < 0f)
				throw new ArgumentException("Time must be equal to or more than zero.");

			if (Mathf.Approximately(time, 0f))
				_scrollRect.normalizedPosition = GetNormalizedPositionOfCellAtIndex(index);
			else
				StartAutoScroll(index, time);
		}

		/// <summary>
		/// Return scroll view's normalized position of cell at index.
		/// </summary>
		/// <param name="index">Index of cell at</param>
		/// <returns>Normalized position of scroll view</returns>
		public Vector2 GetNormalizedPositionOfCellAtIndex(int index)
		{
			var normalizedPosition = _scrollRect.normalizedPosition;
			switch (_direction)
			{
				case Direction.Vertical:
					normalizedPosition.y = 1f - _holders[index].position / (_scrollRect.content.sizeDelta.y - _scrollRectTransform.sizeDelta.y);
					break;
				case Direction.Horizontal:
					normalizedPosition.x = 1f - _holders[index].position / (_scrollRect.content.sizeDelta.x - _scrollRectTransform.sizeDelta.x);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var x = Mathf.Clamp(0f, normalizedPosition.x, 1f);
			var y = Mathf.Clamp(0f, normalizedPosition.y, 1f);
			return new Vector2(x, y);
		}
		
		private void StopAutoScroll()
		{
			if (_autoScroll == null)
				return;

			StopCoroutine(_autoScroll);
			_autoScroll = null;
		}

		private void StartAutoScroll(int index, float time)
		{
			StopAutoScroll();
			_autoScroll = StartCoroutine(AutoScroll(index, time));
		}

		private IEnumerator AutoScroll(int index, float time)
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
		}

		/// <summary>
		/// Return cell at index if it's type is T, or return null.
		/// </summary>
		/// <param name="index">Index of cell</param>
		/// <typeparam name="T">Type of UITableViewCell</typeparam>
		/// <returns>The appearing cell or null</returns>
		/// <exception cref="IndexOutOfRangeException">Index is out of length of cells</exception>
		/// <exception cref="ArgumentException">Cell at index is not type of T</exception>
		public T GetAppearingCell<T>(int index) where T : UITableViewCell
		{
			if (index < 0 || _holders.Count - 1 < index)
				throw new IndexOutOfRangeException("Index is less than 0 or more than count of cells.");

			var tCell = _holders[index].loadedCell as T;
			if (tCell == null)
				throw new ArgumentException($"Cell at index:{index} is not type of {typeof(T)}");

			return tCell;
		}

		/// <summary>
		/// Return all appearing cells.
		/// </summary>
		/// <returns>All appearing cells</returns>
		public IEnumerable<UITableViewCell> GetAllAppearingCells()
		{
			foreach (var holder in _holders)
			{
				if (holder.loadedCell != null)
					yield return holder.loadedCell;
			}
		}

	}
}
