namespace SimpleBrowser;

public class SettingsForm : Form
{
    private readonly TextBox _homePage;
    private readonly TextBox _searchEngine;
    private readonly CheckBox _statusBar;
    private readonly CheckBox _devTools;

    public AppSettings Settings { get; }

    public SettingsForm(AppSettings current)
    {
        Settings = new AppSettings
        {
            HomePage = current.HomePage,
            SearchEngine = current.SearchEngine,
            IsStatusBarEnabled = current.IsStatusBarEnabled,
            AreDevToolsEnabled = current.AreDevToolsEnabled
        };

        Text = "Настройки";
        Width = 560;
        Height = 300;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 4; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _homePage = new TextBox { Dock = DockStyle.Fill, Text = Settings.HomePage };
        _searchEngine = new TextBox { Dock = DockStyle.Fill, Text = Settings.SearchEngine };
        _statusBar = new CheckBox { Text = "Показывать строку состояния", Checked = Settings.IsStatusBarEnabled, AutoSize = true };
        _devTools = new CheckBox { Text = "Разрешить DevTools (F12)", Checked = Settings.AreDevToolsEnabled, AutoSize = true };

        layout.Controls.Add(new Label { Text = "Домашняя страница:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_homePage, 1, 0);
        layout.Controls.Add(new Label { Text = "Поиск (URL с {0}):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_searchEngine, 1, 1);
        layout.Controls.Add(_statusBar, 1, 2);
        layout.Controls.Add(_devTools, 1, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 90 };
        var ok = new Button { Text = "Сохранить", Width = 110 };
        ok.Click += (_, _) =>
        {
            Settings.HomePage = _homePage.Text.Trim();
            Settings.SearchEngine = string.IsNullOrWhiteSpace(_searchEngine.Text)
                ? "https://www.google.com/search?q={0}"
                : _searchEngine.Text.Trim();
            Settings.IsStatusBarEnabled = _statusBar.Checked;
            Settings.AreDevToolsEnabled = _devTools.Checked;
            DialogResult = DialogResult.OK;
            Close();
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        layout.Controls.Add(buttons, 0, 5);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}