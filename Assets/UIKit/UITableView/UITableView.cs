using System;
using System.Collections.Generic;
using UIKit.Helper;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UIKit
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(ScrollRect))]
	public class UITableView : UIBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
	{
		public const float DEFAULT_REACHABLE_EDGE_TOLERANCE = 0.1f;
		public const float DEFAULT_SPEED_OF_TRIGGER_MAGNETIC_ALIGNMENT = 100f;
		public const float DEFAULT_SPEED_OF_COMPLETE_MAGNETIC_ALIGNMENT = 10000f;
		public const float DEFAULT_FLICK_TIME_OF_TRIGGER_FLICK = 1f;
		public static readonly (float lower, float upper) DEFAULT_FLICK_DISTANCE_RANGE_OF_TRIGGER_FLICK = (40f, 100f);
		
		public ScrollRect scrollRect => _scrollRect;
		public IUITableViewDataSource dataSource { get; set; }
		public IUITableViewMargin marginDataSource { get; set; }
		public IUITableViewDelegate @delegate { get; set; }
		public IUITableViewReachable reachable { get; set; }
		public IUITableViewClickable clickable { get; set; }
		public IUITableViewDraggable draggable { get; set; }
		public IUITableViewMagneticAlignment magneticAlignment { get; set; }
		public IUITableViewFlickable flickable { get; set; }

		/// <summary> If TRUE, the UITableViewCellLifeCycle will be ignored and all cells will be loaded at once, or not when FALSE. </summary>
		public bool ignoreCellLifeCycle {
			get => _ignoreCellLifeCycle;
			set {
				if (_ignoreCellLifeCycle == value) return;
				_ignoreCellLifeCycle = value;
				ReloadData();
			}
		}

		public UITableViewDirection direction {
			get => _direction;
			set {
				if (_direction == value) return;
				_direction = value;
				var np = _scrollRect.normalizedPosition;
				if (_direction.IsVertical()) np.y = 1f - _scrollRect.normalizedPosition.y;
				else np.x = 1f - _scrollRect.normalizedPosition.x;
				ReloadData(np);
			}
		}

		List<int> _columnPerRowInGrid;
		readonly List<UITableViewCellHolder> _holders = new List<UITableViewCellHolder>(); // all holders
		readonly Dictionary<string, Queue<UITableViewCell>> _reusableCellQueues = new Dictionary<string, Queue<UITableViewCell>>(); // for caching the cells which waiting for be reused.
		readonly Dictionary<int, UITableViewCellHolder> _loadedHolders = new Dictionary<int, UITableViewCellHolder>(); // appearing cells and those whose UITableViewLifeCycle is set to RecycleWhenReloaded.
		readonly List<int> _swapper = new List<int>(); // helper for modifying dictionary of _loadedHolders.
		readonly ScrollState _scrollState = new ScrollState();
		Transform _cellsPool;
		Vector2 _flickPositionAt;
		float _flickStartAt;
		bool _onNormalizedPositionChangedCalled;
		bool _isReachingBottommostOrLeftmost, _isReachingTopmostOrRightmost; // for detecting boundary when IUITableViewReachable is assigned.
		int? _dragCellIndex, _clickCellIndex;
		UITableViewAlignment _alignment = UITableViewAlignment.RightOrTop;
		UITableViewMagneticInternalState _magneticInternalState = UITableViewMagneticInternalState.Stopped;
		[SerializeField] ScrollRect _scrollRect;
		[SerializeField] RectTransform _viewport;
		[SerializeField] RectTransform _content;
		[SerializeField] UITableViewDirection _direction = UITableViewDirection.TopToBottom;
		[SerializeField] bool _ignoreCellLifeCycle;
		/// <summary> Tag for distinguishing table view. For example, Using two table views with only one datasource. </summary>
		public int _tag;
		/// <summary> If true, the click events will be kept even if drag is began.</summary>
		public bool keepClickEvenIfBeginDrag;
		public delegate void OnScrollingStopped(bool interrupted);

		protected override void Awake()
		{
			base.Awake();
			InitializeScrollRect();
			InitializeCellsPool();
			_scrollRect.onValueChanged.AddListener(OnNormalizedPositionChanged);
		}

		protected override void OnDestroy()
		{
			if (_scrollRect != null) _scrollRect.onValueChanged.RemoveListener(OnNormalizedPositionChanged);
			base.OnDestroy();
		}

		protected virtual void Update()
		{
			UpdateScrollState();
			UpdateMagneticState(null, null, null);
		}

		void UpdateScrollState()
		{
			if (!_scrollState.started) return;
			_scrollRect.normalizedPosition = _scrollState.normalizedPosition;
			if (!_scrollState.stopped) return;
			_scrollState.Stop();
		}

		void UpdateMagneticState(int? toIndexOfCellAt, float? overrideDuration, OnScrollingStopped overrideOnScrollingStopped)
		{
			if (magneticAlignment == null) return;
			if (!toIndexOfCellAt.HasValue) {
				if (_magneticInternalState != UITableViewMagneticInternalState.Scrolling) return;
				var speed = _direction.IsVertical() ? Mathf.Abs(this.scrollRect.velocity.y) : Mathf.Abs(this.scrollRect.velocity.x);
				if (speed > magneticAlignment.SpeedOfTriggerMagneticAlignmentInTableView(this)) return;
			}

			_magneticInternalState = UITableViewMagneticInternalState.Attracting;
			var calibrationPoint = magneticAlignment.CalibrationPointOfMagneticAlignmentInTableView(this);
			var fromNp = _scrollRect.normalizedPosition;
			var cellIndex = toIndexOfCellAt.HasValue ? toIndexOfCellAt.Value : FindIndexOfCellAtCalibrationPoint(calibrationPoint, fromNp);
			var toNp = GetNormalizedPositionOfCellAt(cellIndex, calibrationPoint);
			float duration;
			if (overrideDuration.HasValue)
				duration = overrideDuration.Value;
			else {
				var deltaDistance = (toNp - fromNp) * _content.rect.size;
				duration = Mathf.Abs(_direction.IsVertical() ? deltaDistance.y : deltaDistance.x) / magneticAlignment.SpeedOfCompleteMagneticAlignmentInTableView(this);
			}

			magneticAlignment.MagneticStateDidChangeInTableView(this, cellIndex, UITableViewMagneticState.Start);
			ScrollToNormalizedPosition(fromNp, toNp, duration, onScrollingStopped: interrupted => {
				_magneticInternalState = UITableViewMagneticInternalState.Stopped;
				overrideOnScrollingStopped?.Invoke(interrupted);
				var state = interrupted ? UITableViewMagneticState.Interrupted : UITableViewMagneticState.Completed;
				magneticAlignment.MagneticStateDidChangeInTableView(this, cellIndex, state);
			});
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			InitializeScrollRect();
			Validate();
		}
#endif
		void InitializeScrollRect()
		{
			if (_scrollRect == null) _scrollRect = GetComponent<ScrollRect>();
			if (_viewport == null) _viewport = _scrollRect.viewport;
			if (_content == null) _content = _scrollRect.content;
		}

		void InitializeCellsPool()
		{
			if (_cellsPool != null) return;
			var poolObject = new GameObject("ReusableCells");
			_cellsPool = poolObject.transform;
			_cellsPool.SetParent(_scrollRect.transform);
		}

		/// <summary> Validate if the settings are compatible with UITableView. </summary>
		void Validate()
		{
			var sizeFitter = _content.GetComponent<ContentSizeFitter>();
			if (sizeFitter == null) return; // ContentSizeFitter which attaching to content can not be set to NON-Unconstrained.
			if (sizeFitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained || sizeFitter.verticalFit != ContentSizeFitter.FitMode.Unconstrained)
				throw new Exception("ContentSizeFitter which attaching to content can not be set to NON-Unconstrained.");
			if ((_direction.IsVertical() && !Mathf.Approximately(_content.anchorMin.y, _content.anchorMax.y)) ||
			    (!_direction.IsVertical() && !Mathf.Approximately(_content.anchorMin.x, _content.anchorMax.x)))
				throw new Exception("The direction(UITableViewDirection) of content's anchor can NOT be set to stretch.");
		}

		Vector2Int RecalculateVisibleRange(Vector2 normalizedPosition)
		{
			if (_direction.IsTopToBottomOrRightToLeft()) 
				normalizedPosition = Vector2.one - normalizedPosition;
			var contentSize = _content.rect.size;
			var viewportSize = _ignoreCellLifeCycle ? contentSize : _viewport.rect.size;
			var startPosition = normalizedPosition * (contentSize - viewportSize);
			var startIndex = FindIndexOfCellAtPosition(startPosition);
			var endIndex = FindIndexOfCellAtPosition(startPosition + viewportSize);
			if (_columnPerRowInGrid != null) {
				startIndex -= _holders[startIndex].columnIndex;
				var e = _holders[endIndex];
				endIndex += _columnPerRowInGrid[e.rowIndex] - e.columnIndex - 1;
				endIndex = Mathf.Min(endIndex, _holders.Count - 1);
			}
			return new Vector2Int(startIndex, endIndex);
		}

		int FindIndexOfCellAtPosition(Vector2 position)
		{
			return FindIndexOfCellAtPosition(position, 0, _holders.Count);
		}
		int FindIndexOfCellAtPosition(Vector2 position, int startIndex, int length)
		{
			var positionXY = _direction.IsVertical() ? position.y : position.x;
			while (startIndex < length) {
				var midIndex = (startIndex + length) / 2;
				if (_holders[midIndex].position > positionXY) {
					length = midIndex;
					continue;
				}
				startIndex = midIndex + 1;
			}
			return Math.Max(0, startIndex - 1);
		}
		int FindIndexOfCellAtCalibrationPoint(Vector2 calibrationPoint, Vector2 normalizedPosition)
		{
			var np = _direction.IsTopToBottomOrRightToLeft() ? Vector2.one - normalizedPosition : normalizedPosition;
			var tvPos = np * _content.rect.size - _viewport.rect.size * (np - calibrationPoint);
			return FindIndexOfCellAtPosition(tvPos);
		}

		void ResizeContent(int numberOfCells)
		{
			var lastMaxLowerMargin = 0f;
			var cumulativeLength = 0f;
			var cellIndex = 0;
			var rowNumber = _columnPerRowInGrid?.Count ?? numberOfCells;
			for (var rowIndex = 0; rowIndex < rowNumber; rowIndex++) {
				// find max margin, length at row
				float maxUpperMargin = 0f, maxLowerMargin = 0f, maxLength = 0f;
				var upperMargin = _direction.IsTopToBottomOrRightToLeft()
					? (marginDataSource?.LengthForUpperMarginInTableView(this, rowIndex) ?? 0f)
					: (marginDataSource?.LengthForLowerMarginInTableView(this, rowIndex) ?? 0f);
				maxUpperMargin = Mathf.Max(maxUpperMargin, upperMargin);
				var lowerMargin = _direction.IsTopToBottomOrRightToLeft()
					? (marginDataSource?.LengthForLowerMarginInTableView(this, rowIndex) ?? 0f)
					: (marginDataSource?.LengthForUpperMarginInTableView(this, rowIndex) ?? 0f);
				maxLowerMargin = Mathf.Max(maxLowerMargin, lowerMargin);

				var columnNumber = _columnPerRowInGrid?[rowIndex] ?? 1;
				var cellIndexForCalculateMaxLength = cellIndex;
				for (var columnIndex = 0; columnIndex < columnNumber && cellIndexForCalculateMaxLength < numberOfCells; columnIndex++) {
					var length = dataSource.LengthForCellInTableView(this, cellIndexForCalculateMaxLength);
					maxLength = Mathf.Max(maxLength, length);
					cellIndexForCalculateMaxLength++;
				}

				for (var columnIndex = 0; columnIndex < columnNumber && cellIndex < numberOfCells; columnIndex++) {
					var holder = _holders[cellIndex];
					holder.upperMargin = maxUpperMargin;
					holder.position = cumulativeLength + lastMaxLowerMargin + maxUpperMargin;
					holder.rowIndex = rowIndex;
					holder.columnIndex = columnIndex;
					holder.length = maxLength;
					Debug.Assert(maxLength > 0f, $"Length of cell can not be less than zero, index:{rowIndex}.");
					holder.lowerMargin = maxLowerMargin;
					cellIndex++;
				}

				cumulativeLength += (lastMaxLowerMargin + maxUpperMargin + maxLength);
				lastMaxLowerMargin = maxLowerMargin;
			}

			cumulativeLength += lastMaxLowerMargin; // the last cell's margin
			_content.sizeDelta = _direction.IsVertical()
				? new Vector2(_content.sizeDelta.x, cumulativeLength)
				: new Vector2(cumulativeLength, _content.sizeDelta.y);
		}

		void OnNormalizedPositionChanged(Vector2 normalizedPosition)
		{
			_onNormalizedPositionChangedCalled = true;
			if (_holders.Count <= 0) return;
			ReloadCells(normalizedPosition, false);
			DetectAndNotifyReachableStatus();
		}

		void ReloadCells(Vector2 normalizedPosition, bool alwaysRearrangeCell)
		{
			var range = RecalculateVisibleRange(normalizedPosition);
			UnloadUnusedCells(range); // recycle invisible cells except life cycle is RecycleWhenReload
			LoadCells(range, alwaysRearrangeCell); // reuse or create visible cells
#if UNITY_EDITOR
			_content.name = $"Content({range.x}~{range.y})";
#endif
		}

		void LoadCells(Vector2Int range, bool alwaysRearrangeCell)
		{
			foreach (var kvp in _loadedHolders) {
				if (kvp.Key >= range.x && kvp.Key <= range.y) continue;
				RearrangeCell(kvp.Key);
			}
			for (var i = range.x; i <= range.y; i++) {
				_loadedHolders[i] = _holders[i];
				LoadCell(i, alwaysRearrangeCell);
			}
		}

		void LoadCell(int index, bool alwaysRearrangeCell)
		{
			var holder = _holders[index];
			if (holder.loadedCell != null) {
				if (alwaysRearrangeCell) RearrangeCell(index);
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

		void UnloadUnusedCells(Vector2Int visibleRange)
		{
			foreach (var kvp in _loadedHolders) {
				if (kvp.Key >= visibleRange.x && kvp.Key <= visibleRange.y) continue;
				if (kvp.Value.loadedCell.lifeCycle == UITableViewCellLifeCycle.RecycleWhenReloaded) continue;
				UnloadCell(kvp.Key);
				_swapper.Add(kvp.Key);
			}
			foreach (var key in _swapper)
				_loadedHolders.Remove(key);
			_swapper.Clear();
		}

		void UnloadAllCells()
		{
			foreach (var kvp in _loadedHolders) {
				UnloadCell(kvp.Key);
				_swapper.Add(kvp.Key);
			}
			foreach (var key in _swapper)
				_loadedHolders.Remove(key);
			_swapper.Clear();
		}

		void UnloadCell(int index)
		{
			var holder = _holders[index];
			var cell = holder.loadedCell;
			@delegate?.CellAtIndexInTableViewDidDisappear(this, index);
			cell.index = null;
			switch (cell.lifeCycle) {
				case UITableViewCellLifeCycle.RecycleWhenDisappeared:
				case UITableViewCellLifeCycle.RecycleWhenReloaded:
					var isExist = _reusableCellQueues.TryGetValue(cell.reuseIdentifier, out var cellsQueue);
					if (!isExist) throw new Exception("Queue is not existing.");
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

		void RearrangeCell(int index)
		{
			var holder = _holders[index];
			int columnIndex = holder.columnIndex, columnNumber = 1;
			// Cells' alignment at last row for grid view. 
			var emptyColumnAtLastRow = 0;
			if (_columnPerRowInGrid != null && holder.rowIndex == _columnPerRowInGrid.Count - 1) {
				columnNumber = _columnPerRowInGrid[holder.rowIndex];
				switch (_alignment) {
					case UITableViewAlignment.RightOrTop: 
						columnIndex = columnNumber - holder.columnIndex - 1;
						break;
					case UITableViewAlignment.LeftOrBottom: // Do nothing.
						break;
					case UITableViewAlignment.Center: 
						emptyColumnAtLastRow = columnNumber - _holders[_holders.Count - 1].columnIndex - 1; 
						break;
					default: throw new ArgumentOutOfRangeException();
				}
			}

			float lengthOfColumn;
			var cellRectTransform = holder.loadedCell.rectTransform;
			Vector2 anchoredPosition, cellSize, contentSize = _content.rect.size;
			Vector2 anchorMax = cellRectTransform.anchorMax, anchorMin = cellRectTransform.anchorMin, pivot = cellRectTransform.pivot;
			if (_columnPerRowInGrid != null)
				columnNumber = _columnPerRowInGrid[holder.rowIndex];
			if (_direction.IsVertical()) {
				lengthOfColumn = contentSize.x / columnNumber;
				anchoredPosition.x = (emptyColumnAtLastRow / 2f + columnIndex + pivot.x) * lengthOfColumn - contentSize.x * anchorMax.x;
				anchoredPosition.y = _direction.IsTopToBottomOrRightToLeft()
					? (-(anchorMin.y - pivot.y) * holder.length - holder.position)
					: (pivot.y * holder.length + holder.position);
				cellSize.x = lengthOfColumn;
				cellSize.y = holder.length;
			} else {
				lengthOfColumn = contentSize.y / columnNumber;
				anchoredPosition.x = _direction.IsTopToBottomOrRightToLeft()
					? (-(anchorMin.x - pivot.x) * holder.length - holder.position)
					: (pivot.x * holder.length + holder.position);
				anchoredPosition.y = (emptyColumnAtLastRow / 2f + columnIndex + pivot.y) * lengthOfColumn - contentSize.y * anchorMax.y;
				cellSize.x = holder.length;
				cellSize.y = lengthOfColumn;
			}

			var t = holder.loadedCell.rectTransform;
			t.anchoredPosition = anchoredPosition;
			var pos = t.localPosition;
			pos.z = 0f;
			t.localPosition = pos;
			if (holder.loadedCell.isAutoResize) t.sizeDelta = cellSize;
		}

		void ReloadDataInternal(UITableViewCellLocation? startLocation, Vector2? startNormalizedPosition)
		{
			if (dataSource == null) throw new Exception("DataSource can not be null!");
			if (startLocation.HasValue && startNormalizedPosition.HasValue) throw new IndexOutOfRangeException("You can only choose one between startLocation and startNormalizedPosition.");
			if (startLocation?.index < 0) throw new IndexOutOfRangeException("Start index must be more than zero.");
			var newCount = dataSource.NumberOfCellsInTableView(this);
			if (dataSource is IUIGridViewDataSource gridDataSource) {
				_columnPerRowInGrid = _columnPerRowInGrid ?? new List<int>();
				_columnPerRowInGrid.Clear();
				_alignment = gridDataSource.AlignmentOfCellsAtLastRow(this);
				int numberOfCellsAtRow, rowIndex = 0;
				for (var i = 0; i < newCount; i += numberOfCellsAtRow) {
					numberOfCellsAtRow = gridDataSource.NumberOfColumnPerRow(this, rowIndex);
					if (numberOfCellsAtRow < 1) throw new Exception("Number of cells at row can not be less than 1!");
					_columnPerRowInGrid.Add(numberOfCellsAtRow);
					rowIndex++;
				}
			}
			else _columnPerRowInGrid = null;

			UnloadAllCells();
			var oldCount = _holders.Count;
			if (startLocation?.index > newCount - 1) throw new IndexOutOfRangeException("Start index must be less than quantity of cell.");
			var deltaCount = Mathf.Abs(oldCount - newCount);
			for (var i = 0; i < deltaCount; i++) {
				if (oldCount > newCount)
					_holders.RemoveAt(0);
				else if (oldCount < newCount)
					_holders.Add(new UITableViewCellHolder());
			}
			ResizeContent(newCount);
			if (newCount == 0) return;

			_onNormalizedPositionChangedCalled = false;
			if (magneticAlignment != null) {
				var calibrationPoint = magneticAlignment.CalibrationPointOfMagneticAlignmentInTableView(this);
				var cellIndex = startLocation.HasValue
					? startLocation.Value.index
					: FindIndexOfCellAtCalibrationPoint(calibrationPoint, startNormalizedPosition.HasValue ? startNormalizedPosition.Value : _scrollRect.normalizedPosition);
				_scrollRect.normalizedPosition = GetNormalizedPositionOfCellAt(cellIndex, calibrationPoint);
			}
			else {
				if (startLocation.HasValue)
					_scrollRect.normalizedPosition = GetNormalizedPositionOfCellAt(startLocation.Value);
				else if (startNormalizedPosition.HasValue)
					_scrollRect.normalizedPosition = startNormalizedPosition.Value;
				else
					_scrollRect.normalizedPosition = _scrollRect.normalizedPosition; // WORKAROUND: Make sure private method of EnsureLayoutHasRebuilt() in scrollRect is called.
			}
			if (!_onNormalizedPositionChangedCalled)
				ReloadCells(_scrollRect.normalizedPosition, false);

			// Recalculate if the content is reaching view port's boundary.
			CalculateReachableStatus(out var curIsReachingTopmostOrRightmost, out var curIsReachingBottommostOrLeftmost);
			_isReachingTopmostOrRightmost = curIsReachingTopmostOrRightmost;
			_isReachingBottommostOrLeftmost = curIsReachingBottommostOrLeftmost;
		}

		///<summary> Detect if the table view has reached or left the topmost/rightmost or bottommost/leftmost</summary>
		void DetectAndNotifyReachableStatus()
		{
			if (this.reachable == null) return;
			CalculateReachableStatus(out var curIsReachingTopmostOrRightmost, out var curIsReachingBottommostOrLeftmost);
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

		void CalculateReachableStatus(out bool isReachingTopmostOrRightmost, out bool isReachingBottommostOrLeftmost)
		{
			isReachingTopmostOrRightmost = isReachingBottommostOrLeftmost = false;
			if (this.reachable == null) return;
			var upperTolerance = this.reachable.TableViewReachableEdgeTolerance(this);
			float curPosition, lowerTolerance;
			var deltaSize = _content.rect.size - _viewport.rect.size;
			if (_direction.IsVertical()) {
				lowerTolerance = deltaSize.y - upperTolerance;
				curPosition = (1f - _scrollRect.normalizedPosition.y) * deltaSize.y;
			} else {
				lowerTolerance = deltaSize.x - upperTolerance;
				curPosition = (1f - _scrollRect.normalizedPosition.x) * deltaSize.x;
			}
			isReachingTopmostOrRightmost = curPosition < upperTolerance;
			isReachingBottommostOrLeftmost = curPosition > lowerTolerance;
		}

		/// <summary> Resize and reposition cells without recycle or destroy them. </summary>
		public void RearrangeData()
		{
			if (dataSource == null) throw new Exception("DataSource can not be null!");
			var oldCount = _holders.Count;
			var newCount = dataSource.NumberOfCellsInTableView(this);
			if (oldCount != newCount) throw new Exception("Rearrange can not be called if count is changed");
			ResizeContent(newCount);
			ReloadCells(_scrollRect.normalizedPosition, true);
		}

		/// <summary> Recycle or destroy all loaded cells.</summary>
		/// <exception cref="Exception">DataSource can not be null</exception>
		public void UnloadData()
		{
			if (dataSource == null) throw new Exception("DataSource can not be null!");
			UnloadAllCells();
			_holders.Clear();
			ResizeContent(0);
		}

		/// <summary> Replace the current cell at row with a new cell.  </summary>
		public void ReloadDataAt(int index)
		{
			RearrangeData();
			foreach (var cell in GetAllLoadedCells()) {
				if (!cell.index.HasValue || cell.index.Value != index) continue;
				var targetIdx = cell.index.Value;
				UnloadCell(targetIdx);
				LoadCell(targetIdx, true);
				break;
			}
		}

		/// <summary> Recycle or destroy all loaded cells then reload them again. </summary>
		/// <param name="startIndex">Table view will be scrolled to start index after data reloaded.</param>
		public void ReloadData(int startIndex, UITableViewAlignment alignment = UITableViewAlignment.RightOrTop, bool withMargin = false, float displacement = 0f)
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

		/// <summary> Recycle or destroy all loaded cells then reload them again with current normalized position. </summary>
		public void ReloadData()
		{
			ReloadDataInternal(null, null);
		}

		/// <summary> Append cells to table view without reload them. </summary>
		/// <exception cref="Exception">AppendData() can not be called if number of cells is decreased.</exception>
		public void AppendData()
		{
			if (dataSource == null) throw new Exception("DataSource can not be null!");
			var oldCount = _holders.Count;
			var newCount = dataSource.NumberOfCellsInTableView(this);
			if (oldCount > newCount) throw new Exception("AppendData() can not be called if number of cells is decreased");
			for (var i = 0; i < newCount - oldCount; i++)
				_holders.Add(new UITableViewCellHolder());
			var oldContentSize = _content.rect.size;
			var oldAnchoredPosition = _content.anchoredPosition;
			ResizeContent(newCount);
			var pivot = _direction.IsTopToBottomOrRightToLeft() ? _content.pivot - Vector2.one : _content.pivot;
			_content.anchoredPosition = oldAnchoredPosition + (_content.rect.size - oldContentSize) * pivot;
			ReloadCells(_scrollRect.normalizedPosition, true);
		}

		/// <summary> Prepend cells to table view without reload them. </summary>
		/// <exception cref="Exception">PrependData() can not be called if number of cells is decreased.</exception>
		public void PrependData()
		{
			if (dataSource == null) throw new Exception("DataSource can not be null!");
			var oldCount = _holders.Count;
			var newCount = dataSource.NumberOfCellsInTableView(this);
			var deltaCount = newCount - oldCount;
			if (deltaCount < 0) throw new Exception("PrependData() can not be called if number of cells is decreased.");

			for (var i = 0; i < deltaCount; i++) _holders.Insert(0, new UITableViewCellHolder());

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
			var pivot = _direction.IsTopToBottomOrRightToLeft() ? _content.pivot : (_content.pivot - Vector2.one);
			_content.anchoredPosition = oldAnchoredPosition + (_content.rect.size - oldContentSize) * pivot;
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
			return ReuseOrCreateCell(prefab.GetType().ToString(), prefab, lifeCycle, isAutoResize);
		}

		public T ReuseOrCreateCell<T>(string reuseIdentifier, T prefab, UITableViewCellLifeCycle lifeCycle = UITableViewCellLifeCycle.RecycleWhenDisappeared, bool isAutoResize = true) where T : UITableViewCell
		{
			if (string.IsNullOrEmpty(reuseIdentifier)) {
				lifeCycle = UITableViewCellLifeCycle.DestroyWhenDisappeared;
				Debug.LogWarning($"Cell ({typeof(T).FullName}) which use null or empty reuseIdentifier will be set UITableViewCellLifeCycle.DestroyWhenDisappeared forcibly.");
			}

			Vector2 cellAnchorMin, cellAnchorMax;
			switch (_direction) {
				case UITableViewDirection.TopToBottom:
					cellAnchorMax = cellAnchorMin = Vector2.up;
					break;
				case UITableViewDirection.BottomToTop:
					cellAnchorMax = cellAnchorMin = Vector2.zero;
					break;
				case UITableViewDirection.RightToLeft:
					cellAnchorMax = cellAnchorMin = Vector2.one;
					break;
				case UITableViewDirection.LeftToRight:
					cellAnchorMax = cellAnchorMin = Vector2.up;
					break;
				default: throw new ArgumentOutOfRangeException();
			}

			T cell = null;
			var reused = false;
			if (lifeCycle != UITableViewCellLifeCycle.DestroyWhenDisappeared) {
				var isExist = _reusableCellQueues.TryGetValue(reuseIdentifier, out var cellsQueue);
				if (!isExist) {
					cellsQueue = new Queue<UITableViewCell>();
					_reusableCellQueues.Add(reuseIdentifier, cellsQueue);
				}
				else if (cellsQueue.Count > 0) {
					cell = cellsQueue.Dequeue() as T;
					reused = true;
				}
			}

			if (!reused) {
				cell = Instantiate(prefab);
				cell.reuseIdentifier = reuseIdentifier;
			}
			cell.rectTransform.anchorMin = cellAnchorMin;
			cell.rectTransform.anchorMax = cellAnchorMax;
			cell.isAutoResize = isAutoResize;
			cell.lifeCycle = lifeCycle;
			return cell;
		}

		#region Scroll to specific cell
		public void StopScroll()
		{
			_scrollState.Stop();
		}

		void ScrollToNormalizedPosition(Vector2 from, Vector2 to, float duration, OnScrollingStopped onScrollingStopped)
		{
			_scrollState.Stop();
			_scrollState.Start(from, to, duration, onScrollingStopped);
		}

		/// <summary> Scroll to the cell with animation. </summary>
		/// <param name="index">Index of the cell</param>
		/// <param name="duration">Animation time</param>
		/// <param name="withMargin">If TRUE, calculate margin(IUITableViewMargin) when locate the cell.</param>
		/// <param name="alignment">Alignment of the cell on UITableView.</param>
		/// <param name="displacement">The displacement relative to the cell. Positive number for move up, and negative number for move down.</param>
		/// <param name="onScrollingStopped">Will be called when animation is finished or interrupted.</param>
		/// <seealso cref="UIKit.UITableViewCellLocation">UITableViewCellLocation</seealso>
		/// <seealso cref="ScrollToCellAt(UITableViewCellLocation, float, Action)">ScrollToCellAt(UITableViewCellLocation, float, Action)</seealso>
		public void ScrollToCellAt(int index, float duration, UITableViewAlignment alignment = UITableViewAlignment.RightOrTop, bool withMargin = false, float displacement = 0f, OnScrollingStopped onScrollingStopped = null)
		{
			if (index > _holders.Count - 1 || index < 0) throw new IndexOutOfRangeException("Index must be less than cells' number and more than zero.");
			if (magneticAlignment == null)
				ScrollToNormalizedPosition(_scrollRect.normalizedPosition, GetNormalizedPositionOfCellAt(index, alignment, withMargin, displacement), duration, onScrollingStopped);
			else {
				Debug.LogWarning("Table view with Magnetic will ignore alignment, withMargin and displacement.");
				UpdateMagneticState(index, duration, onScrollingStopped);
			}
		}

		/// <summary> Scroll to the cell. </summary>
		/// <param name="index">Index of the cell</param>
		/// <param name="alignment">Alignment of the cell on UITableView.</param>
		/// <param name="withMargin">If TRUE, calculate margin(IUITableViewMargin) when locate the cell.</param>
		/// <param name="displacement">The displacement relative to the cell. Positive number for move up, and negative number for move down.</param>
		public void ScrollToCellAt(int index, UITableViewAlignment alignment = UITableViewAlignment.RightOrTop, bool withMargin = false, float displacement = 0f)
		{
			ScrollToCellAt(index, 0f, alignment, withMargin, displacement, null);
		}

		/// <summary> Scroll to the cell with animation. </summary>
		/// <param name="location">Use to locate a point in UITableView(scroll view's content).</param>
		/// <param name="duration">Animation time</param>
		/// <param name="onScrollingStopped">Will be called when animation is finished or interrupted.</param>
		public void ScrollToCellAt(UITableViewCellLocation location, float duration = 0f, OnScrollingStopped onScrollingStopped = null)
		{
			ScrollToCellAt(location.index, duration, location.alignment, location.withMargin, location.displacement, onScrollingStopped);
		}

		/// <summary> Return scroll view's normalized position of cell at the location. </summary>
		/// <param name="location">Use to locate a point in UITableView(scroll view's content).</param>
		/// <returns>Normalized position of scroll view</returns>
		public Vector2 GetNormalizedPositionOfCellAt(UITableViewCellLocation location)
		{
			return GetNormalizedPositionOfCellAt(location.index, location.alignment, location.withMargin, location.displacement);
		}

		Vector2 GetNormalizedPositionOfCellAt(int index, Vector2 calibrationPoint)
		{
			var displacement = (calibrationPoint - Vector2.one * 0.5f) * _viewport.rect.size;
			return GetNormalizedPositionOfCellAt(index, UITableViewAlignment.Center, false, _direction.IsVertical() ? displacement.y : displacement.x);
		}

		public Vector2 GetNormalizedPositionOfCellAt(int index, UITableViewAlignment alignment, bool withMargin, float displacement)
		{
			var holder = _holders[index];
			var position = holder.position;
			var viewportLength = _direction.IsVertical() ? _viewport.rect.height : _viewport.rect.width;
			switch (alignment) {
				case UITableViewAlignment.RightOrTop:
					position -= (withMargin ? holder.upperMargin : 0f);
					break;
				case UITableViewAlignment.LeftOrBottom:
					position -= (viewportLength - holder.length);
					position += (withMargin ? holder.lowerMargin : 0f);
					break;
				case UITableViewAlignment.Center:
					var cellMargin = holder.lowerMargin - holder.upperMargin;
					var cellLength = holder.length + (withMargin ? cellMargin : 0f);
					position -= (viewportLength - cellLength) / 2f;
					break;
				default: throw new ArgumentOutOfRangeException();
			}

			var deltaSize = _content.rect.size - _viewport.rect.size;
			var normalizedPosition = _scrollRect.normalizedPosition;
			switch (_direction) {
				case UITableViewDirection.TopToBottom:
					position += displacement;
					normalizedPosition.y = 1f - position / deltaSize.y;
					break;
				case UITableViewDirection.BottomToTop:
					position -= displacement;
					normalizedPosition.y = position / deltaSize.y;
					break;
				case UITableViewDirection.RightToLeft:
					position += displacement;
					normalizedPosition.x = 1f - position / deltaSize.x;
					break;
				case UITableViewDirection.LeftToRight:
					position -= displacement;
					normalizedPosition.x = position / deltaSize.x;
					break;
				default: throw new ArgumentOutOfRangeException();
			}
			if (_scrollRect.movementType == ScrollRect.MovementType.Unrestricted) 
				return normalizedPosition;
			var x = Mathf.Clamp(normalizedPosition.x, 0f, 1f);
			var y = Mathf.Clamp(normalizedPosition.y, 0f, 1f);
			return new Vector2(x, y);
		}
		#endregion

		/// <summary> Return it if the cell at index is appearing or UITableViewCellLifeCycle is set to RecycleWhenReloaded. Null will be returned if not. </summary>
		/// <param name="index">Index of cell at</param>
		/// <typeparam name="T">Type of UITableViewCell</typeparam>
		/// <returns>The loaded cell or null</returns>
		/// <exception cref="ArgumentException">Cell at index is not type of T</exception>
		public T GetLoadedCell<T>(int index) where T : UITableViewCell
		{
			if (!_loadedHolders.TryGetValue(index, out var holder)) return null;
			T cell = holder.loadedCell as T;
			if (cell == null) throw new ArgumentException($"Cell at index:{index} is not type of {typeof(T)}");
			return cell;
		}

		public UITableViewCell GetLoadedCell(int index)
		{
			return _loadedHolders.TryGetValue(index, out var holder) ? holder.loadedCell : null;
		}

		public bool TryGetLoadedCell<T>(int index, out T result) where T : UITableViewCell
		{
			result = null;
			if (index < 0 || _holders.Count - 1 < index) return false;
			if (!_loadedHolders.TryGetValue(index, out var holder)) return false;
			var cell = holder.loadedCell as T;
			if (cell == null) return false;
			result = cell;
			return true;
		}

		/// <summary> Return all appearing cells and those whose UITableViewCellLifeCycle is set to RecycleWhenReloaded. </summary>
		public IEnumerable<UITableViewCell> GetAllLoadedCells()
		{
			foreach (var kvp in _loadedHolders)
				yield return kvp.Value.loadedCell;
		}

		public IEnumerable<T> GetAllLoadedCells<T>() where T : UITableViewCell
		{
			return GetAllLoadedCells<T>(_ => true);
		}

		public IEnumerable<T> GetAllLoadedCells<T>(Func<int, bool> condition) where T : UITableViewCell
		{
			foreach (var kvp in _loadedHolders) {
				if (!condition.Invoke(kvp.Key)) continue;
				var tCell = kvp.Value.loadedCell as T;
				if (tCell == null) continue;
				yield return tCell;
			}
		}

		public IEnumerable<UITableViewCell> GetAllIntersectedCells(int withCellIndex)
		{
			var withCell = GetLoadedCell(withCellIndex);
			if (withCell == null) yield break;
			foreach (var kvp in _loadedHolders) {
				if (kvp.Key == withCellIndex) continue;
				if (kvp.Value.loadedCell.rectTransform.CalculateAreaOfIntersection(withCell.worldRect) <= 0f) continue;
				yield return kvp.Value.loadedCell;
			}
		}

		public IEnumerable<T> GetAllIntersectedCells<T>(int withCellIndex) where T : UITableViewCell
		{
			var withCell = GetLoadedCell(withCellIndex);
			if (withCell == null) yield break;
			foreach (var cell in GetAllIntersectedCells(withCellIndex)) {
				var tCell = cell as T;
				if (tCell == null) continue;
				yield return tCell;
			}
		}

		public bool TryFindMostIntersectedCell(int withCellIndex, out int mostIntersectedCellIndex, out float maxAreaOfIntersection)
		{
			mostIntersectedCellIndex = -1;
			maxAreaOfIntersection = 0f;
			var withCell = GetLoadedCell(withCellIndex);
			if (withCell == null) return false;
			foreach (var kvp in _loadedHolders) {
				if (kvp.Key == withCellIndex) continue;
				var area = kvp.Value.loadedCell.rectTransform.CalculateAreaOfIntersection(withCell.worldRect);
				if (maxAreaOfIntersection >= area) continue;
				mostIntersectedCellIndex = kvp.Key;
				maxAreaOfIntersection = area;
			}
			return mostIntersectedCellIndex >= 0;
		}

		public bool TryFindMostIntersectedCell<T>(int withCellIndex, out int mostIntersectedCellIndex, out float maxAreaOfIntersection)
			where T : UITableViewCell
		{
			mostIntersectedCellIndex = -1;
			maxAreaOfIntersection = 0f;
			var withCell = GetLoadedCell(withCellIndex);
			if (withCell == null) return false;
			foreach (var kvp in _loadedHolders) {
				if (kvp.Key == withCellIndex) continue;
				var tCell = kvp.Value.loadedCell as T;
				if (tCell == null) continue;
				var area = tCell.rectTransform.CalculateAreaOfIntersection(withCell.worldRect);
				if (maxAreaOfIntersection >= area) continue;
				mostIntersectedCellIndex = kvp.Key;
				maxAreaOfIntersection = area;
			}
			return mostIntersectedCellIndex >= 0;
		}

		/// <summary> The interface CellAtIndexInTableViewWillAppear(tableView, index) of all loaded cells will be called without recycling them.
		/// WARNING: If you want to resize any cell or change the quantity of them,
		/// use ReloadData() instead because the IUITableViewDataSource's methods will not be called. </summary>
		public void RefreshAllLoadedCells()
		{
			foreach (var kvp in _loadedHolders)
				this.@delegate.CellAtIndexInTableViewWillAppear(this, kvp.Key);
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

		Vector3 TransformPoint(PointerEventData eventData, IUITableViewInteractable interactable)
		{
			var cam = interactable.TableViewCameraForInteractive(this);
			return cam == null ? (Vector3)eventData.position : cam.ScreenToWorldPoint(eventData.position);
		}

		bool TryFindClickedLoadedCell(PointerEventData eventData, IUITableViewInteractable interactable, out UITableViewCell target)
		{
			var position = TransformPoint(eventData, interactable);
			var viewportSize = _viewport.rect.size;
			var tableViewPosition = viewportSize * (Vector2.one - _viewport.pivot) - (Vector2)_viewport.InverseTransformPoint(position);
			var calibrationPoint = new Vector2(tableViewPosition.x / viewportSize.x, tableViewPosition.y / viewportSize.y);
			if (!_direction.IsTopToBottomOrRightToLeft())
				calibrationPoint = Vector2.one - calibrationPoint;
			var cellIndex = FindIndexOfCellAtCalibrationPoint(calibrationPoint, _scrollRect.normalizedPosition);
			int startIndex = cellIndex, endIndex = cellIndex;
			if (_columnPerRowInGrid != null) {
				var e = _holders[cellIndex];
				startIndex -= e.columnIndex;
				endIndex += _columnPerRowInGrid[e.rowIndex] - e.columnIndex - 1;
				endIndex = Mathf.Min(endIndex, _holders.Count - 1);
			}
			for (var i = startIndex; i <= endIndex; i++) {
				if (!_loadedHolders.TryGetValue(i, out var holder)) continue;
				if (!holder.loadedCell.worldRect.Contains(position)) continue;
				target = holder.loadedCell;
				return true;
			}
			target = null;
			return false;
		}

		#region Interfaces of EventSystems
		public virtual void OnPointerDown(PointerEventData eventData)
		{
			_clickCellIndex = null;
			if (this.clickable == null) return;
			if (!TryFindClickedLoadedCell(eventData, this.clickable, out var result)) return;
			this.clickable.TableViewOnPointerDownCellAt(this, result.index.Value, eventData);
			_clickCellIndex = result.index.Value;
		}
		public virtual void OnPointerClick(PointerEventData eventData)
		{
			if (this.clickable == null) return;
			if (TryFindClickedLoadedCell(eventData, this.clickable, out var result) && _clickCellIndex == result.index)
				this.clickable.TableViewOnPointerClickCellAt(this, result.index.Value, eventData);
			_clickCellIndex = null;
		}
		public virtual void OnPointerUp(PointerEventData eventData)
		{
			if (this.clickable == null) return;
			if (!TryFindClickedLoadedCell(eventData, this.clickable, out var result)) return;
			this.clickable.TableViewOnPointerUpCellAt(this, result.index.Value, eventData);
			_clickCellIndex = _clickCellIndex == result.index ? _clickCellIndex : null;
		}

		public virtual void OnBeginDrag(PointerEventData eventData)
		{
			OnBeginDragIfMagnetic();
			OnBeginDragIfFlickable(eventData);
			OnBeginDragIfDraggable(eventData);
		}
		public virtual void OnDrag(PointerEventData eventData)
		{
			OnDragIfDraggable(eventData);
		}
		public virtual void OnEndDrag(PointerEventData eventData)
		{
			OnEndDragIfMagnetic();
			OnEndDragIfFlickable(eventData);
			OnEndDragIfDraggable(eventData);
		}
		
		void OnBeginDragIfMagnetic()
		{
			if (magneticAlignment == null) return;
			StopScroll();
		}
		void OnBeginDragIfFlickable(PointerEventData eventData)
		{
			if (flickable == null) return;
			_flickPositionAt = eventData.position;
			_flickStartAt = Time.time;
		}
		void OnBeginDragIfDraggable(PointerEventData eventData)
		{
			if (!keepClickEvenIfBeginDrag) _clickCellIndex = null;
			_dragCellIndex = null;
			if (this.draggable == null) return;
			if (TryFindClickedLoadedCell(eventData, this.draggable, out var result))
				_dragCellIndex = result.index;
			if (draggable.TableViewDragCellMovable(this) && _dragCellIndex.HasValue)
				result.transform.localPosition = scrollRect.content.InverseTransformPoint(eventData.position);
			this.draggable.TableViewOnBeginDrag(this, _dragCellIndex, eventData);
		}
		bool TryUpdateDragCellIfDraggable(PointerEventData eventData, out int? dragCellIndex)
		{
			if (this.draggable == null) {
				dragCellIndex = _dragCellIndex = null;
				return false;
			}
			dragCellIndex = _dragCellIndex;
			var movable = draggable.TableViewDragCellMovable(this);
			if (movable && dragCellIndex.HasValue) {
				var cell = GetLoadedCell(dragCellIndex.Value);
				if (cell != null) cell.rectTransform.localPosition = scrollRect.content.InverseTransformPoint(TransformPoint(eventData, this.draggable));
			} else if (!movable && TryFindClickedLoadedCell(eventData, this.draggable, out var result))
				dragCellIndex = result.index;
			return true;
		}
		void OnDragIfDraggable(PointerEventData eventData)
		{
			if (!TryUpdateDragCellIfDraggable(eventData, out var dragCellIndex)) return;
			this.draggable.TableViewOnDrag(this, dragCellIndex, eventData);
		}
		void OnEndDragIfMagnetic()
		{
			if (magneticAlignment == null) return;
			_magneticInternalState = UITableViewMagneticInternalState.Scrolling;
		}
		void OnEndDragIfFlickable(PointerEventData eventData)
		{
			if (flickable == null) return;
			if (Time.time - _flickStartAt > flickable.FlickTimeOfTriggerFlickInTableView(this)) return;
			var cellIndex = TryFindClickedLoadedCell(eventData, this.flickable, out var flickedCell) ? flickedCell.index : null;
			var flickedDelta = _direction.IsVertical() ? eventData.position.y - _flickPositionAt.y : eventData.position.x - _flickPositionAt.x;
			var delta = flickable.FlickDistanceRangeOfTriggerFlickInTableView(this);
			if (flickedDelta > delta.lower && flickedDelta < delta.upper)
				flickable.TableViewOnDidFlick(this, cellIndex, _direction.IsVertical() ? UITableViewDirection.BottomToTop : UITableViewDirection.LeftToRight);
			else if (flickedDelta < -delta.lower && flickedDelta > -delta.upper)
				flickable.TableViewOnDidFlick(this, cellIndex, _direction.IsVertical() ? UITableViewDirection.TopToBottom : UITableViewDirection.RightToLeft);
		}
		void OnEndDragIfDraggable(PointerEventData eventData)
		{
			if (!TryUpdateDragCellIfDraggable(eventData, out var dragCellIndex)) return;
			_dragCellIndex = null;
			this.draggable.TableViewOnEndDrag(this, dragCellIndex, eventData);
		}
		#endregion

		class ScrollState
		{
			public bool started { get; private set; }
			public bool stopped => _stopImmediately || Mathf.Approximately(progress, 1f);
			public Vector2 normalizedPosition => new Vector2(Mathf.Lerp(_from.x, _to.x, progress), Mathf.Lerp(_from.y, _to.y, progress));
			float progress => _stopImmediately ? 1f : Mathf.Min((Time.time - _startAt) / _duration, 1f);
			float _startAt, _duration;
			Vector2 _from, _to;
			bool _stopImmediately;
			OnScrollingStopped _onScrollingStopped;
			public void Start(Vector2 from, Vector2 to, float duration, OnScrollingStopped onStopped)
			{
				this.started = true;
				this._onScrollingStopped = onStopped;
				_from = from;
				_to = to;
				_duration = Mathf.Max(duration, 0f);
				_stopImmediately = Mathf.Approximately(_duration, 0f);
				_startAt = Time.time;
			}
			public void Stop()
			{
				if (!started) return;
				this.started = false;
				this._onScrollingStopped?.Invoke(!stopped);
				this._onScrollingStopped = null;
			}
		}
	}
}
