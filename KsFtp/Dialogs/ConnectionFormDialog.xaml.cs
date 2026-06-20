using System;
using System.Windows;
using System.Windows.Controls;
using KsFtp.Models;

namespace KsFtp.Dialogs;

public partial class ConnectionFormDialog : Window
{
    public ConnectionProfile? Result { get; private set; }
    private readonly ConnectionProfile? _editing;

    public ConnectionFormDialog(ConnectionProfile? existing)
    {
        InitializeComponent();
        _editing = existing;

        ProtocolCombo.SelectedIndex = 0;
        EncodingCombo.SelectedIndex = 0; // Shift-JIS デフォルト

        if (existing != null)
        {
            TitleText.Text = "接続を編集";
            NameBox.Text = existing.Name;
            ProtocolCombo.SelectedIndex = existing.ProtocolType switch
            {
                FtpProtocol.Ftps => 1,
                FtpProtocol.Sftp => 2,
                _ => 0
            };
            HostBox.Text = existing.Host;
            PortBox.Text = existing.Port.ToString();
            UsernameBox.Text = existing.Username;
            PasswordBox.Password = existing.Password;
            InitialPathBox.Text = existing.InitialPath?.Trim('/') ?? "";
            EncodingCombo.SelectedIndex = (existing.FileEncoding ?? "shift_jis") == "utf-8" ? 1 : 0;
        }
        else
        {
            NameBox.Text = "新しい接続";
            UsernameBox.Text = "anonymous";
            PortBox.Text = "21";
        }
        UpdateEncodingVisibility();
    }

    private void Protocol_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (PortBox == null) return;
        var selected = (ProtocolCombo.SelectedItem as ComboBoxItem)?.Tag as string;
        PortBox.Text = selected == "Sftp" ? "22" : "21";
        UpdateEncodingVisibility();
    }

    private void UpdateEncodingVisibility()
    {
        if (EncodingPanel == null) return;
        var selected = (ProtocolCombo.SelectedItem as ComboBoxItem)?.Tag as string;
        // SFTP は常に UTF-8 なので選択不要
        EncodingPanel.Visibility = selected == "Sftp"
            ? System.Windows.Visibility.Collapsed
            : System.Windows.Visibility.Visible;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(HostBox.Text))
        {
            MessageBox.Show("ホストを入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(PortBox.Text, out var port) || port < 1 || port > 65535)
        {
            MessageBox.Show("有効なポート番号を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selected = (ProtocolCombo.SelectedItem as ComboBoxItem)?.Tag as string;
        var protocol = selected switch
        {
            "Ftps" => FtpProtocol.Ftps,
            "Sftp" => FtpProtocol.Sftp,
            _ => FtpProtocol.Ftp
        };

        var encoding = (EncodingCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "shift_jis";

        Result = new ConnectionProfile
        {
            Id = _editing?.Id ?? Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(NameBox.Text) ? HostBox.Text : NameBox.Text.Trim(),
            Host = HostBox.Text.Trim(),
            Port = port,
            ProtocolType = protocol,
            Username = UsernameBox.Text.Trim(),
            Password = PasswordBox.Password,
            InitialPath = InitialPathBox.Text.Trim().Trim('/'),
            FileEncoding = protocol == FtpProtocol.Sftp ? "utf-8" : encoding
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
