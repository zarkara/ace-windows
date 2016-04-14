using System;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using com.vtcsecure.ace.windows.Services;
using VATRP.Core.Interfaces;
using VATRP.Core.Model;
using System.Windows.Threading;
using com.vtcsecure.ace.windows.CustomControls;
using com.vtcsecure.ace.windows.Views;
using log4net;
using System.IO;
using System.Windows.Media.Imaging;

namespace com.vtcsecure.ace.windows.ViewModel
{
    public class CallViewModel : ViewModelBase, IEquatable<CallViewModel>, IEquatable<VATRPCall>
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(CallViewModel));
        private bool _visualizeRing;
        private bool _visualizeIncoming;
        private int _ringCount;
        private bool _showInfo;
        private VATRPCallState _callState;
        private string _displayName;
        private string _remoteNumber;
        private int _callDuration;
        private int _autoAnswer;
        private ILinphoneService _linphoneService;
        private bool _hasVideo;
        private double _displayNameSize;
        private double _remotePartyTextSize;
        private double _infoTextSize;
        private VATRPCall _currentCall = null;
        private readonly System.Timers.Timer ringTimer;
        private readonly System.Timers.Timer autoAnswerTimer;
        private bool subscribedForStats;
        private System.Timers.Timer timerCall;
        private CallInfoViewModel _callInfoViewModel;
        private bool _showIncomingCallPanel;
        private bool _showOutgoingCallPanel;
        private SolidColorBrush _ringCounterBrush;
        private bool _isVideoOn;
        private bool _isMuteOn;
        private bool _isSpeakerOn;
        private bool _isNumpadOn;
        private bool _isRttOn;
        private bool _isRttEnabled;
        private bool _isInfoOn;
        private bool _isCallOnHold;
        private int _videoWidth;
        private int _videoHeight;

        private bool _savedIsVideoOn;
        private bool _savedIsMuteOn;
        private bool _savedIsSpeakerOn;
        private bool _savedIsNumpadOn;
        private bool _savedIsRttOn;
        private bool _savedIsInfoOn;
        private bool _savedIsCallHoldOn;
        private string _errorMessage;
        private ImageSource _avatar;
        public bool PauseRequest;
        public bool ResumeRequest;
        public bool AllowHideContorls;
        public Visibility CommandbarLastTimeVisibility;
        public Visibility NumpadLastTimeVisibility;
        public Visibility CallInfoLastTimeVisibility;
        public Visibility CallSwitchLastTimeVisibility;
        private bool _isFullScreenOn;
        private bool _showAvatar;
        private bool _showDeclineMenu;
        private string _declinedMessage;
        private bool _showRingingTimer;
        private bool _showDeclinedMessage;
        private VATRPContact _contact;

        public event CallInfoViewModel.CallQualityChangedDelegate CallQualityChangedEvent;

        public CallViewModel()
        {
            _visualizeRing = false;
            _visualizeIncoming = false;
            _declinedMessage = string.Empty;
            _showDeclineMenu = false;
            _showRingingTimer = true;
            _callState = VATRPCallState.None;
            _hasVideo = true;
            _displayNameSize = 30;
            _remotePartyTextSize = 25;
            _infoTextSize = 20;
            subscribedForStats = false;
            Declined = false;
            _errorMessage = String.Empty;
            AllowHideContorls = false;
            WaitForDeclineMessage = false;
            CommandbarLastTimeVisibility = Visibility.Hidden;
            NumpadLastTimeVisibility = Visibility.Hidden;
            CallInfoLastTimeVisibility = Visibility.Hidden;
            CallSwitchLastTimeVisibility = Visibility.Hidden;

            // initialize based on stored settings:
            if (App.CurrentAccount != null)
            {
                _savedIsVideoOn = App.CurrentAccount.ShowSelfView;
                _isVideoOn = App.CurrentAccount.ShowSelfView;
                _savedIsMuteOn = App.CurrentAccount.MuteMicrophone;
                _isMuteOn = App.CurrentAccount.MuteMicrophone;
                _isSpeakerOn = App.CurrentAccount.MuteSpeaker;
                _savedIsSpeakerOn = App.CurrentAccount.MuteSpeaker;
            }

            timerCall = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = true
            };
            timerCall.Elapsed += OnUpdatecallTimer;

            ringTimer = new System.Timers.Timer
            {
                Interval = 1800,
                AutoReset = true
            };
            ringTimer.Elapsed += OnUpdateRingCounter;

//#if DEBUG
            autoAnswerTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = true
            };
            autoAnswerTimer.Elapsed += OnAutoAnswerTimer;
//#endif
            _callInfoViewModel = new CallInfoViewModel();
        }

        public CallViewModel(ILinphoneService linphoneSvc, VATRPCall call) : this()
        {
            _linphoneService = linphoneSvc;
            _currentCall = call;
            _currentCall.CallStartTime = DateTime.Now;
            _currentCall.CallEstablishTime = DateTime.MinValue;
            // initialize based on stored settings:
            if (App.CurrentAccount != null)
            {
                _savedIsVideoOn = App.CurrentAccount.ShowSelfView;
                _isVideoOn = App.CurrentAccount.ShowSelfView;
                _savedIsMuteOn = App.CurrentAccount.MuteMicrophone;
                _isMuteOn = App.CurrentAccount.MuteMicrophone;
                _isSpeakerOn = App.CurrentAccount.MuteSpeaker;
                _savedIsSpeakerOn = App.CurrentAccount.MuteSpeaker;
            }


        }

        #region Properties

        public bool Declined { get; set; }

        public bool VisualizeIncoming
        {
            get { return _visualizeIncoming; }
            set
            {
                _visualizeIncoming = value;
                OnPropertyChanged("VisualizeIncoming");
            }
        }

        public bool VisualizeRinging
        {
            get { return _visualizeRing; }
            set
            {
                _visualizeRing = value;
                OnPropertyChanged("VisualizeRinging");
            }
        }

        public SolidColorBrush RingCounterBrush
        {
            get { return _ringCounterBrush; }
            set
            {
                _ringCounterBrush = value;
                OnPropertyChanged("RingCounterBrush");
            }
        }

        public int RingCount
        {
            get { return _ringCount; }
            set
            {
                _ringCount = value;
                OnPropertyChanged("RingCount");
            }
        }

        public bool ShowInfo
        {
            get { return _showInfo; }
            set
            {
                if (_showInfo != value)
                {
                    _showInfo = value;
                    OnPropertyChanged("ShowInfo");
                }
            }
        }

        public bool ShowAvatar
        {
            get { return _showAvatar; }
            set
            {
                _showAvatar = value;
                OnPropertyChanged("ShowAvatar");
            }
        }

        public VATRPCall ActiveCall
        {
            get { return _currentCall; }

            set { _currentCall = value; }
        }

        public VATRPCallState CallState
        {
            get { return _callState; }

            set
            {
                _callState = value;
                OnPropertyChanged("CallState");
            }
        }

        public string DisplayName
        {
            get { return _displayName; }
            set
            {
                _displayName = value;
                OnPropertyChanged("DisplayName");
                OnPropertyChanged("CallerInfo");
            }
        }

        public string RemoteNumber
        {
            get { return _remoteNumber; }
            set
            {
                _remoteNumber = value;
                OnPropertyChanged("RemoteNumber");
                OnPropertyChanged("CallerInfo");
            }
        }

        public string CallerInfo
        {
            get
            {
                if (string.IsNullOrEmpty(DisplayName))
                    return RemoteNumber;
                return DisplayName ?? string.Empty;
            }
        }

        public int CallDuration
        {
            get
            {
                if (_currentCall.CallEstablishTime == DateTime.MinValue)
                    return 0;
                return (int)(DateTime.Now - _currentCall.CallEstablishTime).TotalSeconds;
            }
        }
        public int RingDuration
        {
            get { return (int)(DateTime.Now - _currentCall.CallStartTime).TotalSeconds; }
        }

        public bool ShowRingingTimer
        {
            get { return _showRingingTimer; }
            set
            {
                _showRingingTimer = value; 
                OnPropertyChanged("ShowRingingTimer");
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }

        public int AutoAnswer
        {
            get { return _autoAnswer; }
            set
            {
                _autoAnswer = value;
                OnPropertyChanged("AutoAnswer");
            }
        }

        public bool HasVideo
        {
            get { return _hasVideo; }
            set
            {
                _hasVideo = value;
                OnPropertyChanged("HasVideo");
            }
        }

        public bool ShowIncomingCallPanel
        {
            get { return _showIncomingCallPanel; }
            set
            {
                _showIncomingCallPanel = value;
                OnPropertyChanged("ShowIncomingCallPanel");
                OnPropertyChanged("ShowCallParams");
            }
        }

        public bool ShowCallParams
        {
            get { return ShowIncomingCallPanel || ShowOutgoingEndCall; }
        }

        public bool ShowOutgoingEndCall
        {
            get { return !ShowInfo && _showOutgoingCallPanel; }
            set
            {
                _showOutgoingCallPanel = value;
                OnPropertyChanged("ShowOutgoingEndCall");
                OnPropertyChanged("ShowCallParams");
            }
        }

        public bool IsVideoOn
        {
            get { return _isVideoOn; }
            set
            {
                _isVideoOn = value;
                OnPropertyChanged("IsVideoOn");
            }
        }

        public bool IsMuteOn
        {
            get { return _isMuteOn; }
            set
            {
                _isMuteOn = value;
                OnPropertyChanged("IsMuteOn");
            }
        }

        public bool IsSpeakerOn
        {
            get { return _isSpeakerOn; }
            set
            {
                _isSpeakerOn = value;
                OnPropertyChanged("IsSpeakerOn");
            }
        }

        public bool IsNumpadOn
        {
            get { return _isNumpadOn; }
            set
            {
                _isNumpadOn = value;
                OnPropertyChanged("IsNumpadOn");
            }
        }

        public bool IsRttOn
        {
            get { return _isRttOn; }
            set
            {
                _isRttOn = value;
                OnPropertyChanged("IsRttOn");
            }
        }
		
        public bool IsRTTEnabled
        {
            get { return _isRttEnabled; }
            set
            {
                _isRttEnabled = value;
                OnPropertyChanged("IsRTTEnabled");
            }
        }
		
        public bool IsCallInfoOn
        {
            get { return _isInfoOn; }
            set
            {
                _isInfoOn = value;
                OnPropertyChanged("IsCallInfoOn");
            }
        }

        public bool IsFullScreenOn
        {
            get { return _isFullScreenOn; }
            set
            {
                _isFullScreenOn = value;
                OnPropertyChanged("IsFullScreenOn");
            }
        }
		
        public bool IsCallOnHold
        {
            get { return _isCallOnHold; }
            set
            {
                _isCallOnHold = value;
                OnPropertyChanged("IsCallOnHold");
            }
        }
        public double DisplayNameSize
        {
            get { return _displayNameSize; }
            set
            {
                _displayNameSize = value;
                OnPropertyChanged("DisplayNameSize");
            }
        }

        public double RemotePartyTextSize
        {
            get { return _remotePartyTextSize; }
            set
            {
                _remotePartyTextSize = value;
                OnPropertyChanged("RemotePartyTextSize");
            }
        }

        public double InfoTextSize
        {
            get { return _infoTextSize; }
            set
            {
                _infoTextSize = value;
                OnPropertyChanged("InfoTextSize");
            }
        }

        public int VideoWidth
        {
            get { return _videoWidth; }
            set
            {
                _videoWidth = value;
                OnPropertyChanged("VideoWidth");
            }
        }

        public int VideoHeight
        {
            get { return _videoHeight; }
            set
            {
                _videoHeight = value;
                OnPropertyChanged("VideoHeight");
            }
        }

        public CallInfoViewModel CallInfoModel
        {
            get { return _callInfoViewModel; }
        }

        public CallInfoView CallInfoCtrl { get; set; }

        public bool SavedIsVideoOn
        {
            get { return _savedIsVideoOn; }
            set { _savedIsVideoOn = value; }
        }

        public bool SavedIsMuteOn
        {
            get { return _savedIsMuteOn; }
            set { _savedIsMuteOn = value; }
        }

        public bool SavedIsSpeakerOn
        {
            get { return _savedIsSpeakerOn; }
            set { _savedIsSpeakerOn = value; }
        }

        public bool SavedIsNumpadOn
        {
            get { return _savedIsNumpadOn; }
            set { _savedIsNumpadOn = value; }
        }

        public bool SavedIsRttOn
        {
            get { return _savedIsRttOn; }
            set { _savedIsRttOn = value; }
        }

        public bool SavedIsInfoOn
        {
            get { return _savedIsInfoOn; }
            set { _savedIsInfoOn = value; }
        }

        public bool SavedIsCallHoldOn
        {
            get { return _savedIsCallHoldOn; }
            set { _savedIsCallHoldOn = value; }
        }

        public ImageSource Avatar
        {
            get { return _avatar; }
            set
            {
                _avatar = value;
                OnPropertyChanged("Avatar");
            }
        }
        public bool ShowDeclineMenu
        {
            get { return _showDeclineMenu; }
            set
            {
                _showDeclineMenu = value;
                OnPropertyChanged("ShowDeclineMenu");
            }
        }

        public bool ShowDeclinedMessage
        {
            get { return _showDeclinedMessage; }
            set
            {
                _showDeclinedMessage = value;
                OnPropertyChanged("ShowDeclinedMessage");
            }
        }

        public string DeclinedMessage
        {
            get { return _declinedMessage; }
            set
            {
                if (_declinedMessage != value)
                {
                    _declinedMessage = value;
                    OnPropertyChanged("DeclinedMessage");
                }
            }
        }

        public VATRPContact Contact
        {
            get { return _contact; }
            set
            {
                _contact = value;
                OnPropertyChanged("Contact");
            }
        }

        public bool WaitForDeclineMessage { get; set; }

        #endregion

        #region Methods

        private void OnUpdatecallTimer(object sender, ElapsedEventArgs e)
        {
            if (ServiceManager.Instance.Dispatcher != null)
            {
                if (ServiceManager.Instance.Dispatcher.Thread != Thread.CurrentThread)
                {
                    ServiceManager.Instance.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new EventHandler<ElapsedEventArgs>(OnUpdatecallTimer), sender, new object[] {e});
                    return;
                }
                OnPropertyChanged("RingDuration");
                timerCall.Start();
            }
        }

        private void OnUpdateRingCounter(object sender, ElapsedEventArgs e)
        {
            if (ServiceManager.Instance.Dispatcher != null)
            {
                if (ServiceManager.Instance.Dispatcher.Thread != Thread.CurrentThread)
                {
                    ServiceManager.Instance.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new EventHandler<ElapsedEventArgs>(OnUpdateRingCounter), sender, new object[] {e});
                    return;
                }
                RingCount++;
                ringTimer.Start();
            }
        }

        private void OnAutoAnswerTimer(object sender, ElapsedEventArgs e)
        {
            if (ServiceManager.Instance.Dispatcher != null)
            {
                if (ServiceManager.Instance.Dispatcher.Thread != Thread.CurrentThread)
                {
                    ServiceManager.Instance.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new EventHandler<ElapsedEventArgs>(OnAutoAnswerTimer), sender, new object[] {e});
                    return;
                }

                if (AutoAnswer > 0)
                {
                    AutoAnswer--;

                    if (AutoAnswer == 0)
                    {
                        autoAnswerTimer.Stop();
                        bool muteMicrophone = false;
                        bool muteSpeaker = false;
                        //bool enableVideo = true;
                        if (App.CurrentAccount != null)
                        {
                            muteMicrophone = App.CurrentAccount.MuteMicrophone;
                            muteSpeaker = App.CurrentAccount.MuteSpeaker;
                            //enableVideo = App.CurrentAccount.EnableVideo;
                        }
                        //Hide();
                        _linphoneService.AcceptCall(_currentCall.NativeCallPtr,
                            ServiceManager.Instance.ConfigurationService.Get(Configuration.ConfSection.GENERAL,
                                Configuration.ConfEntry.USE_RTT, true), muteMicrophone, muteSpeaker, true);
                    }
                }
            }
        }


        private void StopAnimation()
        {
            VisualizeRinging = false;
            VisualizeIncoming = false;
            if (ringTimer.Enabled)
                ringTimer.Stop();
        }

        internal void TerminateCall()
        {
            if (_currentCall != null)
                _linphoneService.TerminateCall(_currentCall.NativeCallPtr);
        }

        internal void MuteSpeaker(bool isMuted)
        {
            _linphoneService.MuteSpeaker(isMuted);
            //            _linphoneService.ToggleMute();
            //            IsMuteOn = _linphoneService.IsCallMuted();

            // TM. VATRP-2219, in-call selections should not update the app settings choices
            //if (App.CurrentAccount != null)
            //{
            //    App.CurrentAccount.MuteSpeaker = isMuted;
            //    // this now needs to be able to update the unified settings control as well.
            //    ServiceManager.Instance.SaveAccountSettings();
            //}
        }

        internal bool MuteCall(bool isMuted)
        {
            if (ActiveCall != null && (ActiveCall.CallState != VATRPCallState.LocalPaused &&
                                       ActiveCall.CallState != VATRPCallState.LocalPausing))
            {
                _linphoneService.MuteCall(isMuted);
                return true;
            }
            return false;
//            _linphoneService.ToggleMute();
//            IsMuteOn = _linphoneService.IsCallMuted();

            // TM. VATRP-2219, in-call selections should not update the app settings choices
            //if (App.CurrentAccount != null)
            //{
            //    App.CurrentAccount.MuteMicrophone = isMuted;
            //    // this now needs to be able to update the unified settings control as well.
            //    ServiceManager.Instance.SaveAccountSettings();
            //}
        }

        internal void ToggleCallStatisticsInfo(bool bShow)
        {
            if (CallInfoCtrl != null)
            {
                if (!bShow)
                    CallInfoCtrl.Hide();
                else
                    CallInfoCtrl.Show();
            }
        }

        internal void LoadAvatar(string path)
        {
            if (string.IsNullOrEmpty(path) || _avatar != null) 
                return;
            try
            {
                byte[] data = File.ReadAllBytes(path);
                var source = new BitmapImage();
                source.BeginInit();
                source.StreamSource = new MemoryStream(data);
                source.EndInit();

                Avatar = source;
                // use public setter
            }
            catch (Exception ex)
            {
                    
            }
        }

        #endregion

        #region Events


        internal void OnTrying()
        {
            DisplayName = _currentCall.To.DisplayName;
            RemoteNumber = _currentCall.To.Username;
            AutoAnswer = 0;
            ShowIncomingCallPanel = false;
            ShowOutgoingEndCall = true;
            ShowAvatar = true;
        }

        internal void OnRinging()
        {
            DisplayName = _currentCall.To.DisplayName;
            RemoteNumber = _currentCall.To.Username;
            ShowIncomingCallPanel = false;
            ShowOutgoingEndCall = true;
            AutoAnswer = 0;
            ShowAvatar = true;
            VisualizeIncoming = false;
            if (timerCall != null)
            {
                if (!timerCall.Enabled)
                    timerCall.Start();
            }
            CallState = VATRPCallState.Ringing;
            if (!VisualizeRinging)
            {
                RingCounterBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xD8, 0x1C, 0x1C));
                VisualizeRinging = true;
                RingCount = 1;
                if (ringTimer != null)
                {
                    if (ringTimer.Enabled)
                        ringTimer.Stop();
                    ringTimer.Interval = 4000;
                    ringTimer.Start();
                }
            }
        }

        internal void ToggleVideo(bool videoOn)
        {
            _linphoneService.ToggleVideo(videoOn, _currentCall.NativeCallPtr);
        }

        internal void OnIncomingCall()
        {
            AutoAnswer = 0;
            DisplayName = _currentCall.From.DisplayName;
            RemoteNumber = _currentCall.From.Username;
            ShowOutgoingEndCall = false;
            CallState = VATRPCallState.InProgress;
            ShowAvatar = true;
            ShowDeclineMenu = false;
//#if DEBUG
            bool isUserAgent = false;
            if (App.CurrentAccount != null)
            {
                isUserAgent = App.CurrentAccount.UserNeedsAgentView;
            }
            if ((ServiceManager.Instance.ConfigurationService.Get(Configuration.ConfSection.GENERAL,
                Configuration.ConfEntry.AUTO_ANSWER, false) || isUserAgent) && 
                (ServiceManager.Instance.LinphoneService.GetActiveCallsCount == 1) )
            {
                if (autoAnswerTimer != null)
                {
                    var autoAnswerDuration =
                        ServiceManager.Instance.ConfigurationService.Get(Configuration.ConfSection.GENERAL,
                            Configuration.ConfEntry.AUTO_ANSWER_AFTER, 2);
                    if (autoAnswerDuration > 60)
                        autoAnswerDuration = 60;
                    else if (autoAnswerDuration < 0)
                        autoAnswerDuration = 0;

                    AutoAnswer = autoAnswerDuration;
                    if (autoAnswerDuration > 0)
                        autoAnswerTimer.Start();
                }
            }
//#endif
            if (timerCall != null)
            {
                if (!timerCall.Enabled)
                    timerCall.Start();
            }

            VisualizeIncoming = true;
            if (!VisualizeRinging)
            {
                RingCounterBrush = new SolidColorBrush(Colors.White);
                VisualizeRinging = true;
                RingCount = 1;
                if (ringTimer != null)
                {
                    if (ringTimer.Enabled)
                        ringTimer.Stop();
                    ringTimer.Interval = 1800;
                    ringTimer.Start();
                }
            }
        }

        internal void OnEarlyMedia()
        {

        }

        internal void OnConnected()
        {
            if (timerCall != null && timerCall.Enabled)
                timerCall.Stop();

            AllowHideContorls = true;
            StopAnimation();

            if (_currentCall.CallEstablishTime == DateTime.MinValue)
                _currentCall.CallEstablishTime = DateTime.Now;

            CallState = VATRPCallState.Connected;
            ShowIncomingCallPanel = false;
            ShowDeclineMenu = false;
            IsMuteOn = _linphoneService.IsCallMuted();
            ShowInfo = true;
            ShowAvatar = false;
        }

        internal void OnStreamRunning()
        {
            CallState = VATRPCallState.StreamsRunning;
            AllowHideContorls = true;
            SubscribeCallStatistics();
        }

        internal void OnResumed()
        {
            CallState = VATRPCallState.LocalResumed;
        }

        internal void OnRemotePaused()
        {
            CallState = VATRPCallState.RemotePaused;
        }

        internal void OnLocalPaused()
        {
            CallState = VATRPCallState.LocalPaused;
        }

        internal void OnClosed(bool isError, string errorMessage, bool isDeclined)
        {
            if (isError) 
                CallState = VATRPCallState.Error;
            else if (isDeclined)
                CallState = VATRPCallState.Declined;
            else
                CallState = VATRPCallState.Closed;
           
            ShowIncomingCallPanel = false;
            ShowInfo = false ;
            ShowAvatar = isDeclined;

            ShowOutgoingEndCall = isError || isDeclined;
            ShowRingingTimer = !isError && !isDeclined;

            if (isError)
            {
                if ( string.Compare(errorMessage, "Busy here", StringComparison.InvariantCultureIgnoreCase) == 0)
                    ErrorMessage = string.Format("Call failed, {0} is busy", CallerInfo);
                else if (string.Compare(errorMessage, "Busy here timeout", StringComparison.InvariantCultureIgnoreCase) == 0)
                    ErrorMessage = string.Format("Call failed, {0} is not available", CallerInfo);
                else if (string.Compare(errorMessage, "Not Found", StringComparison.InvariantCultureIgnoreCase) == 0)
                    ErrorMessage = string.Format("Call failed, {0} is temporarily unavailable", CallerInfo);
                else
                    ErrorMessage = errorMessage;
            }
            else
                ErrorMessage = string.Empty;
            AllowHideContorls = false;
            StopAnimation();

            UnsubscribeCallStaistics();

            if (timerCall.Enabled)
                timerCall.Stop();
//#if DEBUG
            if (autoAnswerTimer.Enabled)
                autoAnswerTimer.Stop();
//#endif

        }

        #endregion

        internal void AcceptCall()
        {
//#if DEBUG
            if (autoAnswerTimer.Enabled)
            {
                AutoAnswer = 0;
                autoAnswerTimer.Stop();
            }
//#endif
            StopAnimation();

            //Hide();

            ShowIncomingCallPanel = false;
            IsMuteOn = false;


        }

        internal void DeclineCall(bool declineOnTimeout)
        {
//#if DEBUG
            if (autoAnswerTimer.Enabled)
            {
                AutoAnswer = 0;
                autoAnswerTimer.Stop();
            }
//#endif
            SetTimeout(delegate
            {
                if (_currentCall != null)
                {
                    try
                    {
                        _linphoneService.DeclineCall(_currentCall.NativeCallPtr);
                    }
                    catch (Exception ex)
                    {
                        ServiceManager.LogError("DeclineCall", ex);
                    }
                }
            }, 30);
        }

        internal void PauseCall()
        {
            SetTimeout(delegate
            {
                if (_currentCall != null)
                {
                    try
                    {
                        _linphoneService.PauseCall(_currentCall.NativeCallPtr);
                    }
                    catch (Exception ex)
                    {
                        ServiceManager.LogError("PauseCall", ex);
                    }
                }
            }, 30);
        }

        internal void ResumeCall()
        {
            SetTimeout(delegate
            {
                if (_currentCall != null)
                {
                    try
                    {
                        _linphoneService.ResumeCall(_currentCall.NativeCallPtr);
                    }
                    catch (Exception ex)
                    {
                        ServiceManager.LogError("ResumeCall", ex);
                    }
                }
            }, 30);
        }

        public void SubscribeCallStatistics()
        {
            if (subscribedForStats)
                return;
            subscribedForStats = true;
            CallInfoCtrl.SetViewModel(_callInfoViewModel);
            ServiceManager.Instance.LinphoneService.CallStatisticsChangedEvent +=
                _callInfoViewModel.OnCallStatisticsChanged;

            _callInfoViewModel.CallQualityChangedEvent += OnCalQualityChanged;
        }

        public void UnsubscribeCallStaistics()
        {
            if (!subscribedForStats)
                return;
            subscribedForStats = false;
            ServiceManager.Instance.LinphoneService.CallStatisticsChangedEvent -=
                _callInfoViewModel.OnCallStatisticsChanged;
            _callInfoViewModel.CallQualityChangedEvent -= OnCalQualityChanged;
        }

        private void OnCalQualityChanged(VATRP.Linphone.VideoWrapper.QualityIndicator callQuality)
        {
            if (CallQualityChangedEvent != null)
                CallQualityChangedEvent(callQuality);
        }

        internal void SwitchSelfVideo()
        {
            _linphoneService.SwitchSelfVideo();
        }

        internal void HoldAndAcceptCall()
        {
//#if DEBUG
            if (autoAnswerTimer.Enabled)
            {
                AutoAnswer = 0;
                autoAnswerTimer.Stop();
            }
//#endif
            StopAnimation();

            IsMuteOn = false;

            if (_currentCall != null)
            {
                try
                {
                    bool muteMicrophone = false;
                    bool muteSpeaker = false;
                    //bool enableVideo = true;
                    if (App.CurrentAccount != null)
                    {
                        muteMicrophone = App.CurrentAccount.MuteMicrophone;
                        muteSpeaker = App.CurrentAccount.MuteSpeaker;
                        //enableVideo = App.CurrentAccount.EnableVideo;
                    }
                    _linphoneService.AcceptCall(_currentCall.NativeCallPtr,
                        ServiceManager.Instance.ConfigurationService.Get(Configuration.ConfSection.GENERAL,
                            Configuration.ConfEntry.USE_RTT, true), muteMicrophone, muteSpeaker, true);
                }
                catch (Exception ex)
                {
                    ServiceManager.LogError("AcceptCall", ex);
                }
            }
        }

        private void SetTimeout(Action callback, int miliseconds)
        {
            var timeout = new System.Timers.Timer {Interval = miliseconds, AutoReset = false};
            timeout.Elapsed += (sender, e) => callback();
            timeout.Start();
        }

        public bool Equals(CallViewModel other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (ActiveCall == null || other.ActiveCall == null)
                return false;

            return ActiveCall.Equals(other.ActiveCall);
        }

        public bool Equals(VATRPCall other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ActiveCall == null)
                return false;

            return ActiveCall.Equals(other);
        }

    }
}