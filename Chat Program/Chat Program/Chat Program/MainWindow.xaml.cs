﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Chat_Program
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private string _sendTextBoxText = string.Empty;
		public string SendTextBoxText
		{
			get => _sendTextBoxText;
			set
			{
				if (_sendTextBoxText != value)
				{
					_sendTextBoxText = value;
					OnPropertyChanged("SendTextBoxText");
				}
			}
		}


		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		
		private ChatClient ChatClient { get; }
		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;

			ChatClient = new ChatClient(1024, OnReceiveMessage);


			while (!ChatClient.Connect(IPAddress.Parse("127.0.0.1"), 5000))
			{
				Thread.Sleep(1000);
			}

			ChatClient.StartListeningForMessages();
		}

		private void SendMessage(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}	

			if (ChatClient.TrySendString(message))
			{
				SendTextBoxText = string.Empty;

				ConversationMessage conversationMessage = new ConversationMessage(message, "Sent", "Now", Visibility.Collapsed);
				Globals.ConversationMessages.Add(conversationMessage);
			}
		}

		#region Event Handlers
		private void OnReceiveMessage(Message message)
		{
			ConversationMessage conversationMessage;

			switch (message.ResponseType)
			{
				case ResponseType.StringMessage:
					conversationMessage = new ConversationMessage(message.StringMessage, "Received", "Now", Visibility.Collapsed);
					break;

				case ResponseType.Image:
				case ResponseType.Audio:
				default:
					return;
			}

			Globals.ConversationMessages.Add(conversationMessage);
		}

		private void SendTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (sender is TextBox textBox)
			{
				// Force updating value of SendTextBoxText
				textBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();

				bool minLengthAdded = false;

				// Checking if the changes could plausibly contain a newline
				foreach (var change in e.Changes)
				{
					if (change.AddedLength >= Environment.NewLine.Length)
					{
						minLengthAdded = true;
						break;
					}
				}

				// Only send message if enter was pressed
				if (minLengthAdded && SendTextBoxText.EndsWith(Environment.NewLine))
				{
					SendTextBoxText = SendTextBoxText.Substring(0, SendTextBoxText.Length - Environment.NewLine.Length);
					SendMessage(SendTextBoxText);
				}
			}
		}

		private void SendButton_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(SendTextBoxText))
			{
				SendMessage(SendTextBoxText);
			}
		}
		#endregion
	}
}
