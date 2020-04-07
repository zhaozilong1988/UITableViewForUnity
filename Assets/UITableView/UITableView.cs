using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace UITableViewForUnity
{
	[RequireComponent(typeof(ScrollRect))]
	public class UITableView : MonoBehaviour
	{
		private class UITableViewCellHolder
		{
			public float deltaPosition { get; set; }
			public float length { get; set; }
			public UITableViewCell cell { get; set; }
		}

		private enum Direction
		{
			Vertical = 0,
			Horizontal = 1,
		}

		public IUITableViewDataSource dataSource { get; set; }
		public IUITableViewLifecycle lifecycle { get; set; }

		private readonly List<UITableViewCellHolder> _cellHolders = new List<UITableViewCellHolder>();
		private ScrollRect _scrollRect;
		private RectTransform _scrollRectTransform;
		
		private readonly Dictionary<string, Queue<UITableViewCell>> _reusableCellsPool = new Dictionary<string, Queue<UITableViewCell>>();
		private Transform _cellsPoolTransform;
		private int _visibleStartIndex = int.MinValue;
		private int _visibleEndIndex = int.MinValue;

		[SerializeField]
		private Direction _direction = Direction.Vertical;

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

			GameObject poolObject = new GameObject("ReusableCells");
			_cellsPoolTransform = poolObject.transform;
			_cellsPoolTransform.SetParent(_scrollRect.transform);
		}

		private void OnScrollPositionChanged(Vector2 scrollPosition)
		{
			float normalizedPosition, contentLength, viewportLength;
			switch (_direction)
			{
				case Direction.Vertical:
					normalizedPosition = scrollPosition.y;
					contentLength = _scrollRect.content.sizeDelta.y;
					viewportLength = _scrollRectTransform.sizeDelta.y;
					break;
				case Direction.Horizontal:
					normalizedPosition = scrollPosition.x;
					contentLength = _scrollRect.content.sizeDelta.x;
					viewportLength = _scrollRectTransform.sizeDelta.x;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			var startPosition = (1f - normalizedPosition) * (contentLength - viewportLength);
			var endPosition = startPosition + viewportLength;
			var startIndex = FindIndexOfRowAtPosition(startPosition);
			var endIndex = FindIndexOfRowAtPosition(endPosition);

			ReloadVisibleCells(startIndex, endIndex);
		}

		private int FindIndexOfRowAtPosition(float position)
		{
			return FindIndexOfRowAtPosition(position, 0, _cellHolders.Count);
		}

		private int FindIndexOfRowAtPosition(float position, int startIndex, int endIndex)
		{
			while (startIndex < endIndex)
			{
				int midIndex = (startIndex + endIndex) / 2;
				if (_cellHolders[midIndex].deltaPosition > position)
				{
					endIndex = midIndex;
					continue;
				}

				startIndex = midIndex + 1;
			}

			return Math.Max(0, startIndex - 1);
		}

		private void ResizeContent(float length)
		{
			var size = _scrollRect.content.sizeDelta;

			switch (_direction)
			{
				case Direction.Vertical:
					size.y = length;
					break;
				case Direction.Horizontal:
					size.x = length;
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
				bool isExist = _reusableCellsPool.TryGetValue(cell.reuseIdentifier, out var cellsQueue);
				if (!isExist) 
				{ 
					throw new Exception("Queue is not existing."); 
				}

				this.lifecycle?.CellAtIndexInTableViewWillDisappear(this, index, true);
				cellsQueue.Enqueue(cell);
				cell.transform.SetParent(_cellsPoolTransform);
				cell.gameObject.SetActive(false);

#if UNITY_EDITOR
				_cellsPoolTransform.name = $"ReusableCells({_cellsPoolTransform.childCount})";
#endif
			}
			else
			{
				this.lifecycle?.CellAtIndexInTableViewWillDisappear(this, index, false);
				// destroy cell if non-reusable
				Destroy(cell.gameObject);
			}
		}

		private void ResetCellHolders()
		{
			int curCount = _cellHolders.Count;
			int newCount = this.dataSource.NumberOfCellsInTableView(this);
			float cumulativeLength = 0f;
			for (int i = 0; i < newCount; i++)
			{
				if (i >= curCount)
				{
					_cellHolders.Add(new UITableViewCellHolder());
				}

				var holder = _cellHolders[i];
				RecycleOrDestroyCell(holder.cell, i);
				holder.cell = null;
				holder.deltaPosition = cumulativeLength;
				holder.length = this.dataSource.LengthForCellInTableView(this, i);
				cumulativeLength += holder.length;
			}

			for (int i = curCount; i >= newCount; i--)
			{
				var holder = _cellHolders[i];
				RecycleOrDestroyCell(holder.cell, i);
				holder.cell = null;

				_cellHolders.RemoveAt(i);
			}

			ResizeContent(cumulativeLength);
		}

		private void ReloadVisibleCells(int startIndex, int endIndex)
		{
			// recycle invisible cells
			if (_visibleStartIndex >= 0
			    && _visibleEndIndex >= 0
			    && _visibleStartIndex <= _visibleEndIndex)
			{
				for (var i = _visibleStartIndex; i <= _visibleEndIndex; i++)
				{
					if (i >= startIndex && i <= endIndex)
						continue;

					var holder = _cellHolders[i];
					RecycleOrDestroyCell(holder.cell, i);
					holder.cell = null;
				}
			}

			// reuse or create visible cells
			for (var i = startIndex; i <= endIndex; i++)
			{
				ReuseOrCreateCell(i);
			}

			_visibleStartIndex = startIndex;
			_visibleEndIndex = endIndex;

			Debug.Log($"index: {startIndex} ~ {endIndex}");
		}

		private void ReuseOrCreateCell(int index)
		{
			var holder = _cellHolders[index];
			if (holder.cell != null)
			{
				return;
			}
			var cell = this.dataSource.CellAtIndexInTableView(this, index);
			cell.transform.SetParent(_scrollRect.content);
			Vector2 position, sieDelta = cell.rectTransform.sizeDelta;
			switch (_direction)
			{
				case Direction.Vertical:
					position = new Vector2(0f, _scrollRect.content.sizeDelta.y * cell.rectTransform.anchorMax.y - holder.deltaPosition - (1f - cell.rectTransform.pivot.y) * holder.length);
					sieDelta.y = holder.length;
					break;
				case Direction.Horizontal:
					position = new Vector2(_scrollRect.content.sizeDelta.x * cell.rectTransform.anchorMax.x - holder.deltaPosition - (1f - cell.rectTransform.pivot.x) * holder.length, 0f);
					sieDelta.x = holder.length;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (cell.isAutoResize)
			{
				cell.rectTransform.sizeDelta = sieDelta;
			}
			cell.rectTransform.anchoredPosition = position;
			cell.gameObject.SetActive(true);
			holder.cell = cell;
			this.lifecycle?.CellAtIndexInTableViewDidAppear(this, index, cell.isReused);
#if UNITY_EDITOR
			_cellsPoolTransform.name = $"ReusableCells({_cellsPoolTransform.childCount})";
#endif
		}

		public void ReloadData()
		{
			if (this.dataSource == null)
				throw new Exception("DataSource can not be null!");

			ResetCellHolders();
			OnScrollPositionChanged(_scrollRect.normalizedPosition);
		}

		public T DequeueOrCreateCell<T>(T cellPrefab, string reuseIdentifier, bool isAutoResize) where T : UITableViewCell
		{
			T cell;
			var isReusable = !string.IsNullOrEmpty(reuseIdentifier);
			if (isReusable)
			{
				var isExist = _reusableCellsPool.TryGetValue(reuseIdentifier, out var cellsQueue);
				if (!isExist)
				{
					cellsQueue = new Queue<UITableViewCell>();
					_reusableCellsPool.Add(reuseIdentifier, cellsQueue);
				}
				else if (cellsQueue.Count > 0)
				{
					cell = cellsQueue.Dequeue() as T;
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

		public T GetAppearingCell<T>(int index) where T : UITableViewCell
		{
			if (index < 0 || _cellHolders.Count - 1 < index)
				throw new IndexOutOfRangeException("Index is less than 0 or more than count of cells.");

			var cell = _cellHolders[index].cell;
			if (cell == null)
				throw new ArgumentOutOfRangeException($"Cell at index:{index} is not appearing.");

			var tCell = _cellHolders[index].cell as T;
			if (tCell == null)
				throw new ArgumentException($"Cell at index:{index} is not type of {typeof(T)}");

			return tCell;
		}

		public IEnumerable<UITableViewCell> GetAllAppearingCells()
		{
			foreach (var holder in _cellHolders)
			{
				if (holder.cell != null)
					yield return holder.cell;
			}
		}

	}
}
