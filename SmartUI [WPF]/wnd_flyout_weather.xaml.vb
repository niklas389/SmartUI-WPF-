﻿Imports System
Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Media.Animation

Public Class wnd_flyout_weather

#Region "Window"
    Private Sub wnd_flyout_volume_LostFocus(sender As Object, e As RoutedEventArgs) Handles Me.MouseLeave
        animate_wnd(0, Me.Height * -1)
    End Sub

    Private Sub lbl_now_temp_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_now_temp.SizeChanged
        img_now_condition.Margin = New Thickness(lbl_now_temp.RenderSize.Width + 5, 10, 0, 0)
        lbl_now_txt.Margin = New Thickness(lbl_now_temp.RenderSize.Width, 40, 0, 0)
    End Sub
#End Region

    Private Sub wwnd_flyout_weather_Loaded(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Hidden Then Exit Sub
        cls_blur_behind.blur(Me, cls_config.ui_blur_enabled)

        Left = My.Computer.Screen.WorkingArea.Left
        Height = 255

        lbl_location.Content = cls_weather.oww_data_location

        lbl_now_temp.Content = cls_weather.get_temp
        lbl_now_txt.Content = cls_weather.oww_condition
        wpf_helper.helper_image(img_now_condition, cls_weather.get_condition_pic)
        wpf_helper.helper_image(img_weather_bg, cls_weather.get_condition_pic(True))

        lbl_now_windspeed.Content = Math.Round(CDbl(cls_weather.get_wind_speed), 1)
        lbl_now_winddir_deg.Content = cls_weather.get_wind_dir
        img_now_winddir.RenderTransform = New RotateTransform(cls_weather.get_wind_dir)
        lbl_now_winddir.Content = cls_weather.oww_winddir.Replace("E", "O")

        lbl_now_humidity.Content = cls_weather.get_humidity
        lbl_now_pressure.Content = cls_weather.get_pressure.ToString

        lbl_now_rain.Content = cls_weather.wcom_rain

        animate_wnd(Me.Height * -1, 0)
    End Sub

    Private Sub animate_wnd(ByVal e_from As Double, ByVal e_to As Double)
        Dim dblanim As New DoubleAnimation With {
            .From = e_from,
            .To = (e_to),
            .Duration = TimeSpan.FromSeconds(0.5),
            .EasingFunction = New QuarticEase
        }

        Dim storyboard As New Storyboard()
        Storyboard.SetTarget(dblanim, Me)
        Storyboard.SetTargetProperty(dblanim, New PropertyPath(Window.TopProperty))

        If e_from = 0 Then AddHandler dblanim.Completed, AddressOf dblanim_Completed

        storyboard.Children.Add(dblanim)
        storyboard.Begin(Me)
    End Sub

    Private Sub dblanim_Completed(sender As Object, e As EventArgs)
        Me.Hide()
    End Sub
End Class
