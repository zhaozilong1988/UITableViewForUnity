using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UIKit.Samples
{
	public class SnsScene : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate, IUITableViewMargin, IUITableViewReachable
	{
		[SerializeField] Text _textSizeCalculator;
		[SerializeField] UITableView _tableView;
		[SerializeField] SnsCell _cellPrefab;
		[SerializeField] Image _indicator;

		bool _indicatorVisible;
		readonly List<CellData> _chats = new List<CellData>();
		const int FONT_SIZE = 30;
		const float MSG_TEXT_WIDTH = 520f;
		readonly IReadOnlyList<string> _replies = new List<string>() {
			"What are you talking about? I'm not an AI, I don't know what are you talking about! What are you talking about? I'm not an AI, I don't know what are you talking about! What are you talking about? I'm not an AI, I don't know what are you talking about!",
			"Sorry, I'm not a human, I'm a stupid computer program.",
			"Please don't ask me anything, I'm just a stupid computer program.",
		};

		void Start()
		{
			// Tell the table view that this class will provide that the data it needs
			_tableView.dataSource = this;
			// Tell the table view that this class will respond to its delegate methods
			_tableView.@delegate = this;
			// Tell the table view that this class will provide margin between cells
			_tableView.marginDataSource = this;
			_tableView.reachable = this;

			var msg = "Hi! Type something and click send button.";
			var size = CalculateTextSize(msg, FONT_SIZE);
			_chats.Add(new CellData(msg, MSG_TEXT_WIDTH, size.y));
			
			msg = _replies[Random.Range(0, _replies.Count)];
			size = CalculateTextSize(msg, FONT_SIZE);
			_chats.Add(new CellData(msg, MSG_TEXT_WIDTH, size.y));
			
			msg = _replies[Random.Range(0, _replies.Count)];
			size = CalculateTextSize(msg, FONT_SIZE);
			_chats.Add(new CellData(msg, MSG_TEXT_WIDTH, size.y));
			
			msg = _replies[Random.Range(0, _replies.Count)];
			size = CalculateTextSize(msg, FONT_SIZE);
			_chats.Add(new CellData(msg, MSG_TEXT_WIDTH, size.y));

			_tableView.scrollRect.onValueChanged.AddListener(OnNormalizedPositionChanged);
			// Reload the table view to refresh UI
			_tableView.ReloadData();
		}

		void OnDestroy()
		{
			_tableView.scrollRect.onValueChanged.RemoveListener(OnNormalizedPositionChanged);
		}

		void OnNormalizedPositionChanged(Vector2 normalizedPos)
		{
			if (!_indicatorVisible && normalizedPos.y < 0f) {
				_indicatorVisible = true;
			} else if (_indicatorVisible && normalizedPos.y >= 0f) {
				_indicatorVisible = false;
			}

			_indicator.enabled = _indicatorVisible;
		}

		Vector2 CalculateTextSize(string text, int fontSize)
		{
			_textSizeCalculator.fontSize = fontSize;
			var size = _textSizeCalculator.rectTransform.sizeDelta;
			size.x = MSG_TEXT_WIDTH;
			_textSizeCalculator.rectTransform.sizeDelta = size;
			_textSizeCalculator.text = text;
			return new Vector2(Mathf.Min(size.x, _textSizeCalculator.preferredWidth), _textSizeCalculator.preferredHeight);
		}

		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			return tableView.ReuseOrCreateCell(_cellPrefab);
		}

		public int NumberOfCellsInTableView(UITableView tableView)
		{
			return _chats.Count;
		}

		public float LengthForCellInTableView(UITableView tableView, int index)
		{
			return _chats[index].cellHeight;
		}

		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
		{
			var chat = _chats[index];
			_tableView.GetLoadedCell<SnsCell>(index).UpdateData("name", chat.size, chat.message, FONT_SIZE);
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
		{
			_tableView.GetLoadedCell<SnsCell>(index).UnloadData();
		}

		public float LengthForUpperMarginInTableView(UITableView tableView, int rowIndex)
		{
			return 0f;
		}

		public float LengthForLowerMarginInTableView(UITableView tableView, int rowIndex)
		{
			return 0f;
		}

		public void TableViewReachedTopmostOrRightmost(UITableView tableView)
		{
			
		}

		public void TableViewReachedBottommostOrLeftmost(UITableView tableView)
		{
			
			Debug.Log("TableViewReachedBottommostOrLeftmost" + tableView.scrollRect.normalizedPosition);
			
		}

		public void TableViewLeftTopmostOrRightmost(UITableView tableView)
		{
			
		}

		public void TableViewLeftBottommostOrLeftmost(UITableView tableView)
		{
			var msg = _replies[Random.Range(0, _replies.Count)];
			var size = CalculateTextSize(msg, FONT_SIZE);
			_chats.Add(new CellData(msg, MSG_TEXT_WIDTH, size.y));
			_tableView.AppendData();
			_tableView.ScrollToCellAt(_chats.Count - 2, alignment: UITableViewAlignment.LeftOrBottom, withMargin:true);
		}

		public float TableViewReachableEdgeTolerance(UITableView tableView)
		{
			return -100f;//UITableView.DEFAULT_REACHABLE_EDGE_TOLERANCE;
		}

		class CellData
		{
			public string message;
			public float cellHeight;
			public Vector2 size;

			public CellData(string message, float textWidth, float textHeight)
			{
				this.message = message;
				cellHeight = textHeight + 450f; // 20f padding on each side of bubble background
				size = new Vector2(textWidth, cellHeight);
			}
		}
	}
}