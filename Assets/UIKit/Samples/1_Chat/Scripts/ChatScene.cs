using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class ChatScene : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate, IUITableViewMargin
	{
		[SerializeField] InputField _chatInput;
		[SerializeField] Text _textSizeCalculator;
		[SerializeField] UITableView _tableView;
		[SerializeField] UITableViewCell _myChatCellPrefab;
		[SerializeField] UITableViewCell _othersChatCellPrefab;

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

			var firsttMsg = "Hi! Type something and click send button.";
			var size = CalculateTextSize(firsttMsg, FONT_SIZE);
			_chats.Add(new Chat(false, firsttMsg, size.x, size.y));

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

		public void OnClickSend()
		{
			if (string.IsNullOrEmpty(_chatInput.text))
				return;

			var size = CalculateTextSize(_chatInput.text, FONT_SIZE);
			_chats.Insert(0, new Chat(true, _chatInput.text, size.x, size.y));

			string msg;
			if (_chatInput.text.ToLower().Contains("hello") || _chatInput.text.ToLower().Contains("hi")) {
				msg = "Hi! I'm here.";
			} else {
				msg = _replies[Random.Range(0, _replies.Count-1)];
			}
			size = CalculateTextSize(msg, FONT_SIZE);
			_chats.Insert(0, new Chat(false, msg, size.x, size.y));

			_chatInput.text = string.Empty;

			// Tell the table view that the data has prepended to the list
			_tableView.PrependData();
			// Scroll to the latest message
			_tableView.ScrollToCellAt(0, withMargin:true, duration:0.1f);
		}

		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			var chat = _chats[index];
			var cell = chat.isMine // Specify which cell the table view should use.
				? tableView.ReuseOrCreateCell(_myChatCellPrefab.name, _myChatCellPrefab) 
				: tableView.ReuseOrCreateCell(_othersChatCellPrefab.name, _othersChatCellPrefab);
			return cell;
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
			_tableView.GetLoadedCell<ChatCell>(index).UpdateData(chat.message, FONT_SIZE, chat.bubbleWidth, chat.bubbleHeight);
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
		{
			_tableView.GetLoadedCell<ChatCell>(index).UnloadData();
		}

		public float LengthForUpperMarginInTableView(UITableView tableView, int rowIndex)
		{
			return 20f;
		}

		public float LengthForLowerMarginInTableView(UITableView tableView, int rowIndex)
		{
			return 20f;
		}

		class Chat
		{
			public bool isMine;
			public string message;
			public float bubbleWidth;
			public float bubbleHeight;

			public Chat(bool isMine, string message, float textWidth, float textHeight)
			{
				this.isMine = isMine;
				this.message = message;
				bubbleWidth = textWidth + 70f; // padding of bubble background
				bubbleHeight = textHeight + 40f; // 20f padding on each side of bubble background
			}
		}
	}
}