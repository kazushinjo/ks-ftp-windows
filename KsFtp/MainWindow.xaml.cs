using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using KsFtp.Dialogs;
using KsFtp.Models;
using KsFtp.ViewModels;

namespace KsFtp;

public partial class MainWindow : Window
{
    private readonly AppState _state;
    private bool _transferVisible = false;

    public MainWindow()
    {
        InitializeComponent();
        _state = new AppState();
        DataContext = _state;
        _state.Profiles.CollectionChanged += (_, _) => RebuildProfileList();
        RebuildProfileList();
    }

    // ===== Sidebar =====

    private void RebuildProfileList()
    {
        ProfilesPanel.Children.Clear();
        foreach (var profile in _state.Profiles)
        {
            var btn = BuildProfileButton(profile);
            ProfilesPanel.Children.Add(btn);
        }
        if (_state.Profiles.Count == 0)
        {
            var placeholder = new TextBlock
            {
                Text = "接続先がありません",
                Foreground = System.Windows.Media.Brushes.Gray,
                FontSize = 12,
                Margin = new Thickness(8, 12, 8, 4),
                TextWrapping = TextWrapping.Wrap
            };
            ProfilesPanel.Children.Add(placeholder);
        }
    }

    private Border BuildProfileButton(ConnectionProfile profile)
    {
        var isConnected = _state.SelectedProfile?.Id == profile.Id;

        var iconBg = new Border
        {
            Width = 32, Height = 32,
            CornerRadius = new CornerRadius(6),
            Background = isConnected
                ? System.Windows.Media.Brushes.DodgerBlue
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255)),
            Child = new TextBlock
            {
                Text = profile.ProtocolType switch
                {
                    FtpProtocol.Ftps => "🔒",
                    FtpProtocol.Sftp => "🔑",
                    _ => "🖧"
                },
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji"),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var nameText = new TextBlock
        {
            Text = profile.Name,
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 13,
            FontWeight = isConnected ? FontWeights.SemiBold : FontWeights.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var subText = new TextBlock
        {
            Text = $"{profile.ProtocolDisplay}  {profile.Host}",
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 153, 153)),
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var textStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        textStack.Children.Add(nameText);
        textStack.Children.Add(subText);

        var dot = new Ellipse
        {
            Width = 7, Height = 7,
            Fill = System.Windows.Media.Brushes.LimeGreen,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = isConnected ? Visibility.Visible : Visibility.Collapsed
        };

        var innerGrid = new Grid { Margin = new Thickness(4, 6, 4, 6) };
        innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(iconBg, 0);
        Grid.SetColumn(textStack, 1);
        Grid.SetColumn(dot, 2);
        innerGrid.Children.Add(iconBg);
        innerGrid.Children.Add(textStack);
        innerGrid.Children.Add(dot);

        var border = new Border
        {
            CornerRadius = new CornerRadius(6),
            Cursor = Cursors.Hand,
            Background = System.Windows.Media.Brushes.Transparent,
            Child = innerGrid
        };

        border.MouseEnter += (_, _) =>
            border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255));
        border.MouseLeave += (_, _) =>
            border.Background = System.Windows.Media.Brushes.Transparent;
        border.MouseLeftButtonUp += async (_, _) => await ConnectToProfile(profile);

        var ctxMenu = new ContextMenu();
        var connectItem = new MenuItem { Header = "接続" };
        connectItem.Click += async (_, _) => await ConnectToProfile(profile);
        var editItem = new MenuItem { Header = "編集" };
        editItem.Click += (_, _) => EditProfile(profile);
        var deleteItem = new MenuItem { Header = "削除" };
        deleteItem.Click += (_, _) => DeleteProfile(profile);
        ctxMenu.Items.Add(connectItem);
        ctxMenu.Items.Add(editItem);
        ctxMenu.Items.Add(new Separator());
        ctxMenu.Items.Add(deleteItem);
        border.ContextMenu = ctxMenu;

        return border;
    }

    private async System.Threading.Tasks.Task ConnectToProfile(ConnectionProfile profile)
    {
        try
        {
            await _state.Connect(profile);
            RemoteHeaderText.Text = $"リモート: {profile.Name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"接続に失敗しました:\n{ex.Message}", "接続エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RebuildProfileList();
        }
    }

    private void EditProfile(ConnectionProfile profile)
    {
        var dlg = new ConnectionFormDialog(profile) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result != null)
            _state.UpdateProfile(dlg.Result);
    }

    private void DeleteProfile(ConnectionProfile profile)
    {
        if (MessageBox.Show($"「{profile.Name}」を削除しますか？", "確認",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _state.DeleteProfile(profile);
            RebuildProfileList();
        }
    }

    private void AddProfile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ConnectionFormDialog(null) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result != null)
            _state.AddProfile(dlg.Result);
    }

    // ===== Transfer queue toggle =====

    private void ToggleTransfer_Click(object sender, RoutedEventArgs e)
    {
        _transferVisible = !_transferVisible;
        TransferRow.Height = _transferVisible ? new GridLength(180) : new GridLength(0);
    }

    private void ClearTransfers_Click(object sender, RoutedEventArgs e)
        => _state.ClearFinishedTransfers();

    // ===== Local pane =====

    private void LocalUp_Click(object sender, RoutedEventArgs e) => _state.LocalNavigateUp();

    private void LocalRefresh_Click(object sender, RoutedEventArgs e)
        => _state.LoadLocalDirectory(_state.LocalCurrentPath);

    private void LocalList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LocalListView.SelectedItem is LocalFile file)
            _state.OpenLocalItem(file);
    }

    private async void LocalUpload_Click(object sender, RoutedEventArgs e)
    {
        var selected = LocalListView.SelectedItems.Cast<LocalFile>()
            .Select(f => f.Path).ToList();
        if (selected.Count == 0) return;
        if (!_transferVisible) { _transferVisible = true; TransferRow.Height = new GridLength(180); }
        await _state.UploadFiles(selected);
    }

    private void LocalNewFolder_Click(object sender, RoutedEventArgs e)
    {
        var name = ShowInputDialog("フォルダ名を入力してください", "新しいフォルダ");
        if (!string.IsNullOrWhiteSpace(name))
            _state.CreateLocalDirectory(name);
    }

    private void LocalDelete_Click(object sender, RoutedEventArgs e)
    {
        var files = LocalListView.SelectedItems.Cast<LocalFile>().ToList();
        if (files.Count == 0) return;
        if (MessageBox.Show($"{files.Count} 件のアイテムを削除しますか？", "確認",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            _state.DeleteLocalItems(files);
    }

    // ===== Remote pane =====

    private async void RemoteBack_Click(object sender, RoutedEventArgs e) => await _state.NavigateBack();
    private async void RemoteForward_Click(object sender, RoutedEventArgs e) => await _state.NavigateForward();
    private async void RemoteUp_Click(object sender, RoutedEventArgs e) => await _state.NavigateUp();
    private async void RemoteRefresh_Click(object sender, RoutedEventArgs e) => await _state.Refresh();

    private async void RemotePathBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await _state.NavigateTo(RemotePathBox.Text);
    }

    private async void RemoteList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RemoteListView.SelectedItem is RemoteFile file && file.IsDirectory)
            await _state.NavigateTo(file.Path);
    }

    private async void RemoteDownload_Click(object sender, RoutedEventArgs e)
    {
        var files = RemoteListView.SelectedItems.Cast<RemoteFile>().ToList();
        if (files.Count == 0) return;
        if (!_transferVisible) { _transferVisible = true; TransferRow.Height = new GridLength(180); }
        await _state.DownloadFiles(files);
    }

    private async void RemoteNewFolder_Click(object sender, RoutedEventArgs e)
    {
        var name = ShowInputDialog("フォルダ名を入力してください", "新しいフォルダ");
        if (!string.IsNullOrWhiteSpace(name))
            await _state.CreateRemoteDirectory(name);
    }

    private async void RemoteNewFile_Click(object sender, RoutedEventArgs e)
    {
        var name = ShowInputDialog("ファイル名を入力してください", "新しいファイル.txt");
        if (!string.IsNullOrWhiteSpace(name))
            await _state.CreateRemoteFile(name);
    }

    private async void RemoteDelete_Click(object sender, RoutedEventArgs e)
    {
        var files = RemoteListView.SelectedItems.Cast<RemoteFile>().ToList();
        if (files.Count == 0) return;
        if (MessageBox.Show($"{files.Count} 件のアイテムを削除しますか？", "確認",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            await _state.DeleteRemoteItems(files);
    }

    // ===== Helpers =====

    private void ListViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListViewItem item && !item.IsSelected)
        {
            item.IsSelected = true;
            item.Focus();
        }
    }

    private static string? ShowInputDialog(string prompt, string defaultValue = "")
    {
        var win = new Window
        {
            Title = "入力",
            Width = 360, Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.White
        };
        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) };
        var box = new TextBox { Text = defaultValue, Padding = new Thickness(6, 4, 6, 4) };
        box.SelectAll();

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0), };
        var ok = new Button { Content = "OK", Padding = new Thickness(16, 6, 16, 6), Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
        var cancel = new Button { Content = "キャンセル", Padding = new Thickness(16, 6, 16, 6), IsCancel = true };
        ok.Click += (_, _) => win.DialogResult = true;
        cancel.Click += (_, _) => win.DialogResult = false;
        btnRow.Children.Add(cancel);
        btnRow.Children.Add(ok);

        Grid.SetRow(label, 0); Grid.SetRow(box, 1); Grid.SetRow(btnRow, 2);
        grid.Children.Add(label); grid.Children.Add(box); grid.Children.Add(btnRow);
        win.Content = grid;

        return win.ShowDialog() == true ? box.Text.Trim() : null;
    }
}
