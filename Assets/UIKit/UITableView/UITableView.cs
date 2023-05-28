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
		public const float DEFAULT_REACHABLE_EDGE_TOLERANCE = 0.1f;
		public ScrollRect scrollRect => _scrollRect;
		public IUITableViewDataSource dataSource { get; set; }
		public IUITableViewMargin marginDataSource { get; set; }
		public IUITableViewDelegate @delegate { get; set; }
		public IUITableViewReachable reachable { get; set; }

		/// <summary> If TRUE, the UITableViewCellLifeCycle will be ignored and all cells will be loaded at once, or not when FALSE. </summary>
		public bool ignoreCellLifeCycle {
			get => _ignoreCellLifeCycle;
			set {
				if (_ignoreCellLifeCycle == value)
					return;
				_ignoreCellLifeCycle = value;
				ReloadData();
			}
		}
		public UITableViewDirection direction
		{
			get => _direction;
			set {
				if (_direction == value)
					return;
				_direction = value;
				Validate();
				ReloadData();
			}
		}

		private int _numberOfCellsAtRowOrColumn = 1;
		private UITableViewCellAlignment _cellAlignment = UITableViewCellAlignment.RightOrTop;
		private readonly List<UITableViewCellHolder> _holders = new List<UITableViewCellHolder>(); // all holders
		private readonly Dictionary<string, Queue<UITableViewCell>> _reusableCellQueues = new Dictionary<string, Queue<UITableViewCell>>(); // for caching the cells which waiting for be reused.
		private readonly Dictionary<int, UITableViewCellHolder> _loadedHolders = new Dictionary<int, UITableViewCellHolder>(); // appearing cells and those whose UITableViewLifeCycle is set to RecycleWhenReloaded.
		private readonly List<int> _swapper = new List<int>(); // helper for modifying dictionary of _loadedHolders.
		private Transform _cellsPool;
		private Coroutine _autoScroll;
		private bool _isReloaded;
		private Vector2 _normalizedPositionWhenReloaded;
		private bool _isReachingBottommostOrLeftmost; // for detecting boundary when IUITableViewReachable is assigned.
		private bool _isReachingTopmostOrRightmost;
		[SerializeField] private ScrollRect _scrollRect;
		[SerializeField] private RectTransform _viewport;
		[SerializeField] private RectTransform _content;
		[SerializeField] private UITableViewDirection _direction = UITableViewDirection.TopToBottom;
		[SerializeField] private bool _ignoreCellLifeCycle;
		/// <summary> Tag for distinguishing table view. </summary>
		[SerializeField] public int tag;

		protected override void Awake()
		{
			base.Awake();
			InitializeScrollRect();
			InitializeCellsPool();
			Validate();
			_scrollRect.onValueChanged.AddListener(OnScrollPositionChanged);
		}

		protected override void OnDestroy()
		{
			if (_scrollRect != null)
				_scrollRect.onValueChanged.RemoveListener(OnScrollPositionChanged);
			base.OnDestroy();
		}

		protected void Update()
		{
			// Read the WORKAROUND which is written in summary of Reload().
			if (_isReloaded) {
				if (_scrollRect.normalizedPosition == _normalizedPositionWhenReloaded)
					return;
				_isReloaded = false;
				ReloadCells(_scrollRect.normalizedPosition, false);
			}
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			InitializeScrollRect();
		}
#endif

		private void InitializeScrollRect()
		{
			if (_scrollRect == null)
				_scrollRect = GetComponent<ScrollRect>();
			if (_viewport == null)
				_viewport = _scrollRect.viewport;
			if (_content == null)
				_content = _scrollRect.content;
		}

		private void InitializeCellsPool()
		{
			if (_cellsPool != null)
				return;
			var poolObject = new GameObject("ReusableCells");
			_cellsPool = poolObject.transform;
			_cellsPool.SetParent(_scrollRect.transform);
		}

		/// <summary> Validate if the settings are compatible with UITableView. </summary>
		private void Validate()
		{
			var sizeFitter = _content.GetComponent<ContentSizeFitter>();
			if (sizeFitter != null) // ContentSizeFitter which attaching to content can not be set to NON-Unconstrained.
				if (sizeFitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained || sizeFitter.verticalFit != ContentSizeFitter.FitMode.Unconstrained)
					throw new Exception("ContentSizeFitter which attaching to content can not be set to NON-Unconstrained.");
		}

		private Range RecalculateVisibleRange(Vector2 normalizedPosition)
		{
			var contentSize = _content.rect.size;
			var viewportSize = _ignoreCellLifeCycle ? contentSize : _viewport.rect.size;
			var startPosition = (Vector2.one - normalizedPosition) * (contentSize - viewportSize);
			var endPosition = startPosition + viewportSize;
			int startIndex, endIndex;
			switch (_direction) {
				case UITableViewDirection.TopToBottom:
					startIndex = FindIndexOfCellAtPosition(startPosition.y);
					endIndex = FindIndexOfCellAtPosition(endPosition.y);
					break;
				case UITableViewDirection.RightToLeft:
					startIndex = FindIndexOfCellAtPosition(startPosition.x);
					endIndex = FindIndexOfCellAtPosition(endPosition.x);
					break;
				default: throw new ArgumentOutOfRangeException();
			}

			// find the first and the last index at row or column if it's a grid.
			if (_numberOfCellsAtRowOrColumn > 1) {
				startIndex = Mathf.FloorToInt((float)startIndex / _numberOfCellsAtRowOrColumn) * _numberOfCellsAtRowOrColumn;
				endIndex = Mathf.CeilToInt((float)(endIndex + 1) / _numberOfCellsAtRowOrColumn) * _numberOfCellsAtRowOrColumn - 1;
				endIndex = Mathf.Min(endIndex, _holders.Count-1);
			}

			return new Range(startIndex, endIndex);
		}

		private int FindIndexOfCellAtPosition(float position)
		{
			return FindIndexOfCellAtPosition(position, 0, _holders.Count);
		}

		private int FindIndexOfCellAtPosition(float position, int startIndex, int length)
		{
			while (startIndex < length) {
				var midIndex = (startIndex + length) / 2;
				if (_holders[midIndex].position > position) {
					length = midIndex;
					continue;
				}
				startIndex = midIndex + 1;
			}
			return Math.Max(0, startIndex - 1);
		}

		private void ResizeContent(int numberOfCells)
		{
			var lastMaxLowerMargin = 0f;
			var cumulativeScalar = 0f;
			var numOfRowOrColumn = Mathf.CeilToInt((float)numberOfCells / _numberOfCellsAtRowOrColumn);
			for (var i = 0; i < numOfRowOrColumn; i++) {
				// find max margin, scalar at row or column
				float maxUpperMargin = 0f, maxLowerMargin = 0f, maxScalar = 0f;
				for (var j = 0; j < _numberOfCellsAtRowOrColumn; j++) {
					var upperMargin = marginDataSource?.ScalarForUpperMarginInTableView(this, i) ?? 0f;
					maxUpperMargin = Mathf.Max(maxUpperMargin, upperMargin);
					var lowerMargin = marginDataSource?.ScalarForLowerMarginInTableView(this, i) ?? 0f;
					maxLowerMargin = Mathf.Max(maxLowerMargin, lowerMargin);
					var scalar = dataSource.ScalarForCellInTableView(this, i);
					maxScalar = Mathf.Max(maxScalar, scalar);
				}

				for (var j = 0; j < _numberOfCellsAtRowOrColumn; j++) {
					var index = Mathf.Min(i * _numberOfCellsAtRowOrColumn + j, numberOfCells - 1);
					var holder = _holders[index];
					holder.upperMargin = maxUpperMargin;
					holder.position = cumulativeScalar + lastMaxLowerMargin + maxUpperMargin;
					holder.scalar = maxScalar;
					Debug.Assert(maxScalar > 0f, $"Scalar of cell can not be less than zero, index:{i}.");
					holder.lowerMargin = maxLowerMargin;
				}

				cumulativeScalar += (lastMaxLowerMargin + maxUpperMargin + maxScalar);
				lastMaxLowerMargin = maxLowerMargin;
			}
			cumulativeScalar += lastMaxLowerMargin; // the last cell's margin

			var size = _content.sizeDelta;
			switch (_direction) {
				case UITableViewDirection.TopToBottom:
					size.y = cumulativeScalar;
					break;
				case UITableViewDirection.RightToLeft:
					size.x = cumulativeScalar;
					break;
				default: throw new ArgumentOutOfRangeException();
			}
			_content.sizeDelta = size;
		}

		private void OnScrollPositionChanged(Vector2 normalizedPosition)
		{
			if (_holders.Count <= 0)
				return;
			ReloadCells(normalizedPosition, false);
			DetectAndNotifyReachableStatus(normalizedPosition);
		}

		private void ReloadCells(Vector2 normalizedPosition, bool alwaysRearrangeCell)
		{
			var range = RecalculateVisibleRange(normalizedPosition);
			UnloadUnusedCells(range); // recycle invisible cells except life cycle is RecycleWhenReload
			LoadCells(range, alwaysRearrangeCell); // reuse or create visible cells
#if UNITY_EDITOR
			_content.name = $"Content({range.from}~{range.to})";
#endif
		}

		private void LoadCells(Range range, bool alwaysRearrangeCell)
		{
			foreach (var kvp in _loadedHolders) {
				if (kvp.Key >= range.from && kvp.Key <= range.to)
					continue;
				RearrangeCell(kvp.Key);
			}
			for (var i = range.from; i <= range.to; i++) {
				_loadedHolders[i] = _holders[i];
				LoadCell(i, alwaysRearrangeCell);
			}
		}

		private void LoadCell(int index, bool alwaysRearrangeCell)
		{
			var holder = _holders[index];
			if (holder.loadedCell != null) {
				if (alwaysRearrangeCell)
					RearrangeCell(index);
				return;
			}
			holder.loadedCell = dataSource.CellAtIndexInTableView(this, index);
			holder.loadedCell.rectTransform.SetParent(_content);
			holder.loadedCell.rectTransform.localScale = Vector3.one;
			holder.loadedCell.rectTransform.localRotation = Quaternion.identity;
			RearrangeCell(index);
			holder.loadedCell.gameObject.SetActive(true);
			holder.loadedCell.index = index;
			@delegate?.CellAtIndexInTableViewWillAppear(this, index);
#if UNITY_EDITOR
			_cellsPool.name = $"ReusableCells({_cellsPool.childCount})";
			holder.loadedCell.gameObject.name = $"{index}_{holder.loadedCell.reuseIdentifier}";
#endif
		}

		private void UnloadUnusedCells(Range visibleRange)
		{
			foreach (var kvp in _loadedHolders) {
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
			foreach (var kvp in _loadedHolders) {
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
			@delegate?.CellAtIndexInTableViewDidDisappear(this, index);
			cell.index = null;
			switch (cell.lifeCycle) {
				case UITableViewCellLifeCycle.RecycleWhenDisappeared:
				case UITableViewCellLifeCycle.RecycleWhenReloaded:
					var isExist = _reusableCellQueues.TryGetValue(cell.reuseIdentifier, out var cellsQueue);
					if (!isExist)
						throw new Exception("Queue is not existing.");
					cellsQueue.Enqueue(cell); // enqueue if recyclable
					cell.gameObject.SetActive(false);
					cell.transform.SetParent(_cellsPool);
#if UNITY_EDITOR
					_cellsPool.name = $"ReusableCells({_cellsPool.childCount})";
					cell.gameObject.name = cell.reuseIdentifier;
#endif
					break;
				case UITableViewCellLifeCycle.DestroyWhenDisappeared:
					Destroy(cell.gameObject); // destroy if non-reusable
					break;
				default: throw new ArgumentOutOfRangeException();
			}
			holder.loadedCell = null;
		}

		private void RearrangeCell(int index)
		{
			var holder = _holders[index];
			var cellRectTransform = holder.loadedCell.rectTransform;
			Vector2 anchoredPosition, cellSize, contentSize = _content.rect.size;
			var otherIndex = index % _numberOfCellsAtRowOrColumn;
			float otherScalar;
			Vector2 anchorMax = cellRectTransform.anchorMax, pivot = cellRectTransform.pivot;

			// Cells' alignment at last row or column for grid view. 
			var numberOfCellAtLastRowOrColumn = _holders.Count % _numberOfCellsAtRowOrColumn;
			var emptyNumberAtLastRowOrColumn = 0;
			var maxRowOrColumn = Mathf.CeilToInt((float)_holders.Count / _numberOfCellsAtRowOrColumn);
			if (numberOfCellAtLastRowOrColumn != 0 && index >= (maxRowOrColumn - 1) * _numberOfCellsAtRowOrColumn && index < _holders.Count)
				switch (_cellAlignment) {
					case UITableViewCellAlignment.RightOrTop: 
						otherIndex = _numberOfCellsAtRowOrColumn - numberOfCellAtLastRowOrColumn + otherIndex;
						break;
					case UITableViewCellAlignment.LeftOrBottom: // Do nothing.
						break;
					case UITableViewCellAlignment.Center: 
						emptyNumberAtLastRowOrColumn = _numberOfCellsAtRowOrColumn - numberOfCellAtLastRowOrColumn;
						break;
					default: throw new ArgumentOutOfRangeException();
				}

			switch (_direction)
			{
				case UITableViewDirection.TopToBottom:
					otherScalar = contentSize.x / _numberOfCellsAtRowOrColumn;
					anchoredPosition = new Vector2(
						-(contentSize.x - emptyNumberAtLastRowOrColumn * otherScalar) * anchorMax.x + otherIndex * otherScalar + (1f - pivot.x) * otherScalar, 
						contentSize.y * anchorMax.y - holder.position - (1f - pivot.y) * holder.scalar);
					cellSize.x = otherScalar;
					cellSize.y = holder.scalar;
					break;
				case UITableViewDirection.RightToLeft:
					otherScalar = contentSize.y / _numberOfCellsAtRowOrColumn;
					anchoredPosition = new Vector2(
						contentSize.x * anchorMax.x - holder.position - (1f - pivot.x) * holder.scalar, 
						(contentSize.y - emptyNumberAtLastRowOrColumn * otherScalar) * anchorMax.y - otherIndex * otherScalar - (1f - pivot.y) * otherScalar);
					cellSize.x = holder.scalar;
					cellSize.y = otherScalar;
					break;
				default: throw new ArgumentOutOfRangeException();
			}
			holder.loadedCell.rectTransform.anchoredPosition = anchoredPosition;
			var t = holder.loadedCell.transform;
			var pos = t.localPosition;
			pos.z = 0f;
			t.localPosition = pos;
			if (holder.loadedCell.isAutoResize)
				holder.loadedCell.rectTransform.sizeDelta = cellSize;
		}

		private void ReloadDataInternal(UITableViewCellLocation? startLocation, Vector2? startNormalizedPosition)
		{
			if (dataSource == null)
				throw new Exception("DataSource can not be null!");
			if (startLocation.HasValue && startNormalizedPosition.HasValue)
				throw new IndexOutOfRangeException("You can only choose one between startLocation and startNormalizedPosition.");
			if (startLocation?.index < 0)
				throw new IndexOutOfRangeException("Start index must be more than zero.");
			if (dataSource is IUIGridViewDataSource gridDataSource) {
				_cellAlignment = gridDataSource.AlignmentOfCellsAtRowOrColumnInGrid(this);
				_numberOfCellsAtRowOrColumn = gridDataSource.NumberOfCellsAtRowOrColumnInGrid(this);
			}
			if (_numberOfCellsAtRowOrColumn < 1)
				throw new Exception("Number of cells at row or column can not be less than 1!");

			UnloadAllCells();
			var oldCount = _holders.Count;
			var newCount = dataSource.NumberOfCellsInTableView(this);
			if (startLocation?.index > newCount - 1)
				throw new IndexOutOfRangeException("Start index must be less than quantity of cell.");
			var deltaCount = Mathf.Abs(oldCount - newCount);
			for (var i = 0; i < deltaCount; i++) {
				if (oldCount > newCount)
					_holders.RemoveAt(0);
				else if (oldCount < newCount)
					_holders.Add(new UITableViewCellHolder());
			}
			ResizeContent(newCount);

			if (newCount == 0)
				return;

			if (startLocation.HasValue)
				ScrollToCellAt(startLocation.Value);
			else if (startNormalizedPosition.HasValue) {
				ReloadCells(startNormalizedPosition.Value, false);
				_scrollRect.normalizedPosition = startNormalizedPosition.Value;
			} else {
				_isReloaded = true;
				_normalizedPositionWhenReloaded = _scrollRect.normalizedPosition;
				ReloadCells(_normalizedPositionWhenReloaded, false);
			}

			// Recalculate if the content is reaching view port's boundary.
			CalculateReachableStatus(_scrollRect.normalizedPosition, out var curIsReachingTopmostOrRightmost, out var curIsReachingBottommostOrLeftmost);
			_isReachingTopmostOrRightmost = curIsReachingTopmostOrRightmost;
			_isReachingBottommostOrLeftmost = curIsReachingBottommostOrLeftmost;
		}

		///<summary> Detect if the table view has reached or left the topmost/rightmost or bottommost/leftmost</summary>
		private void DetectAndNotifyReachableStatus(Vector2 normalizedPosition)
		{
			if (this.reachable == null)
				return;
			CalculateReachableStatus(normalizedPosition, out var curIsReachingTopmostOrRightmost, out var curIsReachingBottommostOrLeftmost);
			if (!_isReachingTopmostOrRightmost && curIsReachingTopmostOrRightmost) {
				this.reachable.TableViewReachedTopmostOrRightmost(this);
				_isReachingTopmostOrRightmost = true;
			} else if (_isReachingTopmostOrRightmost && !curIsReachingTopmostOrRightmost) {
				this.reachable.TableViewLeftTopmostOrRightmost(this);
				_isReachingTopmostOrRightmost = false;
			}
			if (!_isReachingBottommostOrLeftmost && curIsReachingBottommostOrLeftmost) {
				this.reachable.TableViewReachedBottommostOrLeftmost(this);
				_isReachingBottommostOrLeftmost = true;
			} else if (_isReachingBottommostOrLeftmost && !curIsReachingBottommostOrLeftmost) {
				this.reachable.TableViewLeftBottommostOrLeftmost(this);
				_isReachingBottommostOrLeftmost = false;
			}
		}

		private void CalculateReachableStatus(Vector2 normalizedPosition, out bool isReachingTopmostOrRightmost, out bool isReachingBottommostOrLeftmost)
		{
			isReachingTopmostOrRightmost = isReachingBottommostOrLeftmost = false;
			if (this.reachable == null)
				return;
			var upperTolerance = this.reachable.TableViewReachableEdgeTolerance(this);
			float curPosition, lowerTolerance;
			var deltaSize = _content.rect.size - _viewport.rect.size;
			switch (_direction) {
				case UITableViewDirection.TopToBottom:
					lowerTolerance = deltaSize.y - upperTolerance;
					curPosition = (1f - normalizedPosition.y) * deltaSize.y;
					break;
				case UITableViewDirection.RightToLeft: 
					lowerTolerance = deltaSize.x - upperTolerance;
					curPosition = (1f - normalizedPosition.x) * deltaSize.x;
					break;
				default: throw new ArgumentOutOfRangeException();
			}
			isReachingTopmostOrRightmost = curPosition < upperTolerance;
			isReachingBottommostOrLeftmost = curPosition > lowerTolerance;
		}

		private void StopAutoScroll(Action onScrollingFinished)
		{
			if (_autoScroll == null)
				return;
			StopCoroutine(_autoScroll);
			_autoScroll = null;
			onScrollingFinished?.Invoke();
		}

		private void StartAutoScroll(UITableViewCellLocation location, float time, Action onScrollingFinished)
		{
			StopAutoScroll(onScrollingFinished);
			_autoScroll = StartCoroutine(AutoScroll(location, time, onScrollingFinished));
		}

		private IEnumerator AutoScroll(UITableViewCellLocation location, float time, Action onScrollingFinished)
		{
			if (location.index > _holders.Count - 1 || location.index < 0)
				throw new IndexOutOfRangeException("Index must be less than cells' number and more than zero.");
			var from = _scrollRect.normalizedPosition;
			var to = GetNormalizedPositionOfCellAt(location);
			var progress = 0f; 
			var startAt = Time.time;
			while (!Mathf.Approximately(progress, 1f)) {
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

		/// <summary> Recycle or destroy all loaded cells.</summary>
		/// <exception cref="Exception">DataSource can not be null</exception>
		public void UnloadData()
		{
			if (dataSource == null)
				throw new Exception("DataSource can not be null!");
			UnloadAllCells();
			_holders.Clear();
			ResizeContent(0);
		}

		/// <summary> Replace the current cell at row with a new cell.  </summary>
		public void ReloadDataAt(int index)
		{
			RearrangeData();
			foreach (var cell in GetAllLoadedCells()) {
				if (!cell.index.HasValue || cell.index.Value != index)
					continue;
				var targetIdx = cell.index.Value;
				UnloadCell(targetIdx);
				LoadCell(targetIdx, true);
				break;
			}
		}

		/// <summary> Recycle or destroy all loaded cells then reload them again. </summary>
		/// <param name="startIndex">Table view will be scrolled to start index after data reloaded.</param>
		/// <param name="alignment"></param>
		/// <param name="withMargin"></param>
		/// <param name="displacement"></param>
		public void ReloadData(int startIndex, UITableViewCellAlignment alignment = UITableViewCellAlignment.RightOrTop, bool withMargin = false, float displacement = 0f)
		{
			var location = new UITableViewCellLocation(startIndex, alignment, withMargin, displacement);
			ReloadData(location);
		}

		public void ReloadData(UITableViewCellLocation location)
		{
			ReloadDataInternal(location, null);
		}

		/// <summary> Recycle or destroy all loaded cells then reload them again with specialized normalized position. </summary>
		/// <param name="normalizedPosition">specialized normalized position</param>
		public void ReloadData(Vector2 normalizedPosition)
		{
			ReloadDataInternal(null, normalizedPosition);
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
			ReloadDataInternal(null, null);
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

			var oldContentSize = _content.rect.size;
			var oldAnchoredPosition = _content.anchoredPosition;
			ResizeContent(newCount);
			_content.anchoredPosition = oldAnchoredPosition - (_content.rect.size - oldContentSize) * (Vector2.one - _content.pivot);
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
			foreach (var key in _swapper) {
				_loadedHolders[key + deltaCount] = _loadedHolders[key];
				_loadedHolders.Remove(key);
			}
			_swapper.Clear();

			var oldContentSize = _content.rect.size;
			var oldAnchoredPosition = _content.anchoredPosition;
			ResizeContent(newCount);
			_content.anchoredPosition = oldAnchoredPosition + (_content.rect.size - oldContentSize) * _content.pivot;
			ReloadCells(_scrollRect.normalizedPosition, true);
		}

		/// <summary> Get a cell from pool or instantiate a new one. </summary>
		/// <param name="prefab">A prefab which one inherited from UITableView.</param>
		/// <param name="lifeCycle">How the cell will be when it disappeared from scroll view's viewport or data is reloaded.</param>
		/// <param name="isAutoResize">The cell will be resized when it appearing into scroll view's viewport if isAutoResize is true, or not if false.</param>
		/// <typeparam name="T">Type of cell</typeparam>
		/// <returns>Subclass of UITableViewCell</returns>
		public T ReuseOrCreateCell<T>(T prefab, UITableViewCellLifeCycle lifeCycle = UITableViewCellLifeCycle.RecycleWhenDisappeared, bool isAutoResize = true) where T : UITableViewCell
		{
			return ReuseOrCreateCell(prefab.GetType().ToString(), prefab, lifeCycle, isAutoResize);
		}

		/// <summary> Get a cell from the pool which use your own reuse identifier or instantiate a new one. </summary>
		/// <param name="reuseIdentifier">You can create multiple different prefabs with only one UITableViewCell class by use your own reuse identifier</param>
		/// <param name="prefab">A prefab which one inherited from UITableView.</param>
		/// <param name="lifeCycle">How the cell will be when it disappeared from scroll view's viewport or data is reloaded.</param>
		/// <param name="isAutoResize">The cell will be resized when it appearing into scroll view's viewport if isAutoResize is true, or not if false.</param>
		/// <typeparam name="T">Type of cell</typeparam>
		/// <returns>Subclass of UITableViewCell</returns>
		public T ReuseOrCreateCell<T>(string reuseIdentifier, T prefab, UITableViewCellLifeCycle lifeCycle = UITableViewCellLifeCycle.RecycleWhenDisappeared, bool isAutoResize = true) where T : UITableViewCell
		{
			T cell;
			if (lifeCycle != UITableViewCellLifeCycle.DestroyWhenDisappeared) {
				var isExist = _reusableCellQueues.TryGetValue(reuseIdentifier, out var cellsQueue);
				if (!isExist) {
					cellsQueue = new Queue<UITableViewCell>();
					_reusableCellQueues.Add(reuseIdentifier, cellsQueue);
				}
				else if (cellsQueue.Count > 0) {
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

		/// <summary> Scroll to the cell with animation. </summary>
		/// <param name="index">Index of the cell</param>
		/// <param name="time">Animation time</param>
		/// <param name="withMargin">If TRUE, calculate margin(IUITableViewMargin) when locate the cell.</param>
		/// <param name="alignment">Alignment of the cell on UITableView.</param>
		/// <param name="displacement">The displacement relative to the cell. Positive number for move up, and negative number for move down.</param>
		/// <param name="onScrollingFinished">Will be called when animation is finished or interrupted.</param>
		/// <seealso cref="UIKit.UITableViewCellLocation">UITableViewCellLocation</seealso>
		/// <seealso cref="ScrollToCellAt(UITableViewCellLocation, float, Action)">ScrollToCellAt(UITableViewCellLocation, float, Action)</seealso>
		public void ScrollToCellAt(int index, float time, UITableViewCellAlignment alignment = UITableViewCellAlignment.RightOrTop, bool withMargin = false, float displacement = 0f, Action onScrollingFinished = null)
		{
			var location = new UITableViewCellLocation(index, alignment, withMargin, displacement);
			ScrollToCellAt(location, time, onScrollingFinished);
		}

		/// <summary> Scroll to the cell with animation. </summary>
		/// <param name="location">Use to locate a point in UITableView(scroll view's content).</param>
		/// <param name="time">Animation time</param>
		/// <param name="onScrollingFinished">Will be called when animation is finished or interrupted.</param>
		/// <exception cref="ArgumentException">Throw if time is negative</exception>
		public void ScrollToCellAt(UITableViewCellLocation location, float time, Action onScrollingFinished)
		{
			if (time < 0f)
				throw new ArgumentException("Time must be equal to or more than zero.");
			if (Mathf.Approximately(time, 0f))
				ScrollToCellAt(location);
			else
				StartAutoScroll(location, time, onScrollingFinished);
		}

		/// <summary> Scroll to the cell. </summary>
		/// <param name="index">Index of the cell</param>
		/// <param name="alignment">Alignment of the cell on UITableView.</param>
		/// <param name="withMargin">If TRUE, calculate margin(IUITableViewMargin) when locate the cell.</param>
		/// <param name="displacement">The displacement relative to the cell. Positive number for move up, and negative number for move down.</param>
		public void ScrollToCellAt(int index, UITableViewCellAlignment alignment = UITableViewCellAlignment.RightOrTop, bool withMargin = false, float displacement = 0f)
		{
			var location = new UITableViewCellLocation(index, alignment, withMargin, displacement);
			ScrollToCellAt(location);
		}

		/// <summary> Scroll to the cell. </summary>
		/// <param name="location">Use to locate a point in UITableView(scroll view's content).</param>
		public void ScrollToCellAt(UITableViewCellLocation location)
		{
			if (location.index > _holders.Count - 1 || location.index < 0)
				throw new IndexOutOfRangeException("Index must be less than cells' number and more than zero.");
			_scrollRect.normalizedPosition = GetNormalizedPositionOfCellAt(location);
			ReloadCells(_scrollRect.normalizedPosition, false);
		}

		/// <summary> Return scroll view's normalized position of cell at the location. </summary>
		/// <param name="location">Use to locate a point in UITableView(scroll view's content).</param>
		/// <returns>Normalized position of scroll view</returns>
		public Vector2 GetNormalizedPositionOfCellAt(UITableViewCellLocation location)
		{
			var normalizedPosition = _scrollRect.normalizedPosition;
			var deltaSize = _content.rect.size - _viewport.rect.size;
			var holder = _holders[location.index];
			var position = holder.position;
			float viewportLength;
			switch (_direction) {
				case UITableViewDirection.TopToBottom:
					viewportLength = _viewport.rect.height;
					break;
				case UITableViewDirection.RightToLeft:
					viewportLength = _viewport.rect.width;
					break;
				default: throw new ArgumentOutOfRangeException();
			}
			switch (location.alignment) {
				case UITableViewCellAlignment.RightOrTop:
					position -= (location.withMargin ? holder.upperMargin : 0f);
					break;
				case UITableViewCellAlignment.LeftOrBottom:
					position -= (viewportLength - holder.scalar);
					position += (location.withMargin ? holder.lowerMargin : 0f);
					break;
				case UITableViewCellAlignment.Center:
					var cellMargin = holder.lowerMargin - holder.upperMargin;
					var cellScalar = holder.scalar + (location.withMargin ? cellMargin : 0f);
					position -= (viewportLength - cellScalar) / 2f;
					break;
				default: throw new ArgumentOutOfRangeException();
			}
			position += location.displacement;
			switch (_direction) {
				case UITableViewDirection.TopToBottom:
					normalizedPosition.y = 1f - position / deltaSize.y;
					break;
				case UITableViewDirection.RightToLeft:
					normalizedPosition.x = 1f - position / deltaSize.x;
					break;
				default: throw new ArgumentOutOfRangeException();
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

		/// <summary> Return it if the cell at index is appearing or UITableViewCellLifeCycle is set to RecycleWhenReloaded. </summary>
		/// <param name="index">Index of cell at</param>
		/// <param name="result">The cell be found</param>
		/// <typeparam name="T">Type of UITableViewCell</typeparam>
		/// <returns></returns>
		public bool TryGetLoadedCell<T>(int index, out T result) where T : UITableViewCell
		{
			result = null;
			if (index < 0 || _holders.Count - 1 < index)
				return false;
			if (!_loadedHolders.TryGetValue(index, out var holder))
				return false;
			var cell = holder.loadedCell as T;
			if (cell == null)
				return false;
			result = cell;
			return true;
		}

		/// <summary> Return all appearing cells and those whose UITableViewCellLifeCycle is set to RecycleWhenReloaded. </summary>
		public IEnumerable<UITableViewCell> GetAllLoadedCells()
		{
			foreach (var kvp in _loadedHolders) {
				Debug.Assert(kvp.Value.loadedCell != null, nameof(kvp.Value.loadedCell) + " != null");
				yield return kvp.Value.loadedCell;
			}
		}

		/// <summary> Return all appearing cells and those whose UITableViewCellLifeCycle is set to RecycleWhenReloaded where condition returns true. </summary>
		public IEnumerable<T> GetAllLoadedCells<T>(Func<int, bool> condition) where T : UITableViewCell
		{
			foreach (var kvp in _loadedHolders) {
				Debug.Assert(kvp.Value.loadedCell != null, nameof(kvp.Value.loadedCell) + " != null");
				if (!condition.Invoke(kvp.Key))
					continue;
				var tCell = kvp.Value.loadedCell as T;
				if (tCell == null)
					continue;
				yield return tCell;
			}
		}

		/// <summary> The interface CellAtIndexInTableViewWillAppear(tableView, index) of all loaded cells will be called without recycling them.
		/// WARNING: If you want to resize any cell or change the quantity of them,
		/// use ReloadData() instead because the IUITableViewDataSource's methods will not be called. </summary>
		public void RefreshAllLoadedCells()
		{
			foreach (var kvp in _loadedHolders) {
				Debug.Assert(kvp.Value.loadedCell != null, nameof(kvp.Value.loadedCell) + " != null");
				this.@delegate.CellAtIndexInTableViewWillAppear(this, kvp.Key);
			}
		}

		/// <summary> Destroy the cells those which waiting for reuse. </summary>
		public void DestroyCachedReusableCells()
		{
			foreach (var queue in _reusableCellQueues.Values) {
				var count = queue.Count;
				for (var i = 0; i < count; i++) {
					var cell = queue.Dequeue();
					Destroy(cell);
				}
			}
		}

		private readonly struct Range
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
			/// <summary> Upper margin (top or right) for cell. </summary>
			public float upperMargin { get; set; }
			/// <summary> Lower margin (bottom or left) for cell. </summary>
			public float lowerMargin { get; set; }
			/// <summary> The position relative to scroll view's content without considering anchor. </summary>
			public float position { get; set; }
		}
	}

	public enum UITableViewDirection
	{
		/// <summary> Index of cell at the top is zero. </summary>
		TopToBottom = 0,
		/// <summary> Index of cell at the rightmost is zero. </summary>
		RightToLeft = 1,
	}
}
