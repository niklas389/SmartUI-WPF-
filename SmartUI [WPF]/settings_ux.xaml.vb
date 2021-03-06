﻿Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Animation

Public Class wnd_settings
    Dim updater_enabled As Boolean = False

#Region "Window CMD"
    'Move Window
    Private Sub lbl_header_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        Me.DragMove()
    End Sub

    Private Sub btn_wnd_hide_MouseLeftButtonDown(sender As Object, e As RoutedEventArgs) Handles btn_wnd_hide.Click
        Me.Visibility = Visibility.Hidden
        lasttab_lic = False
    End Sub

    Private Sub btn_wnd_minimize_MouseLeftButtonUp(sender As Object, e As RoutedEventArgs) Handles btn_wnd_minimize.Click
        Me.WindowState = WindowState.Minimized
    End Sub

    Private Sub wnd_settings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        lib_hVOSD.Init() 'bring up lib_HVOSD
        Me.Hide()

        grd_navbar.Visibility = Visibility.Visible
        matc_tabctrl.Visibility = Visibility.Visible

        matc_main.Visibility = Visibility.Hidden
        matc_weather.Visibility = Visibility.Hidden
        matc_changelog.Visibility = Visibility.Hidden
        matc_spotify.Visibility = Visibility.Hidden
        matc_updates.Visibility = Visibility.Hidden
        matc_3rdpty.Visibility = Visibility.Hidden
        matc_about.Visibility = Visibility.Hidden

        flyout_cache_reset.IsOpen = False
        flyout_message.IsOpen = False
        pring_flyout_message.Visibility = Visibility.Hidden

        load_settings()
        get_3rdptysw_versions()

        anim_sep_pos(sep_nav_highlight, 0)
        anim_width(sep_nav_highlight, grd_navbar.ActualWidth)
    End Sub

    Public Shared net_NIC_list As String = ";"

    Private Sub wnd_settings_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Hidden Or lasttab_lic = True Then Exit Sub

        nav_home()

        ComboBox_net_interface.Items.Clear()
        ComboBox_net_interface.Items.Add("Deaktiviert")

        For Each str As String In net_NIC_list.Split(CType(";", Char()))
            If str = "" Then Exit For
            ComboBox_net_interface.Items.Add(str)
        Next

        'Network
        If conf.read("NET", "ComboBox_net_interface", "") = "null" Then
            ComboBox_net_interface.SelectedIndex = 0
        Else
            ComboBox_net_interface.SelectedItem = conf.read("NET", "ComboBox_net_interface", "")
            If Not ComboBox_net_interface.SelectedItem Is conf.read("NET", "ComboBox_net_interface", "") Then ComboBox_net_interface.SelectedIndex = 1
        End If

        flyout_cache_reset.IsOpen = False
        matc_tabctrl.Effect = Nothing
    End Sub
#End Region

#Region "Read/Save Settings"
    Dim conf As New cls_config

    Private Sub btn_settings_save_Click(sender As Object, e As RoutedEventArgs) Handles btn_settings_save.Click
        conf.write("UI", "cb_wndmain_clock_enabled", CType(cb_wndmain_clock_enabled.IsChecked, String))
        conf.write("UI", "cb_wndmain_clock_seconds", CType(cb_wndmain_clock_seconds.IsChecked, String))
        conf.write("UI", "cb_wndmain_clock_weekday", CType(cb_wndmain_clock_weekday.IsChecked, String))

        'Window Blur
        conf.write("UI", "cb_wndmain_blur_enabled", CType(cb_wndmain_blur_enabled.IsChecked, String))
        conf.write("UI", "slider_bg_transp", CType(slider_bg_transp.Value, String))

        'Weather
        cls_weather.enabled(cb_wndmain_weather_enabled.IsChecked.Value)

        If Not txtBx_weather_cid.Text = "<City ID>" And Not txtBx_weather_APIkey.Text = "<API Key>" Then _
           cls_weather.oww_set_data(CInt(txtBx_weather_cid.Text), txtBx_weather_APIkey.Text)

        'Network
        If Not ComboBox_net_interface.SelectedIndex = -1 And Not ComboBox_net_interface.SelectedIndex = 0 Then
            conf.write("NET", "ComboBox_net_interface", ComboBox_net_interface.SelectedItem.ToString)
        Else
            conf.write("NET", "ComboBox_net_interface", "null")
        End If

        conf.write("UI", "cb_wndmain_net_textDisableSpeedLimit", CType(cb_wndmain_net_textDisableSpeedLimit.IsChecked, String))
        conf.write("UI", "slider_netmon_threshold", CType(Math.Round(slider_netmon_threshold.Value, 0), String))

        'others
        conf.write("SYS", "cb_other_disableVolumeOSD", CType(cb_other_disableVolumeOSD.IsChecked, String))
        conf.write("SYS", "cb_other_startup_play", CType(cb_other_startup_play.IsChecked, String))

        'Show flyout after saving settings
        grd_bottom_strip.Visibility = Visibility.Hidden
        show_flyout("Änderungen gespeichert!", False)

        MainWindow.settings_need_update = True
        cls_blur_behind.blur(Me, cls_config.ui_blur_enabled)
    End Sub

    Private Sub load_settings()
        cb_wndmain_clock_enabled.IsChecked = CType(conf.read("UI", "cb_wndmain_clock_enabled", "True"), Boolean)
        cb_wndmain_clock_seconds.IsChecked = CType(conf.read("UI", "cb_wndmain_clock_seconds", "False"), Boolean)
        cb_wndmain_clock_weekday.IsChecked = CType(conf.read("UI", "cb_wndmain_clock_weekday", "True"), Boolean)

        'Window Blur
        cb_wndmain_blur_enabled.IsChecked = cls_config.ui_blur_enabled
        cls_blur_behind.blur(Me, cls_config.ui_blur_enabled)
        slider_bg_transp.Value = CType(conf.read("UI", "slider_bg_transp", "165"), Double)
        MainWindow.settings_need_update = True

        'Weather
        cb_wndmain_weather_enabled.IsChecked = cls_weather.conf_enabled

        txtBx_weather_cid.Text = cls_weather.oww_API_cityID.ToString
        txtBx_weather_APIkey.Text = cls_weather.oww_API_key

        'Network
        If conf.read("NET", "ComboBox_net_interface", "") = "null" Then
            ComboBox_net_interface.SelectedIndex = 0
        Else
            ComboBox_net_interface.SelectedItem = conf.read("NET", "ComboBox_net_interface", "")
        End If

        cb_wndmain_net_textDisableSpeedLimit.IsChecked = CType(conf.read("UI", "cb_wndmain_net_textDisableSpeedLimit", "False"), Boolean)
        slider_netmon_threshold.Value = CType(conf.read("UI", "slider_netmon_threshold", "10"), Double)

        'Others
        cb_other_disableVolumeOSD.IsChecked = CType(conf.read("SYS", "cb_other_disableVolumeOSD", "False"), Boolean)
        cb_other_startup_play.IsChecked = CType(conf.read("SYS", "cb_other_startup_play", "False"), Boolean)
        'Others End
    End Sub

    Private Sub wnd_settings_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        conf.write("app", "firstrun", CType(My.Application.Info.Version.ToString, String))
    End Sub

    Dim lib_hVOSD As New HideVolumeOSD.HideVolumeOSDLib
    Private Sub cb_other_disableVolumeOSD_checked(sender As Object, e As RoutedEventArgs) Handles cb_other_disableVolumeOSD.Checked, cb_other_disableVolumeOSD.Unchecked
        If cb_other_disableVolumeOSD.IsChecked = True Then lib_hVOSD.HideOSD() Else lib_hVOSD.ShowOSD()
    End Sub

    Private Sub cb_wndmain_blur_enabled_Checked(sender As Object, e As RoutedEventArgs) Handles cb_wndmain_blur_enabled.Checked, cb_wndmain_blur_enabled.Unchecked
        cls_config.ui_blur_enabled = cb_wndmain_blur_enabled.IsChecked.Value
    End Sub

#End Region

#Region "Tab Navigation"
    Private Sub ico_nav_home_Click(sender As Object, e As RoutedEventArgs) Handles ico_nav_home.MouseLeftButtonDown
        If matc_tabctrl.SelectedIndex = 2 Or matc_tabctrl.SelectedIndex = 5 Then
            matc_tabctrl.SelectedIndex = 6
            lbl_header.Content = "INFO & SUPPORT"
            Exit Sub
        End If

        nav_home()
    End Sub

    Private Sub nav_home()
        matc_tabctrl.SelectedIndex = 0
        lbl_header.Content = "EINSTELLUNGEN"
        lasttab_lic = False

        anim_sep_pos(sep_nav_highlight, 0)
        anim_width(sep_nav_highlight, grd_navbar.ActualWidth)
    End Sub

    Private Sub matc_tabctrl_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles matc_tabctrl.SelectionChanged
        If Not matc_tabctrl.SelectedIndex = 0 Then
            sep_nav_highlight.Background = New SolidColorBrush(Color.FromArgb(255, 0, 241, 159))
            ico_nav_home.Visibility = Visibility.Visible

            anim_img_pos(ico_nav_home, 120)
            anim_grd_pos(grd_navbar, 25)
        Else
            anim_img_pos(ico_nav_home, 100)
            anim_grd_pos(grd_navbar, 0)

            sep_nav_highlight.Background = New SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
            ico_nav_home.Visibility = Visibility.Hidden
        End If
    End Sub

    Private Sub btn_page_weather_Click(sender As Object, e As RoutedEventArgs) Handles btn_page_weather.Click
        matc_tabctrl.SelectedIndex = 1
        lbl_header.Content = "WETTER"

        If cls_weather.conf_wcom_enabled = True Then
            lbl_wcom_check.Content = "( ! ) wetter.com wird zum abrufen der aktuellen Wetterlage genutzt."
        Else
            lbl_wcom_check.Visibility = Visibility.Hidden
        End If

        anim_width(sep_nav_highlight, btn_page_weather.Width)
        anim_sep_pos(sep_nav_highlight, btn_page_weather.Margin.Left)
    End Sub

    Private Async Sub btn_page_media_Click(sender As Object, e As RoutedEventArgs) Handles btn_page_media.Click
        matc_tabctrl.SelectedIndex = 3
        lbl_header.Content = "SPOTIFY"

        check_spotify_installation()
        lbl_cacheSize.Content = Await Task.Run(Function() get_cache_size())

        anim_width(sep_nav_highlight, btn_page_media.Width)
        anim_sep_pos(sep_nav_highlight, btn_page_media.Margin.Left)
    End Sub

    Private Sub btn_page_updates_Click(sender As Object, e As RoutedEventArgs) Handles btn_page_updates.Click
        If updater_enabled = False Then
            show_flyout("Die suche nach Updates ist in diesem Build deaktiviert", False)
            Exit Sub
        End If

        matc_tabctrl.SelectedIndex = 4
        lbl_header.Content = "UPDATES"

        anim_width(sep_nav_highlight, btn_page_updates.Width)
        anim_sep_pos(sep_nav_highlight, btn_page_updates.Margin.Left)
    End Sub

    Private Sub btn_page_about_Click(sender As Object, e As RoutedEventArgs) Handles btn_page_about.Click
        matc_tabctrl.SelectedIndex = 6
        lbl_header.Content = "INFO & SUPPORT"
        anim_width(sep_nav_highlight, btn_page_about.Width)
        anim_sep_pos(sep_nav_highlight, btn_page_about.Margin.Left)
        lbl_cpr.Content = "v" & MainWindow.suiversion & " - " & cls_config.build_version
    End Sub


#End Region

#Region "Weather & API-Key Overlay"
    Private Sub cb_wndmain_weather_enabled_Checked(sender As Object, e As RoutedEventArgs) Handles cb_wndmain_weather_enabled.Click
        If txtBx_weather_APIkey.Text = "0" Or txtBx_weather_cid.Text = "0" Then
            cb_wndmain_weather_enabled.IsChecked = False
            show_flyout("API-Key oder City-ID ungültig!", False)
        End If
    End Sub

    Private Async Sub weather_api_test()
        lbl_wcom_check.Visibility = Visibility.Hidden
        btn_ovlay_setKey.Visibility = Visibility.Hidden

        show_flyout("Einen moment, bitte...", True, 0)

        Dim oww_api_request As WebRequest = WebRequest.Create("http://api.openweathermap.org/data/2.5/weather?id=" & txtBx_weather_cid.Text & "&APPID=" & txtBx_weather_APIkey.Text & "&mode=xml&units=metric&lang=DE")
        Dim oww_api_respone As WebResponse
        Dim oww_api_txt As String = ""

        Try
            'server response
            oww_api_respone = Await oww_api_request.GetResponseAsync()
            Dim oww_api_dataStream As Stream = oww_api_respone.GetResponseStream()
            ' Open the stream using a StreamReader for easy access.
            Dim oww_api_reader As New StreamReader(oww_api_dataStream)
            oww_api_txt = oww_api_reader.ReadToEnd

            If oww_api_txt.Length > 1 And oww_api_txt.Contains("temp") Then
                show_flyout("API-Test Erfolgreich!", False, 0)
                Await Task.Run(Sub() Threading.Thread.Sleep(2500))
                show_flyout("Vergessen Sie nicht, ihre Änderungen zu Speichern!", False)
                matc_tabctrl.SelectedIndex = 0
            Else
                show_flyout("Fehler beim API-Test (!)", False, 0)
                Await Task.Run(Sub() Threading.Thread.Sleep(2500))
                show_flyout("API-Key & City-ID prüfen", False)
            End If

            lbl_wcom_check.Visibility = Visibility.Visible
            btn_ovlay_setKey.Visibility = Visibility.Visible

        Catch ex As WebException
            api_errmsg("API-Key & City-ID prüfen (PE)")
        End Try

    End Sub

    Private Sub api_errmsg(e_str As String)
        show_flyout(e_str, False)
        lbl_wcom_check.Visibility = Visibility.Visible
        btn_ovlay_setKey.Visibility = Visibility.Visible
    End Sub

    'API Overlay
    Private Sub btn_ovlay_setKey_Click(sender As Object, e As RoutedEventArgs) Handles btn_ovlay_setKey.Click
        weather_api_test()
    End Sub

    'Private Sub btn_overlay_show_Click(sender As Object, e As RoutedEventArgs) Handles btn_overlay_show.Click
    '    matc_tabctrl.SelectedIndex = 1
    '    lbl_header.Content = "WETTER"

    '    If cls_weather.conf_wcom_enabled = True Then
    '        lbl_wcom_check.Content = "( ! ) wetter.com wird zum abrufen der aktuellen Wetterlage genutzt."
    '    Else
    '        lbl_wcom_check.Visibility = Visibility.Hidden
    '    End If
    'End Sub

    Private Sub btn_oww_getKey_Click(sender As Object, e As RoutedEventArgs) Handles btn_oww_getKey.Click
        Process.Start("http://openweathermap.org/appid")
    End Sub

#End Region

#Region "Changelog & 3rd Party"
    Dim chglog_loaded As Integer = 0
    Dim lasttab_lic As Boolean = False

    Private Sub btn_changelog_Click(sender As Object, e As RoutedEventArgs) Handles btn_changelog.Click
        matc_tabctrl.SelectedIndex = 2
        lbl_header.Content = "CHANGELOG"

        If chglog_loaded <> 1 Then changelog_load()
    End Sub

    Private Sub changelog_load()
        If File.Exists(".\changelog") Then
            Dim docpath As String = ".\changelog"
            Dim range As System.Windows.Documents.TextRange
            Dim fStream As System.IO.FileStream
            If System.IO.File.Exists(docpath) Then
                Try
                    'Open the document in the RichTextBox.
                    range = New System.Windows.Documents.TextRange(rtb_chLog.Document.ContentStart, rtb_chLog.Document.ContentEnd)
                    fStream = New System.IO.FileStream(docpath, IO.FileMode.Open)
                    range.Load(fStream, DataFormats.Text)
                    fStream.Close()
                    chglog_loaded = 1

                Catch generatedExceptionName As System.Exception
                    show_flyout("Couldn't load Changelog", False)
                    If chglog_loaded = -1 Then Exit Sub

                    rtb_chLog.Document.ContentStart.InsertTextInRun("Changelog konnte nicht geladen werden")
                    chglog_loaded = -1
                End Try
            End If
        Else
            show_flyout("Couldn't load Changelog", False)
            If chglog_loaded = -1 Then Exit Sub

            rtb_chLog.Document.ContentStart.InsertTextInRun("Changelog nicht verfügbar")
            chglog_loaded = -1
        End If
    End Sub

    '3rd Party Software
    Private Sub btn_info_3rdpty_Click(sender As Object, e As RoutedEventArgs) Handles btn_info_3rdpty.Click
        matc_tabctrl.SelectedIndex = 5
        lbl_header.Content = "DRITTANBIETER SOFTWARE"
        lasttab_lic = True
    End Sub

    Private Sub scrlvwr_3rdpty_ScrollChanged(sender As Object, e As ScrollChangedEventArgs) Handles scrlvwr_3rdpty.ScrollChanged
        'lbl_test.Content = e.VerticalOffset
        If e.VerticalOffset < 310 Then grd_3rdpty_shadow.Visibility = Visibility.Visible Else grd_3rdpty_shadow.Visibility = Visibility.Hidden
    End Sub

#Region "Credits"
    Dim licvwr As New wnd_licensevwr

    Private Sub lbl_coreaudio_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_coreaudio_url.MouseLeftButtonDown
        Process.Start("http://whenimbored.xfx.net/download-links/?did=5")
    End Sub

    Private Sub lbl_mahapps_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_mahapps_url.MouseLeftButtonDown
        Process.Start("http://mahapps.com")
    End Sub

    Private Sub lbl_newtonsoft_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_newtonsoft_url.MouseLeftButtonDown
        Process.Start("http://www.newtonsoft.com/json")
    End Sub

    Private Sub lbl_nSpotify_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_sAPI_url.MouseLeftButtonDown
        Process.Start("https://github.com/JohnnyCrazy/SpotifyAPI-NET/releases")
    End Sub

    Private Sub lbl_hvOSD_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_hvOSD_url.MouseLeftButtonDown
        Process.Start("http://wordpress.venturi.de/?p=1")
    End Sub

    Private Sub lbl_nUpdate_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_nUpdate_url.MouseLeftButtonDown
        Process.Start("https://www.nupdate.net/")
    End Sub

    Private Sub btn_hvOSD_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_hvOSD_license.Click
        licvwr.show_license(".\licenses\hvOSD_license.txt", "HideVolumeOSD Lizenz")
    End Sub

    Private Sub btn_sAPI_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_sAPI_license.Click
        licvwr.show_license(".\licenses\sAPI_license.txt", "SpotifyAPI-NET Lizenz")
    End Sub

    Private Sub btn_nUpdate_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_nUpdate_license.Click
        licvwr.show_license(".\licenses\nUpdate_license.txt", "nUpdate Lizenz")
    End Sub

    Private Sub btn_newtonsoft_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_newtonsoft_license.Click
        licvwr.show_license(".\licenses\Json.NET_license.txt", "Json.NET Lizenz")
    End Sub

    Private Sub btn_mahapps_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_mahapps_license.Click
        licvwr.show_license(".\licenses\MahApps.Metro_license.txt", "mahapps.metro Lizenz")
    End Sub

    Private Sub btn_coreaudio_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_coreaudio_license.Click
        licvwr.show_license(".\licenses\coreaudio-dotnet_license.txt", "Core Audio .NET Wrapper Lizenz")
    End Sub

#End Region

    Private Sub get_3rdptysw_versions()
        If IO.File.Exists(".\CoreAudioApi.dll") Then lbl_coreaudio_version.Content = "Version: " & FileVersionInfo.GetVersionInfo(".\CoreAudioApi.dll").FileVersion
        If IO.File.Exists(".\MahApps.Metro.dll") Then lbl_mahapps_version.Content = "Version: " & FileVersionInfo.GetVersionInfo(".\MahApps.Metro.dll").FileVersion
        If IO.File.Exists(".\Newtonsoft.Json.dll") Then lbl_newtonsoft_version.Content = "Version: " & FileVersionInfo.GetVersionInfo(".\Newtonsoft.Json.dll").FileVersion
        lbl_sAPI_version.Content = "Version: 2.17.0"
        'lbl_nUpdate_version.Content = "Version: " & FileVersionInfo.GetVersionInfo(".\nUpdate.Internal.dll").FileVersion
    End Sub
#End Region

#Region "Spotify"
    Dim media_spotify_type As String

    Private Sub check_spotify_installation()
        Dim spotify_cnt As Integer
        Dim spotify_str As String
        For Each prog As Process In Process.GetProcesses
            If prog.ProcessName = "Spotify" Then
                spotify_cnt += 1
            End If
        Next
        If spotify_cnt > 1 Then spotify_str = " - läuft" Else spotify_str = " - läuft nicht"

        If IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\Packages\SpotifyAB.SpotifyMusic_zpdnekdrzrea0") Then
            media_spotify_type = "UWP"
            lbl_spotifyCheck.Content = "Spotify (UWP / MS Store Version) installiert" & spotify_str
        ElseIf IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\Spotify\Spotify.exe") Then
            media_spotify_type = "normal"
            lbl_spotifyCheck.Content = "Spotify (Desktop) installiert" & spotify_str
        Else
            media_spotify_type = "N/A"
            lbl_spotifyCheck.Content = "Spotify ist nicht installiert"
        End If
    End Sub

    Private Sub btn_restart_spotify_Click(sender As Object, e As RoutedEventArgs) Handles btn_restart_spotify.Click
        If media_spotify_type <> "N/A" Then flyout_spotify("force_restart") Else show_flyout("Spotify nicht installiert!", False)
    End Sub

    Private Sub flyout_spotify(ctnt As String, ByVal Optional e_csize As String = "")
        Select Case ctnt
            Case "del_cache"
                lbl_reset_cache_header.Content = "Daten im Cache löschen?"
                btn_flyout_spotify_confirm.Tag = "del_cache"
                lbl_flyout_spotify_msg.Content = "Alle Albumcover werden entfernt, es werden " & e_csize & " Speicherplatz freigegeben." & NewLine & "Die Cover werden bei bedarf erneut heruntergeladen."
                btn_flyout_spotify_confirm.Content = "Löschen"

            Case "force_restart"
                If media_spotify_type <> "UWP" Then
                    lbl_reset_cache_header.Content = "Spotify neu-starten?"
                    btn_flyout_spotify_confirm.Tag = "force_restart"
                    lbl_flyout_spotify_msg.Content = "Der Spotify Client wird beendet und neu-gestartet"
                    btn_flyout_spotify_confirm.Content = "Neu starten"
                Else
                    show_flyout("Spotify UWP kann nicht neu gestartet werden", False)
                    Exit Sub
                End If

        End Select

        flyout_cache_reset.IsOpen = True
        Dim c_blur As New Effects.BlurEffect With {.Radius = 10}
        matc_tabctrl.Effect = c_blur
    End Sub

#Region "AlbumArt Cache"
    Private Sub btn_cache_refresh_Click(sender As Object, e As RoutedEventArgs) Handles btn_cache_refresh.Click
        check_spotify_installation()
        lbl_cacheSize.Content = get_cache_size()
    End Sub

    Private Sub btn_cache_reset_Click(sender As Object, e As RoutedEventArgs) Handles btn_cache_reset.Click
        flyout_spotify("del_cache", get_cache_size(True))
    End Sub

    Private Sub btn_reset_cache_confirm_Click(sender As Object, e As RoutedEventArgs) Handles btn_flyout_spotify_confirm.Click
        If btn_flyout_spotify_confirm.Tag Is "del_cache" Then

            If IO.Directory.Exists(AppDomain.CurrentDomain.BaseDirectory & "cache\media\") Then
                For Each file As String In System.IO.Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory & "cache\media\", "*.*", System.IO.SearchOption.AllDirectories)
                    Try
                        IO.File.Delete(file)
                    Catch ex As Exception
                    End Try
                Next
            End If

            lbl_cacheSize.Content = get_cache_size()

        ElseIf btn_flyout_spotify_confirm.Tag Is "force_restart" Then
            show_flyout("Spotify wird neu-gestartet...", True)
            Topmost = True

            For Each prog As Process In Process.GetProcesses
                If prog.ProcessName = "Spotify" Or prog.ProcessName = "SpotifyWebHelper" Then
                    prog.Kill()
                End If
            Next

            Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\Spotify\SpotifyLauncher.exe")
        End If

        flyout_cache_reset.IsOpen = False
        matc_tabctrl.Effect = Nothing
    End Sub

    Private Sub btn_reset_cache_cancel_Click(sender As Object, e As RoutedEventArgs) Handles btn_reset_cache_cancel.Click
        flyout_cache_reset.IsOpen = False
        matc_tabctrl.Effect = Nothing
    End Sub

    Private Function get_cache_size(ByVal Optional e_notext As Boolean = False) As String
        Dim files_count As Integer
        Dim cache_size As Integer

        If IO.Directory.Exists(AppDomain.CurrentDomain.BaseDirectory & "cache\media\") Then
            For Each file As String In System.IO.Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory & "cache\media\", "*.*", System.IO.SearchOption.AllDirectories)
                cache_size += CInt(My.Computer.FileSystem.GetFileInfo(file).Length)
                files_count += 1
            Next
        Else
            If e_notext = False Then
                Return "Keine Album-Cover zwischengespeichert"
            Else
                Return "0MB"
            End If
        End If

        If e_notext = False Then
            If cache_size = 0 Then
                Return "Keine Album-Cover zwischengespeichert"
            ElseIf cache_size < 1048576 Then
                Return "Album-Cover zwischengespeichert: " & files_count & " (" & (cache_size / 1024).ToString("0") & "KB)" & " (" & ((cache_size / files_count) / 1024).ToString("0.0") & "KB/Cover)"
            Else
                Return "Album-Cover zwischengespeichert: " & files_count & " (" & (cache_size / 1048576).ToString("0.0") & "MB)" & " (" & ((cache_size / files_count) / 1024).ToString("0.0") & "KB/Cover)"
            End If
        Else
            Return (cache_size / 1048576).ToString("0.0") & "MB"
        End If
    End Function
#End Region

#End Region

#Region "Flyout Message"
    Private Sub show_flyout(ByVal e_msg As String, ByVal e_show_pring As Boolean, ByVal Optional e_timeout As Integer = -1)
        lbl_flyout_message.Content = e_msg
        If e_show_pring = True Then pring_flyout_message.Visibility = Visibility.Visible Else pring_flyout_message.Visibility = Visibility.Hidden

        If e_timeout = -1 Then
            flyout_message.IsAutoCloseEnabled = True
        ElseIf e_timeout = 0 Then
            flyout_message.IsAutoCloseEnabled = False
        Else
            flyout_message.IsAutoCloseEnabled = True
            flyout_message.AutoCloseInterval = e_timeout
        End If

        grd_bottom_strip.Visibility = Visibility.Hidden
        grd_bottom_strip_weather.Visibility = Visibility.Hidden

        flyout_message.IsOpen = True
    End Sub

    Private Sub close_flyout()
        flyout_message.IsOpen = False
    End Sub

    Private Sub flyout_message_closed(sender As Object, e As RoutedEventArgs) Handles flyout_message.ClosingFinished
        pring_flyout_message.Visibility = Visibility.Hidden
        grd_bottom_strip.Visibility = Visibility.Visible
        grd_bottom_strip_weather.Visibility = Visibility.Visible
        If matc_tabctrl.SelectedIndex = 1 Then matc_tabctrl.SelectedIndex = 0
        Topmost = False
    End Sub
#End Region

#Region "Animations"
    Private Sub anim_grd_pos(ByVal e_ctrl As Grid, ByVal e_pos_left As Double)
        Application.Current.Dispatcher.Invoke(Threading.DispatcherPriority.Normal, New ThreadStart(Sub()


                                                                                                       Dim ta As ThicknessAnimation = New ThicknessAnimation With {
                                                                                                                   .From = e_ctrl.Margin,
                                                                                                                   .[To] = New Thickness(e_pos_left, e_ctrl.Margin.Top, e_ctrl.Margin.Right, e_ctrl.Margin.Bottom),
                                                                                                                   .Duration = New Duration(TimeSpan.FromSeconds(0.5)),
                                                                                                                   .EasingFunction = New QuarticEase
                                                                                                               }
                                                                                                       e_ctrl.BeginAnimation(Grid.MarginProperty, ta)
                                                                                                   End Sub))
    End Sub

    Private Sub anim_sep_pos(ByVal e_ctrl As Separator, ByVal e_pos_left As Double)
        Application.Current.Dispatcher.Invoke(Threading.DispatcherPriority.Normal, New ThreadStart(Sub()


                                                                                                       Dim ta As ThicknessAnimation = New ThicknessAnimation With {
                                                                                                           .From = e_ctrl.Margin,
                                                                                                           .[To] = New Thickness(e_pos_left, e_ctrl.Margin.Top, e_ctrl.Margin.Right, e_ctrl.Margin.Bottom),
                                                                                                           .Duration = New Duration(TimeSpan.FromSeconds(0.5)),
                                                                                                           .EasingFunction = New QuarticEase
                                                                                                       }
                                                                                                       e_ctrl.BeginAnimation(Separator.MarginProperty, ta)
                                                                                                   End Sub))
    End Sub

    Private Sub anim_img_pos(ByVal e_ctrl As Image, ByVal e_pos_left As Double)
        Application.Current.Dispatcher.Invoke(Threading.DispatcherPriority.Normal, New ThreadStart(Sub()


                                                                                                       Dim ta As ThicknessAnimation = New ThicknessAnimation With {
                                                                                                                   .From = e_ctrl.Margin,
                                                                                                                   .[To] = New Thickness(e_pos_left, e_ctrl.Margin.Top, e_ctrl.Margin.Right, e_ctrl.Margin.Bottom),
                                                                                                                   .Duration = New Duration(TimeSpan.FromSeconds(0.5)),
                                                                                                                   .EasingFunction = New QuarticEase
                                                                                                               }
                                                                                                       e_ctrl.BeginAnimation(Grid.MarginProperty, ta)
                                                                                                   End Sub))
    End Sub

    Private Sub anim_width(ByVal e_ctrl As Separator, ByVal e_width As Double)
        Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Normal, New ThreadStart(Sub()


                                                                                                               Dim ta As DoubleAnimation = New DoubleAnimation With {
                                                                                                                   .From = e_ctrl.ActualWidth,
                                                                                                                   .[To] = e_width,
                                                                                                                   .Duration = New Duration(TimeSpan.FromSeconds(0.5)),
                                                                                                                   .EasingFunction = New QuarticEase
                                                                                                               }
                                                                                                               e_ctrl.BeginAnimation(Separator.WidthProperty, ta)
                                                                                                           End Sub))
    End Sub



#End Region

    Private Sub cb_other_startup_play_Checked(sender As Object, e As RoutedEventArgs) Handles cb_other_startup_play.Checked
        My.Computer.Audio.Play(AppDomain.CurrentDomain.BaseDirectory & "\Resources\win_vis_beta_startup.wav")
    End Sub

    Private Sub slider_bg_transp_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles slider_bg_transp.ValueChanged
        cls_config.ui_blur_transparency = CByte(slider_bg_transp.Value)
        cls_blur_behind.blur(Me, cls_config.ui_blur_enabled)
    End Sub

    Private Sub slider_netmon_threshold_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles slider_netmon_threshold.ValueChanged
        lbl_netmon_threshold.Content = Math.Round(slider_netmon_threshold.Value, 0) & " KB/s"
    End Sub

End Class
