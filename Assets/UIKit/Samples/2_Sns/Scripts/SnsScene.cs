using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class SnsScene : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate, IUITableViewMargin, IUITableViewReachable
	{
		[SerializeField] Text _textSizeCalculator;
		[SerializeField] UITableView _tableView;
		[SerializeField] UITableViewCell _cellPrefab;

		readonly List<Chat> _chats = new List<Chat>();
		const int FONT_SIZE = 30;
		readonly IReadOnlyList<string> _replies = new List<string>() {
			"What are you talking about? I'm not an AI, I don't know what are you talking about!",
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
			// _tableView.reachable = this;

			var firsttMsg = "Hi! Type something and click send button.";
			var size = CalculateTextSize(firsttMsg, FONT_SIZE);
			_chats.Add(new Chat(false, firsttMsg, size.x, size.y));
			_chats.Add(new Chat(false, _replies[Random.Range(0, _replies.Count-1)], size.x, size.y));

			// Reload the table view to refresh UI
			_tableView.ReloadData();
		}

		Vector2 CalculateTextSize(string text, int fontSize)
		{
			_textSizeCalculator.fontSize = fontSize;
			var size = _textSizeCalculator.rectTransform.sizeDelta;
			size.x = ((RectTransform)(_tableView.scrollRect.transform)).rect.width * 2f / 3f;
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
			return _chats[index].bubbleHeight;
		}

		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
		{
			var chat = _chats[index];
			_tableView.GetLoadedCell<SnsCell>(index).UpdateData("name", chat.message, FONT_SIZE);
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
			throw new System.NotImplementedException();
		}

		public void TableViewReachedBottommostOrLeftmost(UITableView tableView)
		{
			throw new System.NotImplementedException();
		}

		public void TableViewLeftTopmostOrRightmost(UITableView tableView)
		{
			throw new System.NotImplementedException();
		}

		public void TableViewLeftBottommostOrLeftmost(UITableView tableView)
		{
			throw new System.NotImplementedException();
		}

		public float TableViewReachableEdgeTolerance(UITableView tableView)
		{
			return UITableView.DEFAULT_REACHABLE_EDGE_TOLERANCE;
		}

		class Chat
		{
			public string message;
			public float bubbleWidth;
			public float bubbleHeight;

			public Chat(bool isMine, string message, float textWidth, float textHeight)
			{
				this.message = message;
				bubbleWidth = textWidth + 70f; // padding of bubble background
				bubbleHeight = textHeight + 450f; // 20f padding on each side of bubble background
			}
		}
	}
}