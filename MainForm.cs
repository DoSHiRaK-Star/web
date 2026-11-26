using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SimpleBrowser;

public class MainForm : Form
{
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Проверяем нужные нам комбинации
        if (keyData == (Keys.Control | Keys.T))
        {
            _ = AddNewTabAsync(); // Ваш метод для новой вкладки
            return true; // Сообщаем, что мы обработали клавишу
        }
        if (keyData == (Keys.Control | Keys.W))
        {
            CloseTab(_tabs.SelectedIndex); // Ваш метод для закрытия вкладки
            return true;
        }
        if (keyData == (Keys.Control | Keys.L))
        {
            _addressBar.Focus();
            _addressBar.SelectAll();
            return true;
        }
        if (keyData == Keys.F5)
        {
            GetCurrentCore()?.Reload();
            return true;
        }

        // Для всех остальных клавиш вызываем базовую реализацию
        return base.ProcessCmdKey(ref msg, keyData);
    }
    private ZapretManager? _zapret;
    private readonly Button _zapretBtn = new() { Text = "Обход DPI", Dock = DockStyle.Fill };
    private AppSettings _settings;
    private CoreWebView2Environment? _environment;
    private const int CloseBtnSize = 14;   // размер квадрата с крестиком
                                           // ============================ Отрисовка вкладок с крестиком ============================

    private void Tabs_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var g = e.Graphics;
        var tabRect = _tabs.GetTabRect(e.Index);
        bool isSelected = (_tabs.SelectedIndex == e.Index);

        // --- фон и текст ---
        using var back = new SolidBrush(isSelected ? SystemColors.Window : SystemColors.Control);
        g.FillRectangle(back, tabRect);

        var textRect = new Rectangle(tabRect.X + 4, tabRect.Y,
                                     tabRect.Width - CloseBtnSize - 10, tabRect.Height);
        TextRenderer.DrawText(
            g,
            _tabs.TabPages[e.Index].Text,
            _tabs.Font,
            textRect,
            SystemColors.ControlText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        // --- крестик ---
        var closeRect = GetCloseRect(tabRect);
        bool hovered = _hoveredCloseIndex == e.Index;

        if (hovered)
        {
            using var hoverBack = new SolidBrush(Color.FromArgb(220, 60, 60));
            g.FillRectangle(hoverBack, closeRect);
        }

        using var pen = new Pen(hovered ? Color.White : Color.DimGray, 1.6f);
        int pad = 4;
        g.DrawLine(pen, closeRect.Left + pad, closeRect.Top + pad,
                        closeRect.Right - pad, closeRect.Bottom - pad);
        g.DrawLine(pen, closeRect.Right - pad, closeRect.Top + pad,
                        closeRect.Left + pad, closeRect.Bottom - pad);
    }

    /// <summary>Прямоугольник крестика внутри вкладки.</summary>
    private static Rectangle GetCloseRect(Rectangle tabRect)
    {
        int size = CloseBtnSize;
        int x = tabRect.Right - size - 4;
        int y = tabRect.Top + (tabRect.Height - size) / 2;
        return new Rectangle(x, y, size, size);
    }

    // ============================ Индекс вкладки под курсором и hover-эффект ============================

    private int _hoveredCloseIndex = -1;

    private void Tabs_MouseMove(object? sender, MouseEventArgs e)
    {
        int idx = GetCloseHitIndex(e.Location);

        if (idx != _hoveredCloseIndex)
        {
            _hoveredCloseIndex = idx;
            _tabs.Invalidate();                 // перерисовать, чтобы обновить hover
        }

        _tabs.Cursor = idx >= 0 ? Cursors.Hand : Cursors.Default;
    }

    private int GetCloseHitIndex(Point p)
    {
        for (int i = 0; i < _tabs.TabPages.Count; i++)
        {
            var rect = GetCloseRect(_tabs.GetTabRect(i));
            if (rect.Contains(p)) return i;
        }
        return -1;
    }

    // ============================ Закрытие вкладок ============================

    private void Tabs_MouseDown(object? sender, MouseEventArgs e)
    {
        // --- клик по крестику (левая кнопка) ---
        if (e.Button == MouseButtons.Left)
        {
            int idx = GetCloseHitIndex(e.Location);
            if (idx >= 0)
            {
                CloseTab(idx);
                return;
            }
        }

        // --- средний клик по вкладке тоже закрывает ---
        if (e.Button == MouseButtons.Middle)
        {
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                if (_tabs.GetTabRect(i).Contains(e.Location))
                {
                    CloseTab(i);
                    return;
                }
            }
        }
    }
    private async Task ToggleZapretAsync()
    {
        // Инициализируем менеджер, если он ещё не создан
        if (_zapret == null)
        {
            string winwsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Zapret", "bin","winws.exe");

            if (!File.Exists(winwsPath))
            {
                MessageBox.Show(
                    $"Не найден winws.exe.\n\nИскал здесь:\n{winwsPath}\n\n" +
                    $"Папка exe: {AppDomain.CurrentDomain.BaseDirectory}",
                    "Ошибка");
                return;
            }
            _zapret = new ZapretManager(winwsPath);
        }

        if (_zapret.IsRunning)
        {
            _zapret.Stop();
            _zapretBtn.Text = "Включить обход";
        }
        else
        {
            // Строка с аргументами. Её нужно адаптировать под вашего провайдера.
            // Эта строка — лишь пример. Рабочие параметры вы найдёте в логах blockcheck или в готовых .bat файлах.
            string args = "--wf-tcp=80,443 --wf-udp=443 --filter-tcp=443 --hostlist=\"Zapret/lists/list-general.txt\" --dpi-desync=fake,multidisorder --dpi-desync-split-pos=1,midsld --dpi-desync-fooling=md5sig";

            // Меняем текст кнопки на время запуска
            _zapretBtn.Text = "Запуск...";

            // Запускаем и ждём немного, чтобы процесс успел стартовать
            bool success = _zapret.Start(args);
            await Task.Delay(1000); // Даём время на UAC и старт процесса

            if (success && _zapret.IsRunning)
            {
                _zapretBtn.Text = "Обход DPI вкл.";
            }
            else
            {
                _zapretBtn.Text = "Ошибка запуска";
                // Сбрасываем текст через пару секунд
                await Task.Delay(2000);
                _zapretBtn.Text = "Включить обход";
            }
        }
    }


    private readonly TabControl _tabs = new()
    {
        Dock = DockStyle.Fill,
        Padding = new Point(18, 6),
        DrawMode = TabDrawMode.OwnerDrawFixed,
        SizeMode = TabSizeMode.Fixed,       // фиксированная ширина — проще попасть по крестику
        ItemSize = new Size(180, 24)
    };
    private readonly TextBox _addressBar = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
    private readonly ProgressBar _progress = new()
    {
        Dock = DockStyle.Bottom,
        Height = 4,
        Style = ProgressBarStyle.Marquee,
        Visible = false
    };
    private readonly Button _back        = new() { Text = "◀", Dock = DockStyle.Fill };
    private readonly Button _forward     = new() { Text = "▶", Dock = DockStyle.Fill };
    private readonly Button _reload      = new() { Text = "⟳", Dock = DockStyle.Fill };
    private readonly Button _home        = new() { Text = "🏠", Dock = DockStyle.Fill };
    private readonly Button _newTab      = new() { Text = "＋", Dock = DockStyle.Fill };
    private readonly Button _settingsBtn = new() { Text = "⚙", Dock = DockStyle.Fill };

    public MainForm()
    {
        _settings = AppSettings.Load();

        Text = "Мой браузер";
        Width = 1200;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;

        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            ColumnCount = 7,
            RowCount = 1,
            Padding = new Padding(4)
        };
        for (int i = 0; i < 6; i++)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        toolbar.Controls.Add(_back,        0, 0);
        toolbar.Controls.Add(_forward,     1, 0);
        toolbar.Controls.Add(_reload,      2, 0);
        toolbar.Controls.Add(_home,        3, 0);
        toolbar.Controls.Add(_newTab,      4, 0);
        toolbar.Controls.Add(_settingsBtn, 5, 0);
        toolbar.Controls.Add(_addressBar,  6, 0);

        Controls.Add(_tabs);
        Controls.Add(_progress);
        Controls.Add(toolbar);

        // --- Toolbar ---
        _back.Click        += (_, _) => { var c = GetCurrentCore(); if (c?.CanGoBack == true) c.GoBack(); };
        _forward.Click     += (_, _) => { var c = GetCurrentCore(); if (c?.CanGoForward == true) c.GoForward(); };
        _reload.Click      += (_, _) => GetCurrentCore()?.Reload();
        _home.Click        += (_, _) => GetCurrentCore()?.Navigate(_settings.HomePage);
        _newTab.Click      += async (_, _) => await AddNewTabAsync();
        _settingsBtn.Click += (_, _) => OpenSettings();
        toolbar.Controls.Add(_zapretBtn, 6, 0); // Измените индекс, если нужно
                                                // ...
                                                // Подписка на событие клика
        _zapretBtn.Click += async (_, _) => await ToggleZapretAsync();
        _addressBar.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                NavigateCurrent(_addressBar.Text);
            }
        };

        // --- Tabs ---
        _tabs.SelectedIndexChanged += (_, _) => SyncToolbarToTab();
        _tabs.DrawItem += Tabs_DrawItem;
        _tabs.MouseDown += Tabs_MouseDown;   // вместо/в дополнение к _tabs.MouseUp
        _tabs.MouseMove += Tabs_MouseMove;

        // --- Горячие клавиши на уровне формы ---
        KeyPreview = true;
        KeyDown += MainForm_KeyDown;

        Load += async (_, _) => await InitAsync();
    }

    // ============================ Инициализация ============================

    private async Task InitAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SimpleBrowser");

        _environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

        await AddNewTabAsync(_settings.HomePage);
    }

    private async Task AddNewTabAsync(string? url = null)
    {
        var wv = new WebView2 { Dock = DockStyle.Fill };
        var page = new TabPage("Загрузка…");
        page.Controls.Add(wv);
        _tabs.TabPages.Add(page);
        _tabs.SelectedTab = page;

        await wv.EnsureCoreWebView2Async(_environment);

        var core = wv.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.IsStatusBarEnabled = _settings.IsStatusBarEnabled;
        core.Settings.AreDevToolsEnabled = _settings.AreDevToolsEnabled;

        core.SourceChanged += (_, _) =>
        {
            if (GetCurrentWebView() == wv) _addressBar.Text = core.Source;
        };
        core.DocumentTitleChanged += (_, _) =>
        {
            page.Text = string.IsNullOrWhiteSpace(core.DocumentTitle)
                ? "Без названия"
                : Truncate(core.DocumentTitle, 32);
        };
        core.NavigationStarting += (_, _) =>
        {
            if (GetCurrentWebView() == wv) _progress.Visible = true;
        };
        core.NavigationCompleted += (_, e) =>
        {
            if (GetCurrentWebView() == wv)
            {
                _progress.Visible = false;
                UpdateNavButtons();
            }
        };
        core.HistoryChanged += (_, _) =>
        {
            if (GetCurrentWebView() == wv) UpdateNavButtons();
        };
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            _ = AddNewTabAsync(e.Uri);        // target="_blank" → новая вкладка
        };

        // Ctrl+T, Ctrl+W, Ctrl+L, F5, Alt+Left/Right внутри WebView2
        //core.AcceleratorKeyPressed += (_, e) => HandleWebViewAccelerator(e);

        core.Navigate(url ?? _settings.HomePage);
        UpdateNavButtons();
    }

    // ============================ Хелперы ============================

    private WebView2? GetCurrentWebView() =>
        _tabs.SelectedTab?.Controls.OfType<WebView2>().FirstOrDefault();

    private CoreWebView2? GetCurrentCore() => GetCurrentWebView()?.CoreWebView2;

    private void UpdateNavButtons()
    {
        var c = GetCurrentCore();
        _back.Enabled = c?.CanGoBack == true;
        _forward.Enabled = c?.CanGoForward == true;
    }

    private void SyncToolbarToTab()
    {
        var core = GetCurrentCore();
        _addressBar.Text = core?.Source ?? "";
        UpdateNavButtons();
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";

    private void NavigateCurrent(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        GetCurrentCore()?.Navigate(NormalizeUrl(input));
    }

    private string NormalizeUrl(string input)
    {
        input = input.Trim();

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return input;

        if (input.Contains('.') && !input.Contains(' '))
            return "https://" + input;

        return string.Format(_settings.SearchEngine, Uri.EscapeDataString(input));
    }

    // ============================ Закрытие вкладок ============================

    private void Tabs_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Middle) return;
        for (int i = 0; i < _tabs.TabPages.Count; i++)
        {
            if (_tabs.GetTabRect(i).Contains(e.Location))
            {
                CloseTab(i);
                break;
            }
        }
    }

    private void CloseTab(int index)
    {
        if (index < 0 || index >= _tabs.TabPages.Count) return;
        if (_tabs.TabPages.Count == 1) return;   // последнюю не закрываем

        var page = _tabs.TabPages[index];
        var wv = page.Controls.OfType<WebView2>().FirstOrDefault();
        wv?.Dispose();
        _tabs.TabPages.Remove(page);
        page.Dispose();
    }

    // ============================ Горячие клавиши ============================

    private async void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (await TryHandleShortcut(e.Control, e.Alt, e.KeyCode))
            e.Handled = true;
    }

    private void HandleWebViewAccelerator(CoreWebView2AcceleratorKeyPressedEventArgs e)
    {
        if (e.KeyEventKind != CoreWebView2KeyEventKind.KeyDown &&
            e.KeyEventKind != CoreWebView2KeyEventKind.SystemKeyDown)
            return;

        bool ctrl = (Control.ModifierKeys & Keys.Control) != 0;
        bool alt  = (Control.ModifierKeys & Keys.Alt) != 0;

        _ = TryHandleShortcut(ctrl, alt, (Keys)e.VirtualKey, markHandled: h => e.Handled = h);
    }

    private async Task<bool> TryHandleShortcut(bool ctrl, bool alt, Keys key, Action<bool>? markHandled = null)
    {
        switch (key)
        {
            case Keys.T when ctrl:
                markHandled?.Invoke(true);
                await AddNewTabAsync();
                return true;

            case Keys.W when ctrl:
                markHandled?.Invoke(true);
                CloseTab(_tabs.SelectedIndex);
                return true;

            case Keys.L when ctrl:
                markHandled?.Invoke(true);
                _addressBar.Focus();
                _addressBar.SelectAll();
                return true;

            case Keys.F5:
                markHandled?.Invoke(true);
                GetCurrentCore()?.Reload();
                return true;

            case Keys.Left when alt:
            {
                var c = GetCurrentCore();
                if (c?.CanGoBack == true) { c.GoBack(); markHandled?.Invoke(true); return true; }
                return false;
            }

            case Keys.Right when alt:
            {
                var c = GetCurrentCore();
                if (c?.CanGoForward == true) { c.GoForward(); markHandled?.Invoke(true); return true; }
                return false;
            }
        }
        return false;
    }

    // ============================ Настройки ============================

    private void OpenSettings()
    {
        using var dlg = new SettingsForm(_settings);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _settings = dlg.Settings;
        _settings.Save();

        // Применить к уже открытым вкладкам
        foreach (TabPage page in _tabs.TabPages)
        {
            var wv = page.Controls.OfType<WebView2>().FirstOrDefault();
            if (wv?.CoreWebView2 is { } c)
            {
                c.Settings.IsStatusBarEnabled = _settings.IsStatusBarEnabled;
                c.Settings.AreDevToolsEnabled = _settings.AreDevToolsEnabled;
            }
        }
    }
}