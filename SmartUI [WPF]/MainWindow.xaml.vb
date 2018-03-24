﻿Imports System
Imports System.ComponentModel
Imports System.Globalization
Imports System.Net.NetworkInformation
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports CoreAudioApi
'Imports for SPOTIFY-API .NET
Imports SpotifyAPI.Local 'Enums
Imports SpotifyAPI.Local.Models 'Models for the JSON-responses

Class MainWindow
    ReadOnly _conf As New cls_config
    Public Shared wnd_log As New wnd_log

    Public Shared settings_need_update As Boolean = False
    Public Shared weather_need_update As Boolean = False

    Public Shared suiversion As String = My.Application.Info.Version.Major & "." & My.Application.Info.Version.Minor & ".21"

#Region "Dock"
    Const ABM_NEW As Int32 = 0
    Const ABM_REMOVE As Int32 = 1
    Const ABM_SETPOS As Int32 = 3

    Private Structure APPBARDATA
        Public cbSize As Int32
        Public hwnd As IntPtr
        Public uCallbackMessage As [Delegate]
        Public uEdge As Int32
        Public rc As RECT
        Public lParam As Int32
    End Structure

    Private Structure RECT
        Public rLeft As Int32
        Public rTop As Int32
        Public rRight As Int32
        Public rBottom As Int32
    End Structure

    Private Declare Function apiSHAppBarMessage Lib "shell32" Alias "SHAppBarMessage" (ByVal dwMessage As Int32, ByRef pData As APPBARDATA) As Int32
    Private abd As New APPBARDATA

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If abd.rc.rBottom = 25 Then apiSHAppBarMessage(ABM_REMOVE, abd)
        'RemoveHandler NetworkChange.NetworkAddressChanged, AddressOf AddressChangedCallback
    End Sub

    Private Sub MainWindow_LocationChanged(sender As Object, e As EventArgs) Handles Me.LocationChanged, Me.SizeChanged
        Width = SystemParameters.PrimaryScreenWidth
        Top = 0
        Left = 0
    End Sub

    Private Sub sui_dock()  'DOCK
        On Error Resume Next
        apiSHAppBarMessage(ABM_REMOVE, abd)
        abd = New APPBARDATA
        abd.cbSize = Marshal.SizeOf(abd)
        abd.uEdge = 1
        abd.rc.rLeft = 0
        abd.rc.rRight = CInt(SystemParameters.PrimaryScreenWidth)
        abd.rc.rTop = 0
        abd.rc.rBottom = 25
        apiSHAppBarMessage(ABM_NEW, abd)
        apiSHAppBarMessage(ABM_SETPOS, abd)

        Topmost = True
        anim_slidein()
    End Sub

    Private Sub anim_slidein()
        Top = -25
        Left = 0
        Me.Width = SystemParameters.PrimaryScreenWidth

        Dim dblanim As New DoubleAnimation()
        dblanim.From = -25
        dblanim.To = 0
        dblanim.AutoReverse = False
        dblanim.Duration = TimeSpan.FromSeconds(0.6)

        Dim storyboard As New Storyboard()
        Storyboard.SetTarget(dblanim, Me)
        Storyboard.SetTargetProperty(dblanim, New PropertyPath(Window.TopProperty))

        storyboard.Children.Add(dblanim)
        storyboard.Begin(Me)
    End Sub
#End Region

#Region "MyBase & Startup"
    Public Sub New()
        wnd_log.outputBox.AppendText("SmartUI LOG")
        wnd_log.outputBox.AppendText(NewLine & "Made in 2016/18 by Niklas Wagner in Hannover")
        wnd_log.outputBox.AppendText(NewLine & "APP start time: " & DateTime.Now.Day & ". " & CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month) & " - " & DateTime.Now.ToLongTimeString)
        wnd_log.outputBox.AppendText(NewLine & "APP Version: " & suiversion & " (" & My.Application.Info.Version.ToString & " - " & IO.File.GetLastWriteTime(AppDomain.CurrentDomain.BaseDirectory & "\SmartUI.exe").ToString("yyMMdd") & ")")
        wnd_log.outputBox.AppendText(NewLine & "OS Version: " & Environment.OSVersion.ToString & NewLine)

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()
        ' Fügen Sie Initialisierungen nach dem InitializeComponent()-Aufruf hinzu.

        'SmartUI multi instance check
        Dim int_sui_pcount As Integer = 0
        For Each prog As Diagnostics.Process In System.Diagnostics.Process.GetProcesses
            If prog.ProcessName = "SmartUI" Then int_sui_pcount += 1
        Next

        If int_sui_pcount > 1 Then
            MessageBox.Show("Der Start wurde abgebrochen, da bereits eine Instanz läuft", "SmartUI", MessageBoxButton.OK, MessageBoxImage.Stop)
            Application.Current.Shutdown()
        Else
            _conf.load_variables()
            sui_dock()
        End If
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        wpf_helper.helper_grid(grd_volume, False)
        wpf_helper.helper_grid(grd_spotify, False)
        wpf_helper.helper_grid(grd_weather, False)
        wpf_helper.helper_grid(grd_network, False)
        wpf_helper.helper_grid(grd_menu_right, False)

        lbl_clock.Content = "SmartUI"
        lbl_clock_weekday.Content = "v" & suiversion

        ui_clock_weekday = CultureInfo.CurrentCulture.DateTimeFormat.GetShortestDayName(DateTime.Now.DayOfWeek)

        'log if this is this versions first run
        If Not My.Application.Info.Version.ToString = _conf.read("app", "firstrun", "") Then wnd_log.AddLine("INFO", "First start after updating the app" & Environment.NewLine)

        lbl_weather.Content = "--°" 'Change text to this to avoid that user see's labels standard text
        settings_load()

        AddHandler Microsoft.Win32.SystemEvents.PowerModeChanged, AddressOf SystemEvents_PowerModeChanged
    End Sub

    Public Sub settings_load()
        'LOG
        If settings_need_update Then
            wnd_log.AddLine("INFO" & "-SETTINGS", "Loading updated settings")
        Else
            wnd_log.AddLine("INFO" & "-SETTINGS", "Loading settings")
        End If

        'Seconds
        If CType(_conf.read("UI", "cb_wndmain_clock_enabled", "True"), Boolean) = False Then ui_clock_style = 0 Else ui_clock_style = 1

        If Not ui_clock_style = 0 Then
            If CType(_conf.read("UI", "cb_wndmain_clock_seconds", "False"), Boolean) = True Then ui_clock_style = 2
            If CBool(_conf.read("UI", "cb_wndmain_clock_weekday", "True")) = True Then ui_clock_style += 10 'Weekday
        End If

        'window blur
        cls_blur_behind.blur(Me, CType(_conf.read("UI", "cb_wndmain_blur_enabled", "False"), Boolean))

        'Network
        netmon_config_thresh_text = CType(_conf.read("UI", "cb_wndmain_net_textDisableSpeedLimit", "False"), Boolean)
        netmon_config_threshold = CType(_conf.read("UI", "slider_netmon_threshold", "10"), Integer)

        If cls_weather.get_humidity = -1 And cls_weather.conf_enabled = True Then
            weather_need_update = True
            cls_weather.init_update(False)
        End If
    End Sub

    Private WithEvents tmr_aInit As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 4), .IsEnabled = True}
    Private Sub tmr_aInit_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmr_aInit.Tick
        cls_weather.init_update(True) 'Init Weather
        wnd_flyout_appmenu.ui_settings.Show() 'Init settings window #!

        wpf_helper.helper_grid(grd_menu_right, True)

        init_coreaudio() 'Init CoreAudio API

        init_spotifyAPI() 'Init Spotify-API

        wpf_helper.helper_grid(grd_weather, cls_weather.conf_enabled)

        tmr_clock.Start()
        tmr_netmon_v2.Start()

        mpb_indicateLoading.Visibility = Visibility.Hidden
        'mpb_indicateLoading.Margin = New Thickness(0, -5, 0, 0)
        'mpb_indicateLoading.IsIndeterminate = False
        'mpb_indicateLoading.Foreground = Brushes.White
        'mpb_indicateLoading.Opacity = 0.5

        If sAPI_allowed = True Then
            If e_playing = True Then
                wpf_helper.helper_grid(grd_spotify, True, -5, -25)
                anim_grd_pos(grd_spotify, weather_width)
                anim_grd_pos(grd_weather, 1.5)
            Else
                wpf_helper.helper_grid(grd_spotify, True, 25, -25)
                anim_grd_pos(grd_spotify, 5)
                anim_grd_pos(grd_weather, 23.5)
            End If
        End If

        tmr_aInit.Stop()
    End Sub
#End Region

#Region "SYS_EVENTS"
    Private Sub SystemEvents_PowerModeChanged(ByVal sender As Object, ByVal e As Microsoft.Win32.PowerModeChangedEventArgs) 'Update weather on os resume
        'Select Case e.Mode
        '    Case Microsoft.Win32.PowerModes.Resume
        '    Case Microsoft.Win32.PowerModes.StatusChange
        '    Case Microsoft.Win32.PowerModes.Suspend
        'End Select

        If e.Mode = Microsoft.Win32.PowerModes.Resume Then
            wnd_log.AddLine("SYSEVENT", "OS resumed from Standby/Hibernation", "att")
            lbl_weather.Content = "--°"
            tmr_waitfornetwork.Start()
        End If
    End Sub

    Private WithEvents tmr_waitfornetwork As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 5), .IsEnabled = False}
    Private Sub tmr_WaitForNetwork_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmr_waitfornetwork.Tick
        If My.Computer.Network.IsAvailable = True Then
            cls_weather.init_update(False)
            tmr_waitfornetwork.Stop()
        End If
    End Sub

#End Region

#Region "Clock"
    Private WithEvents tmr_clock As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1), .IsEnabled = False}
    Private ui_clock_weekday As String = "Mo"
    Private ui_clock_style As Integer = -1
    Dim clock_date As Boolean = False

    Private Sub tmr_clock_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmr_clock.Tick
        If DateTime.Now.Minute = 0 Then ui_clock_weekday = CultureInfo.CurrentCulture.DateTimeFormat.GetShortestDayName(DateTime.Now.DayOfWeek)

        If clock_date = False Then
            Select Case ui_clock_style
                Case 0 'no clock
                    lbl_clock.Content = Nothing
                    lbl_clock_weekday.Content = Nothing

                Case 1 'hh:mm
                    lbl_clock.Content = DateTime.Now.ToShortTimeString
                    lbl_clock_weekday.Content = Nothing

                Case 2 'hh:mm:ss
                    lbl_clock.Content = DateTime.Now.ToLongTimeString
                    lbl_clock_weekday.Content = Nothing

                Case 11 'DDD, hh:mm
                    lbl_clock_weekday.Content = ui_clock_weekday & "."
                    lbl_clock.Content = DateTime.Now.ToShortTimeString()

                Case 12 'DDD, hh:mm:ss
                    lbl_clock_weekday.Content = ui_clock_weekday & "."
                    lbl_clock.Content = DateTime.Now.ToLongTimeString()
            End Select
        End If

        If weather_need_update = True Then
            lbl_weather.Content = cls_weather.get_temp
            wpf_helper.helper_image(icn_weather, cls_weather.oww_data_conditionIMG)
            weather_need_update = False
        End If

        'WA Spotify v2 (Aggressive) | Restart sotify evp in case of error
        If e_playing = True And dbg_sptfy = dbg_sptfy_2 Then
            sAPI_error_count += 1

            If sAPI_error_count > 4 Then
                wpf_helper.helper_grid(grd_spotify, False)

                'TEST
                wpf_helper.helper_grid(grd_spotify, True, 25)
                wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/spotify_notification.png")
                anim_grd_pos(grd_spotify, 5)
                anim_grd_pos(grd_weather, 25)
                'END
            Else
                flyout_media.Close() 'close media widget
                media_widget_opened = -1

                sAPI_error = True
                sAPI_error_count = 0
                init_spotifyAPI()
            End If
        Else
            sAPI_error = False
            sAPI_error_count = 0
        End If

        dbg_sptfy = dbg_sptfy_2

        If settings_need_update = True Then
            settings_load()
            settings_need_update = False
            wpf_helper.helper_grid(grd_weather, cls_weather.conf_enabled)
        End If
    End Sub

    'Show Date on MouseOver
    Private Sub lbl_clock_MouseEnter(sender As Object, e As MouseEventArgs) Handles grd_clock.MouseEnter
        If tmr_clock.IsEnabled = False Then Exit Sub
        clock_date = True 'Block content change from clock timer
        lbl_clock.Content = DateTime.Now.Day & ". " & CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month) & " " & DateTime.Now.Year
        lbl_clock_weekday.Content = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.DayOfWeek) & ","
    End Sub

    Private Sub lbl_clock_MouseLeave(sender As Object, e As MouseEventArgs) Handles grd_clock.MouseLeave
        clock_date = False 'Allow content change from clock timer
    End Sub

    Private Sub lbl_clock_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_clock_weekday.SizeChanged
        If lbl_clock_weekday.Content Is Nothing Then
            lbl_clock.Margin = New Thickness(0, -2, 0, 0)
        Else
            lbl_clock.Margin = New Thickness(lbl_clock_weekday.RenderSize.Width - 5, -2, 0, 0)
        End If
    End Sub

    Private Sub grd_clock_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles grd_clock.SizeChanged
        anim_grd_pos(grd_clock, (ActualWidth - grd_clock.ActualWidth) / 2)
    End Sub

#End Region

#Region "CoreAudio"
    ReadOnly _device_enum As New MMDeviceEnumerator()
    ReadOnly audio_device As MMDevice = _device_enum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)

    Private Sub init_coreaudio()
        wnd_log.AddLine("INFO" & "-CORE AUDIO", "Initializing...")

        wpf_helper.helper_grid(grd_volume, True)

        ca_update(Math.Round(audio_device.AudioEndpointVolume.MasterVolumeLevelScalar * 100, 0), audio_device.AudioEndpointVolume.Mute)
        AddHandler audio_device.AudioEndpointVolume.OnVolumeNotification, AddressOf AudioEndpointVolume_OnVolumeNotification
    End Sub

    Private Sub AudioEndpointVolume_OnVolumeNotification(data As AudioVolumeNotificationData)
        ca_update(Math.Round(data.MasterVolume * 100, 0), data.Muted)
    End Sub

    Private Sub ca_update(ByVal e_volume As Double, ByVal e_muted As Boolean)
        If e_volume < 1 Or e_muted = True Then
            wpf_helper.helper_image(icn_volume, "pack://application:,,,/Resources/snd_off.png")
            wpf_helper.helper_label(lbl_volume, Nothing, Visibility.Hidden)
            wpf_helper.helper_label(lbl_volume_unit, "Mute")

        Else
            wpf_helper.helper_label(lbl_volume_unit, "%")
            wpf_helper.helper_label(lbl_volume, e_volume.ToString, Visibility.Visible)

            Select Case e_volume
                Case < 5
                    wpf_helper.helper_image(icn_volume, "pack://application:,,,/Resources/snd_vLow.png")
                Case < 30
                    wpf_helper.helper_image(icn_volume, "pack://application:,,,/Resources/snd_low.png")
                Case < 60
                    wpf_helper.helper_image(icn_volume, "pack://application:,,,/Resources/snd_mid.png")
                Case Else
                    wpf_helper.helper_image(icn_volume, "pack://application:,,,/Resources/snd_high.png")
            End Select
        End If
    End Sub

    Dim ca_skip_volChange As Boolean
    Private Sub icn_volume_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles icn_volume.MouseWheel, lbl_volume.MouseWheel, grd_volume.MouseWheel
        ca_skip_volChange = Not ca_skip_volChange

        If ca_skip_volChange = True Then
            If e.Delta > 0 Then
                audio_device.AudioEndpointVolume.VolumeStepUp()
            Else
                audio_device.AudioEndpointVolume.VolumeStepDown()
            End If
        End If
    End Sub

    '---- RWork
    Private Sub grd_volume_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles grd_volume.SizeChanged, lbl_volume.SizeChanged
        If lbl_volume.Visibility = Visibility.Visible Then
            lbl_volume_unit.Margin = New Thickness(17 + Math.Round(lbl_volume.RenderSize.Width, 0), -1, 0, 0)
        Else
            lbl_volume_unit.Margin = New Thickness(19, -1, 0, 0)
        End If

        grd_network.Margin = New Thickness(0, 0, grd_volume.RenderSize.Width + grd_volume.Margin.Right + 6, 0)
    End Sub
    '-----
    Private Sub grd_volume_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles grd_volume.MouseUp
        audio_device.AudioEndpointVolume.Mute = Not audio_device.AudioEndpointVolume.Mute
    End Sub

#End Region

#Region "SpotifyAPI-NET"
    Dim sAPI_allowed As Boolean = False
    Dim sAPI_error As Boolean = False
    Dim sAPI_error_count As Integer = 0
    'needed for recovery (...)
    Dim dbg_sptfy As String = ""
    Dim dbg_sptfy_2 As String = ""

    Public Shared spotifyapi As SpotifyLocalAPI
    Public Shared _currentTrack As Track
    Public Shared _sAPI_ClientVersion As String

    Sub init_spotifyAPI() 'Init and connect Spotify-API
        If sAPI_error = False Then wnd_log.AddLine("INFO" & "-MEDIA", "Init Spotify-API .NET...") Else wnd_log.AddLine("ATT" & "-MEDIA", "(!) Restarting Spotify-API .NET...", "att")
        spotifyapi = New SpotifyLocalAPI

        'Check if Spotify (and WebHelper) are running
        If Not SpotifyLocalAPI.IsSpotifyRunning() Then wnd_log.AddLine("INFO" & "-MEDIA", "Spotify isn't running!")
        If Not SpotifyLocalAPI.IsSpotifyWebHelperRunning() Then wnd_log.AddLine("INFO" & "-MEDIA", "SpotifyWebHelper isn't running!")

        Try
            sAPI_allowed = spotifyapi.Connect

            If sAPI_allowed = True Then
                wnd_log.AddLine("INFO" & "-MEDIA", "Connected to Spotify Client (Version: " & spotifyapi.GetStatus.ClientVersion.ToString & ")")
                sAPI_UpdateInfos()

                spotifyapi.ListenForEvents = True
                sAPI_error = False

                AddHandler spotifyapi.OnPlayStateChange, AddressOf spotifyapi_OnPlayStateChange
                AddHandler spotifyapi.OnTrackChange, AddressOf spotifyapi_OnTrackChange
                AddHandler spotifyapi.OnTrackTimeChange, AddressOf spotifyapi_OnTrackTimeChange
            Else
                wnd_log.AddLine("INFO" & "-MEDIA", "Couldn't connect - API disabled until next app start!")
                sAPI_error = True
            End If

        Catch ex As Exception
            wnd_log.AddLine("ERR" & "-MEDIA", "Well, here's something really fucked up...", "err")
            'MessageBox.Show(ex.Message, "Spotify .NET API", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Public Sub sAPI_UpdateInfos()
        _sAPI_ClientVersion = spotifyapi.GetStatus.ClientVersion.ToString

        If spotifyapi.GetStatus.Track IsNot Nothing Then        'Update track infos
            _currentTrack = spotifyapi.GetStatus.Track
            e_playing = spotifyapi.GetStatus.Playing
        End If
    End Sub

    Dim lasttrack As Track
    Private Sub spotifyapi_OnTrackTimeChange(sender As Object, e As TrackTimeChangeEventArgs)
        dbg_sptfy = (_currentTrack.TrackResource.Uri & e.TrackTime & _currentTrack.Length & e_playing)

        If Not lasttrack Is _currentTrack Or media_newtrack = True Then
            media_newtrack = True
            lasttrack = _currentTrack
            sui_media_update(_currentTrack.TrackResource.Name, _currentTrack.ArtistResource.Name, Date.MinValue.AddSeconds(_currentTrack.Length - CInt(e.TrackTime)).ToString("m:ss"), e.TrackTime, _currentTrack.Length)
        Else
            sui_media_update(Nothing, Nothing, Date.MinValue.AddSeconds(_currentTrack.Length - CInt(e.TrackTime)).ToString("m:ss"), e.TrackTime, _currentTrack.Length)
        End If
    End Sub

    Private Sub spotifyapi_OnTrackChange(sender As Object, e As TrackChangeEventArgs)
        _currentTrack = e.NewTrack
    End Sub

    Public Shared e_playing As Boolean
    Private Sub spotifyapi_OnPlayStateChange(sender As Object, e As PlayStateEventArgs)
        e_playing = e.Playing

        If e.Playing = False Then
            tmr_mediaInfo_delay.Start()
            wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/ic_pause_white_24dp.png")
        Else
            tmr_mediaInfo_delay.Stop()
            wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/spotify_notification.png")
            wpf_helper.helper_grid(grd_spotify, True, -5)
            anim_grd_pos(grd_weather, 1.5)
            anim_grd_pos(grd_spotify, weather_width)

        End If
    End Sub

    Private WithEvents tmr_mediaInfo_delay As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 1), .IsEnabled = False}
    Private Sub tmr_mediaInfo_delay_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmr_mediaInfo_delay.Tick
        wpf_helper.helper_grid(grd_spotify, True, 25)
        wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/spotify_notification.png")
        anim_grd_pos(grd_spotify, 5)
        anim_grd_pos(grd_weather, 23.5)

        tmr_mediaInfo_delay.Stop()
    End Sub
#End Region

#Region "MIP (Media Info Processing)"
    Public Shared media_newtrack As Boolean
    Dim media_last_time As String
    Dim media_artist As String

    Private Sub sui_media_update(ByVal e_title As String, ByVal e_artist As String, ByVal e_Tremaining As String, ByVal e_pb_val As Double, ByVal e_pb_max As Double)
        'Title ------------------ don't update label if title didn't change

        If e_title <> Nothing And media_widget_opened <> 1 Then
            Dim media_additional_text As String

            Select Case True
                Case e_title.Contains("(") And Not e_title.StartsWith("(")
                    wpf_helper.helper_label(lbl_spotify, e_title.Substring(0, (e_title.IndexOf("(") - 1))) 'main title
                    media_additional_text = media_trk_adinfo(e_title.Substring(e_title.IndexOf("("), e_title.Length - e_title.IndexOf("("))) & " ٠ " 'check & add_info in SubLabel

                Case e_title.Contains("- ")
                    wpf_helper.helper_label(lbl_spotify, e_title.Substring(0, (e_title.IndexOf("-") - 1))) 'main title
                    media_additional_text = media_trk_adinfo(e_title.Substring(e_title.IndexOf("-"), e_title.Length - e_title.IndexOf("-"))) & " ٠ "  'check & add_info in SubLabel

                Case Else
                    media_additional_text = ""

                    If e_title.Length > 41 Then
                        wpf_helper.helper_label(lbl_spotify, e_title.Remove(40, e_title.Length - 40) & "...")
                    Else
                        wpf_helper.helper_label(lbl_spotify, e_title)
                    End If
            End Select

            media_artist = media_additional_text & e_artist
            media_newtrack = False
        End If

        If Not media_last_time = e_Tremaining And media_widget_opened <> 1 Then
            wpf_helper.helper_label(lbl_spotify_remaining, media_artist & " ٠ " & e_Tremaining)
            media_last_time = e_Tremaining
        End If

        If media_widget_opened = 1 Then wnd_flyout_media.str_media_time = e_pb_val & "%" & e_pb_max & "#" & e_Tremaining
    End Sub

    'MEDIA GRID UPDATE PART ---------------------------
    Private Function media_trk_adinfo(e As String) As String
        Dim e_fs As String = e

        'remove "(" at start and ")" end
        If e.StartsWith("(") And e.EndsWith(")") Then e_fs = e.Substring(1, e.Length - 2)
        'remove "-" at beginning
        If e.StartsWith("-") Then e_fs = e.Substring(2)
        'limit to length of 30
        If e_fs.Length > 30 Then
            If e_fs.Substring(0, 30).StartsWith("(") And Not e_fs.Substring(0, 30).Contains(")") Then Return e_fs.Substring(1, 30) & "..." Else Return e_fs.Substring(0, 30) & "..."
        Else
            Return e_fs
        End If
    End Function

    Private Sub lbl_spotify_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_spotify.SizeChanged, lbl_spotify_remaining.SizeChanged
        lbl_spotify_remaining.Margin = New Thickness(icn_spotify.RenderSize.Width + lbl_spotify.RenderSize.Width - 4, -1, 0, 0)
    End Sub

    'SPOTIFY TRACK CHANGE
    Dim spotify_skip_trackChange As Boolean = False

    'Change track with scrolling up/down
    Private Sub lbl_spotify_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles grd_spotify.MouseWheel
        If spotify_skip_trackChange = True Then
            spotify_skip_trackChange = False
        Else
            'Scroll up > Next Track / Skip | Scroll down > Last Track / Return
            If e.Delta > 0 Then spotifyapi.Skip() Else spotifyapi.Previous()

            spotify_skip_trackChange = True
        End If
    End Sub

    Private Sub btn_spotify_playpause_Click(sender As Object, e As RoutedEventArgs) Handles icn_spotify.MouseUp
        If sAPI_error = True Then
            sAPI_error_count = 0
            init_spotifyAPI()
            wpf_helper.helper_grid(grd_spotify, e_playing) 'grid is only visible if Spotify is playing something.
        Else
            ' PlayPause
            If e_playing = False Then spotifyapi.Play() Else spotifyapi.Pause()
        End If
    End Sub

    Private Sub icn_spotify_MouseEnter(sender As Object, e As MouseEventArgs) Handles icn_spotify.MouseEnter
        If e_playing = True Then
            wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/ic_pause_white_24dp.png")
        Else
            wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/ic_play_arrow_white_24dp.png")
        End If
    End Sub

    Private Sub icn_spotify_MouseLeave(sender As Object, e As MouseEventArgs) Handles icn_spotify.MouseLeave
        wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/spotify_notification.png")
    End Sub

    'positioning labels - left
    Dim weather_width As Double = 0
    Private Sub lbl_weather_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles grd_weather.SizeChanged ', grd_spotify.SizeChanged
        If grd_weather.Visibility = Visibility.Visible Then
            weather_width = grd_weather.ActualWidth

            If e_playing = True Then
                anim_grd_pos(grd_spotify, weather_width)
                anim_grd_pos(grd_weather, 1.5)
            Else

            End If
        Else
            'grd_spotify.Margin = New Thickness(3, 0, 0, 0)
            weather_width = 5
            anim_grd_pos(grd_spotify, weather_width)
        End If
    End Sub

    Private Sub grd_weather_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles grd_weather.IsVisibleChanged
        If grd_weather.Visibility = Visibility.Visible Then
            weather_width = grd_weather.ActualWidth
            If e_playing = True Then
                anim_grd_pos(grd_spotify, weather_width)
                anim_grd_pos(grd_weather, 1.5)
            End If
        Else
            'grd_spotify.Margin = New Thickness(3, 0, 0, 0)
            weather_width = 5
            anim_grd_pos(grd_spotify, weather_width)
        End If
    End Sub
#End Region

#Region "NOTIFICATION"
    Private Sub helper_notification(e_msg As String, Optional e_icn As String = Nothing)
        Dim c_blur As New Effects.BlurEffect With {.Radius = 10}
        grd_controls.Effect = c_blur
        grd_controls.Opacity = 0.65

        Application.Current.Dispatcher.Invoke(Threading.DispatcherPriority.Normal,
                                              New ThreadStart(Sub()
                                                                  fout_notification.IsOpen = True
                                                                  fout_notification.IsAutoCloseEnabled = False
                                                                  fout_notification.IsAutoCloseEnabled = True

                                                                  lbl_notification.Content = e_msg
                                                                  If Not e_icn = Nothing Then
                                                                      icn_notification.Source = CType(New ImageSourceConverter().ConvertFromString(e_icn),
                                                                      ImageSource)
                                                                  Else
                                                                      icn_notification.Source = CType(New ImageSourceConverter().
                                                                      ConvertFromString("pack://application:,,,/Resources/ic_error_outline_white_24dp.png"), ImageSource)
                                                                  End If
                                                              End Sub))
    End Sub

    Private Sub fout_notification_ClosingFinished(sender As Object, e As RoutedEventArgs) Handles fout_notification.ClosingFinished
        grd_controls.Effect = Nothing
        grd_controls.Opacity = 1
    End Sub
#End Region

#Region "NetMonitor v2"
    Dim netmon_enabled As Integer = 0 '0 = disabled / 1 = enabled / -1 = error
    Dim netmon_NIC As NetworkInterface
    Dim netmon_config_thresh_text As Boolean = False    'Settings
    Dim netmon_config_threshold As Integer = 10

    Private WithEvents tmr_netmon_v2 As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 500), .IsEnabled = False}
    Private Sub tmr_netmon_v2_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmr_netmon_v2.Tick
        If netmon_enabled = -1 Then
            wnd_log.AddLine("INFO" & "-NET", "Network monitoring state: " & netmon_enabled.ToString)
            tmr_netmon_v2.Stop()
        End If

        net_monitoring()
    End Sub

    Private Sub netmon_set_nic()
        If netmon_enabled = -1 Then Exit Sub

        Try
            Dim net_allNICs As NetworkInterface() = NetworkInterface.GetAllNetworkInterfaces()
            wnd_settings.net_NIC_list = Nothing

            For i As Integer = 0 To net_allNICs.Length - 1
                Dim net_interface As NetworkInterface = net_allNICs(i)

                If net_interface.NetworkInterfaceType <> NetworkInterfaceType.Tunnel AndAlso net_interface.NetworkInterfaceType <> NetworkInterfaceType.Loopback Then
                    If Not _conf.read("NET", "ComboBox_net_interface", "NOT_SET") = "null" And Not _conf.read("NET", "ComboBox_net_interface", "NOT_SET") = "NOT_SET" Then
                        If net_interface.Name = _conf.read("NET", "ComboBox_net_interface", "NOT_SET") Then netmon_NIC = net_interface
                    End If

                    wnd_settings.net_NIC_list &= net_interface.Name & ";"
                End If
            Next

            wnd_log.AddLine("INFO" & "-NET", "Seleceted Interface: " & netmon_NIC.Name)

            wpf_helper.helper_grid(grd_network, True)
            netmon_enabled = 1

        Catch
            wnd_log.AddLine("ERR" & "-NET", "Error in 'net_get_interfaces'", "err")
            netmon_enabled = -1
            wpf_helper.helper_grid(grd_network, False)
        End Try
    End Sub

    Dim netmon_bytesTx_total As Int64
    Dim netmon_bytesRx_total As Int64
    Dim netmon_speed_Tx As Integer
    Dim netmon_speed_Rx As Integer

    Dim net_NIC_statistic As IPInterfaceStatistics
    Private Sub net_monitoring()
        If netmon_enabled = 0 Then
            wnd_log.AddLine("INFO" & "-NET", "Network monitoring disabled or no interface selected, -> 'net_get_interfaces'", "att")
            netmon_set_nic()
            Exit Sub

        ElseIf netmon_enabled = -1 Then 'Exit sub if netmon got an error
            wnd_log.AddLine("ERR" & "-NET", "Network monitoring error, exiting", "err")
            wpf_helper.helper_grid(grd_network, False)
            Exit Sub
        End If

        Try
            net_NIC_statistic = netmon_NIC.GetIPStatistics()
        Catch ex As Exception
            wnd_log.AddLine("ERR" & "-NET", "error @ netmon_NIC.GetIPStatistics()", "err")
            Exit Sub
        End Try

        If netmon_bytesTx_total = 0 And netmon_bytesRx_total = 0 Then
            netmon_bytesTx_total = net_NIC_statistic.BytesSent
            netmon_bytesRx_total = net_NIC_statistic.BytesReceived
        Else
            Try 'get sent/received kbytes
                netmon_speed_Tx = CInt(net_NIC_statistic.BytesSent - netmon_bytesTx_total) * 2
                netmon_speed_Rx = CInt(net_NIC_statistic.BytesReceived - netmon_bytesRx_total) * 2
            Catch ex As Exception

                lbl_network_traffic_send.Content = Nothing
                icn_network_send.Visibility = Visibility.Hidden
                lbl_network_traffic_receive.Content = Nothing
                icn_network_receive.Visibility = Visibility.Hidden

                wnd_log.AddLine("ERR" & "-NET", "Error in 'get sent/received kbytes'", "err")
                Exit Sub
            End Try
        End If

        netmon_bytesTx_total = net_NIC_statistic.BytesSent
        netmon_bytesRx_total = net_NIC_statistic.BytesReceived

        'sent v2
        If netmon_speed_Tx > netmon_config_threshold Then
            icn_network_send.Visibility = Visibility.Visible

            If netmon_speed_Tx > 51200 Or netmon_config_thresh_text = True Then
                lbl_network_traffic_send.Content = get_formatted_bytes(netmon_speed_Tx)
            Else
                lbl_network_traffic_send.Content = Nothing
            End If
        Else
            lbl_network_traffic_send.Content = Nothing
            icn_network_send.Visibility = Visibility.Hidden
        End If

        'received v2
        If netmon_speed_Rx > netmon_config_threshold Then
            icn_network_receive.Visibility = Visibility.Visible

            If netmon_speed_Rx > 51200 Or netmon_config_thresh_text = True Then
                lbl_network_traffic_receive.Content = get_formatted_bytes(netmon_speed_Rx)
            Else
                lbl_network_traffic_receive.Content = Nothing
            End If
        Else
            lbl_network_traffic_receive.Content = Nothing
            icn_network_receive.Visibility = Visibility.Hidden
        End If
    End Sub

    'convert bytes
    Private Function get_formatted_bytes(bytes As Integer) As String
        If bytes < 1024 Then
            Return Math.Round(CDbl(bytes), 0) & "B/s"
        ElseIf bytes < 1048576 Then
            Return Math.Round(CDbl(bytes / 1024), 0) & "KB/s"
        ElseIf bytes < 1073741824 Then
            Return Math.Round(CDbl(bytes / 1048576), 1) & "MB/s"
        Else
            Return Math.Round(CDbl(bytes / 1073741824), 1) & "GB/s"
        End If
    End Function

    'Net Grid Size
    Private Sub lbl_network_traffic_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_network_traffic_receive.SizeChanged, lbl_network_traffic_send.SizeChanged
        icn_network_send.Margin = New Thickness(0, 0, 0, 0)
        lbl_network_traffic_send.Margin = New Thickness(icn_network_send.RenderSize.Width, -1, 0, 0)

        icn_network_receive.Margin = New Thickness(icn_network_send.RenderSize.Width + lbl_network_traffic_send.RenderSize.Width, 0, 0, 0)
        lbl_network_traffic_receive.Margin = New Thickness(icn_network_send.RenderSize.Width + lbl_network_traffic_send.RenderSize.Width + icn_network_receive.RenderSize.Width, -1, 0, 0)

        icn_network_state.Margin = New Thickness((icn_network_send.RenderSize.Width + lbl_network_traffic_send.RenderSize.Width + icn_network_receive.RenderSize.Width + lbl_network_traffic_receive.RenderSize.Width + 3), 3, 0, 0)
    End Sub
#End Region

#Region "FLYOUTS"
    'APPMENU
    ReadOnly _ui_appmenu As New wnd_flyout_appmenu
    Private Sub btn_exit_Click(sender As Object, e As RoutedEventArgs) Handles icn_menu.MouseLeftButtonUp, grd_menu_right.MouseLeftButtonUp
        _ui_appmenu.Show()
        Topmost = False
        Topmost = True
    End Sub

    'WEATHER
    ReadOnly _weather_flyout As New wnd_flyout_weather
    Private Sub icn_weather_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles grd_weather.MouseUp
        If media_widget_opened = 1 Then
            media_newtrack = True
            flyout_media.Hide()
            media_widget_opened = 0
        End If

        _weather_flyout.Show()
        Topmost = False
        Topmost = True
    End Sub

    'MEDIA
    Public Shared media_widget_opened As Integer = 0
    Dim flyout_media As New wnd_flyout_media

    Private Sub lbl_spotify_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles lbl_spotify.MouseLeftButtonUp, lbl_spotify_remaining.MouseLeftButtonUp
        If media_widget_opened = -1 Then flyout_media = New wnd_flyout_media

        If media_widget_opened = 1 Then
            media_newtrack = True
            flyout_media.Hide()
            media_widget_opened = 0
        Else
            _weather_flyout.Hide()
            flyout_media.Show()
            media_widget_opened = 1
            wpf_helper.helper_label(lbl_spotify, "Spotify")
            wpf_helper.helper_label(lbl_spotify_remaining, " ")
            Topmost = False
            Topmost = True
        End If
    End Sub
#End Region

#Region "Animations"
    Private Sub anim_grd_pos(ByVal e_grid As Grid, ByVal e_pos_left As Double)
        Application.Current.Dispatcher.Invoke(Threading.DispatcherPriority.Normal, New ThreadStart(Sub()
                                                                                                       Dim ta As ThicknessAnimation = New ThicknessAnimation With {
                                                                                                                            .From = e_grid.Margin,
                                                                                                                            .[To] = New Thickness(e_pos_left, 0, 0, 0),
                                                                                                                            .Duration = New Duration(TimeSpan.FromSeconds(0.5)),
                                                                                                                            .EasingFunction = New QuarticEase
                                                                                                                             }
                                                                                                       e_grid.BeginAnimation(MarginProperty, ta)
                                                                                                   End Sub))
    End Sub
#End Region

End Class