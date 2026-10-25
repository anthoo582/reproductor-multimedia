using System.Drawing;
using System.Windows.Forms;
using MpvNet;

namespace MpvNet.Windows.WinForms.Controls;

public class PlaylistPanel : UserControl
{
    readonly ListBox _listBox;
    readonly Button _btnAdd;
    readonly Button _btnRemove;
    readonly Button _btnClear;
    readonly Button _btnShuffle;

    public PlaylistPanel()
    {
        Dock = DockStyle.Right;
        Width = 250;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        _listBox = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            IntegralHeight = false
        };
        _listBox.DoubleClick += (s, e) =>
        {
            if (_listBox.SelectedIndex < 0)
                return;

            Player.CommandV("playlist-play-index", _listBox.SelectedIndex.ToString());
        };

        var panelTop = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(50, 50, 50),
            WrapContents = false
        };

        _btnAdd = new Button { Text = "+", Width = 30 };
        _btnRemove = new Button { Text = "-", Width = 30 };
        _btnClear = new Button { Text = "Clear", Width = 50 };
        _btnShuffle = new Button { Text = "Shuffle", Width = 60 };

        _btnAdd.Click += (s, e) =>
        {
            using var ofd = new OpenFileDialog { Multiselect = true };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            foreach (string file in ofd.FileNames)
                Player.CommandV("loadfile", MainPlayer.PrepareFileForPlayback(file), "append");
        };

        _btnRemove.Click += (s, e) =>
        {
            if (_listBox.SelectedIndex < 0)
                return;

            Player.CommandV("playlist-remove", _listBox.SelectedIndex.ToString());
        };

        _btnClear.Click += (s, e) => Player.Command("playlist-clear");
        _btnShuffle.Click += (s, e) => Player.Command("playlist-shuffle");

        panelTop.Controls.AddRange(new Control[] { _btnAdd, _btnRemove, _btnClear, _btnShuffle });

        Controls.Add(_listBox);
        Controls.Add(panelTop);
    }

    public void RefreshPlaylist()
    {
        if (InvokeRequired)
        {
            Invoke(RefreshPlaylist);
            return;
        }

        int count = 0;

        try
        {
            count = Player.GetPropertyInt("playlist-count");
        }
        catch
        {
        }

        _listBox.BeginUpdate();
        _listBox.Items.Clear();

        for (int i = 0; i < count; i++)
        {
            string title = "";

            try
            {
                title = Player.GetPropertyString($"playlist/{i}/title");
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(title))
            {
                try
                {
                    title = Path.GetFileName(Player.GetPropertyString($"playlist/{i}/filename"));
                }
                catch
                {
                }
            }

            _listBox.Items.Add(string.IsNullOrEmpty(title) ? $"Item {i}" : title);
        }

        try
        {
            _listBox.SelectedIndex = Player.GetPropertyInt("playlist-pos");
        }
        catch
        {
        }

        _listBox.EndUpdate();
    }
}
