﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using com.vtcsecure.ace.windows.Model;
using com.vtcsecure.ace.windows.Services;
using com.vtcsecure.ace.windows.ViewModel;
using VATRP.Core.Extensions;
using VATRP.Core.Model;

namespace com.vtcsecure.ace.windows.Views
{
    /// <summary>
    /// Interaction logic for MediaTextWindow.xaml
    /// </summary>
    public partial class MediaTextWindow
    {
        private MessagingViewModel _viewModel;
        public MediaTextWindow(MessagingViewModel vm) : base(VATRPWindowType.MESSAGE_VIEW)
        {
            _viewModel = vm;
            DataContext = vm;
            InitializeComponent();
            ServiceManager.Instance.ChatService.ConversationUpdated += ChatManagerOnConversationUpdated;
        }

        private void ChatManagerOnConversationUpdated(object sender, VATRP.Core.Events.ConversationUpdatedEventArgs e)
        {
            ScrollToEnd();
        }

        private void ScrollToEnd()
        {
            if (ServiceManager.Instance.Dispatcher.Thread != Thread.CurrentThread)
            {
                ServiceManager.Instance.Dispatcher.BeginInvoke((Action)(ScrollToEnd));
                return;
            }

            MessageListView.SelectedIndex = MessageListView.Items.Count - 1;
            MessageListView.ScrollIntoView(MessageListView.SelectedItem);
        }

        private void OnChatSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ContactsList.SelectedItem != null)
            {
                var contactModel = ContactsList.SelectedItem as ContactViewModel;

                if (contactModel != null)
                {
                    _viewModel.SetActiveChatContact(contactModel.Contact, IntPtr.Zero);
                    ScrollToEnd();
                }
            }
        }

        private void OnSendButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!ServiceManager.Instance.IsRttAvailable)
            {
                _viewModel.SendMessage(_viewModel.MessageText);
            }
            else
            {
                _viewModel.EnqueueInput("\r");
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            Char inputKey = Char.MinValue;
            switch (e.Key)
            {
                case Key.None:
                    break;
                case Key.Back:
                    inputKey = '\b';
                    break;
                case Key.Tab:
                    inputKey = '\t';
                    break;
                case Key.LineFeed:
                    inputKey = '\n';
                    break;
                case Key.Clear:
                    break;
                case Key.Return:
                    inputKey = '\r';
                    break;
                case Key.Space:
                    inputKey = ' ';
                    break;
                default:
                    break;
            }
            if (inputKey != Char.MinValue)
            {
                _viewModel.EnqueueInput(inputKey.ToString());
            }
        }

        private void OnTextInpput(object sender, TextCompositionEventArgs e)
        {
            _viewModel.EnqueueInput(e.Text);
            if (!ServiceManager.Instance.IsRttAvailable )
            {
                if (e.Text.Length > 0 && e.Text[e.Text.Length - 1] == '\r')
                    _viewModel.SendMessage(_viewModel.MessageText);
                return;
            }
        }
       
    }
}
