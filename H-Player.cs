// ============================================================================
// H-Player —— 开源、无广告、免费的轻量视频播放器
// 解码内核: libvlc (VLC)。支持 mkv/mp4/avi/mov/wmv/flv/webm/ts/rmvb 等主流格式
// 编译: csc /target:winexe /platform:x64 H-Player.cs
// ============================================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HPlayer
{
    // ---------------------------------------------------------------- 主题
    static class T
    {
        public static readonly Color Bg      = Color.FromArgb(18, 18, 18);
        public static readonly Color Panel   = Color.FromArgb(26, 26, 26);
        public static readonly Color PanelAlt= Color.FromArgb(31, 31, 31);
        public static readonly Color Border  = Color.FromArgb(52, 52, 52);
        public static readonly Color Text    = Color.FromArgb(232, 232, 232);
        public static readonly Color Dim     = Color.FromArgb(140, 140, 140);
        public static readonly Color Accent  = Color.FromArgb(255, 122, 26);
        public static readonly Color Hover   = Color.FromArgb(48, 48, 48);
        public static readonly Color Track   = Color.FromArgb(58, 58, 58);
        public static readonly Color CloseRed= Color.FromArgb(232, 17, 35);

        public static readonly Font Mdl2   = new Font("Segoe MDL2 Assets", 11f);
        public static readonly Font Mdl2Sm = new Font("Segoe MDL2 Assets", 10f);
        public static readonly Font Symbol = new Font("Segoe UI Symbol", 11f);
        public static readonly Font Ui     = new Font("Microsoft YaHei UI", 9f);
        public static readonly Font UiBold = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
        public static readonly Font UiSm   = new Font("Microsoft YaHei UI", 8.25f);
        public static readonly Font UiBig  = new Font("Microsoft YaHei UI", 20f, FontStyle.Bold);

        // Segoe MDL2 Assets 图标
        public const string IcoPlay      = "\uE768";
        public const string IcoPause     = "\uE769";
        public const string IcoVol       = "\uE767";
        public const string IcoMute      = "\uE74F";
        public const string IcoFull      = "\uE740";
        public const string IcoFullExit  = "\uE73F";
        public const string IcoList      = "\uE700";
        public const string IcoRepeatAll = "\uE8EE";
        public const string IcoRepeatOne = "\uE8ED";
        public const string IcoClose     = "\uE8BB";
        public const string IcoMin       = "\uE921";
        public const string IcoMax       = "\uE922";
        public const string IcoRestore   = "\uE923";
        // 通用符号字体
        public const string IcoPrev = "\u23EE";
        public const string IcoNext = "\u23ED";
        public const string IcoStop = "\u25A0";
    }

    // ---------------------------------------------------------------- Win32
    static class Native
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool SetDllDirectory(string lpPathName);
        [DllImport("user32")] public static extern bool SetProcessDPIAware();
        [DllImport("user32")] public static extern bool ReleaseCapture();
        [DllImport("user32")] public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 2;
    }

    // ---------------------------------------------------------------- libvlc 封装
    static class Vlc
    {
        const string LIB = "libvlc";
        static IntPtr _inst = IntPtr.Zero;
        static readonly List<IntPtr> _keep = new List<IntPtr>();
        public static bool Available { get; private set; }

        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr libvlc_new(int argc, IntPtr[] argv);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern void libvlc_release(IntPtr inst);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr libvlc_media_new_path(IntPtr inst, IntPtr path);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern void libvlc_media_release(IntPtr media);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr libvlc_media_player_new_from_media(IntPtr media);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern void libvlc_media_player_release(IntPtr mp);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern int  libvlc_media_player_play(IntPtr mp);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern void libvlc_media_player_set_pause(IntPtr mp, int doPause);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern void libvlc_media_player_stop(IntPtr mp);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern void libvlc_media_player_set_hwnd(IntPtr mp, IntPtr drawable);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern long libvlc_media_player_get_time(IntPtr mp);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern long libvlc_media_player_get_length(IntPtr mp);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern void libvlc_media_player_set_time(IntPtr mp, long time);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern float libvlc_media_player_get_position(IntPtr mp);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern void libvlc_media_player_set_position(IntPtr mp, float pos);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern int  libvlc_media_player_get_state(IntPtr mp);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern int  libvlc_audio_get_volume(IntPtr mp);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern int  libvlc_audio_set_volume(IntPtr mp, int vol);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern void libvlc_audio_set_mute(IntPtr mp, int status);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] static extern int  libvlc_media_player_set_rate(IntPtr mp, float rate);

        public enum State { NothingSpecial = 0, Opening = 1, Buffering = 2, Playing = 3, Paused = 4, Stopped = 5, Ended = 6, Error = 7 }

        static IntPtr Utf8(string s, bool keep)
        {
            byte[] b = Encoding.UTF8.GetBytes(s + "\0");
            IntPtr p = Marshal.AllocHGlobal(b.Length);
            Marshal.Copy(b, 0, p, b.Length);
            if (keep) _keep.Add(p);
            return p;
        }

        static string FindVlcDir()
        {
            string[] candidates = {
                @"C:\Program Files\VideoLAN\VLC",
                @"C:\Program Files (x86)\VideoLAN\VLC"
            };
            foreach (var c in candidates)
                if (File.Exists(Path.Combine(c, "libvlc.dll"))) return c;
            try
            {
                foreach (var name in new[] { @"SOFTWARE\VideoLAN\VLC", @"SOFTWARE\WOW6432Node\VideoLAN\VLC" })
                    using (var k = Registry.LocalMachine.OpenSubKey(name))
                    {
                        var dir = k != null ? k.GetValue("InstallDir") as string : null;
                        if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "libvlc.dll")))
                            return dir;
                    }
            }
            catch { }
            return null;
        }

        public static bool Init(out string error)
        {
            string dir = FindVlcDir();
            if (dir == null)
            {
                error = "未找到 VLC 运行库（libvlc.dll）。\n请安装 64 位 VLC: https://www.videolan.org/";
                return false;
            }
            Native.SetDllDirectory(dir);
            string[] args = { "--no-video-title-show", "--no-osd", "--quiet" };
            var argv = new IntPtr[args.Length];
            for (int i = 0; i < args.Length; i++) argv[i] = Utf8(args[i], true);
            _inst = libvlc_new(args.Length, argv);
            if (_inst == IntPtr.Zero) { error = "libvlc 初始化失败。"; return false; }
            Available = true;
            error = null;
            return true;
        }

        public static IntPtr NewPlayerFromPath(string path)
        {
            IntPtr p = Utf8(path, false);
            IntPtr media = libvlc_media_new_path(_inst, p);
            Marshal.FreeHGlobal(p);
            if (media == IntPtr.Zero) return IntPtr.Zero;
            IntPtr mp = libvlc_media_player_new_from_media(media);
            libvlc_media_release(media);
            return mp;
        }

        public static void Shutdown()
        {
            if (_inst != IntPtr.Zero) { libvlc_release(_inst); _inst = IntPtr.Zero; }
            foreach (var p in _keep) Marshal.FreeHGlobal(p);
            _keep.Clear();
        }

        public static int    Play(IntPtr mp)          { return libvlc_media_player_play(mp); }
        public static void   Pause(IntPtr mp, bool p) { libvlc_media_player_set_pause(mp, p ? 1 : 0); }
        public static void   Stop(IntPtr mp)          { libvlc_media_player_stop(mp); }
        public static void   Release(IntPtr mp)       { libvlc_media_player_release(mp); }
        public static void   SetHwnd(IntPtr mp, IntPtr h) { libvlc_media_player_set_hwnd(mp, h); }
        public static long   GetTime(IntPtr mp)       { return libvlc_media_player_get_time(mp); }
        public static long   GetLength(IntPtr mp)     { return libvlc_media_player_get_length(mp); }
        public static void   SetTime(IntPtr mp, long t)   { libvlc_media_player_set_time(mp, t); }
        public static float  GetPosition(IntPtr mp)   { return libvlc_media_player_get_position(mp); }
        public static void   SetPosition(IntPtr mp, float p) { libvlc_media_player_set_position(mp, p); }
        public static State  GetState(IntPtr mp)      { return (State)libvlc_media_player_get_state(mp); }
        public static void   SetVolume(IntPtr mp, int v)  { libvlc_audio_set_volume(mp, v); }
        public static void   SetMute(IntPtr mp, bool m)   { libvlc_audio_set_mute(mp, m ? 1 : 0); }
        public static void   SetRate(IntPtr mp, float r)  { libvlc_media_player_set_rate(mp, r); }
    }

    // ---------------------------------------------------------------- 程序入口
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) =>
                MessageBox.Show(e.Exception.Message, "H-Player 出错", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            string err;
            if (!Vlc.Init(out err))
            {
                MessageBox.Show(err, "H-Player", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var files = (args ?? new string[0]).Where(File.Exists).ToArray();
            Application.Run(new MainForm(files));
            Vlc.Shutdown();
        }
    }

    // ---------------------------------------------------------------- 自绘图标按钮
    class IconButton : Control
    {
        public string Icon = "";
        public Color IconColor = T.Text;
        public Color? HoverBack = T.Hover;
        bool _hover;

        public IconButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            Size = new Size(36, 34);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (_hover && HoverBack.HasValue)
                using (var b = new SolidBrush(HoverBack.Value))
                    g.FillRectangle(b, ClientRectangle);
            TextRenderer.DrawText(g, Icon, Font, ClientRectangle, IconColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    // ---------------------------------------------------------------- 进度条
    class SeekBar : Control
    {
        public float Value { get; private set; }
        public bool Dragging { get; private set; }
        public event Action<float> SeekRequested;

        public SeekBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 18;
            Cursor = Cursors.Hand;
        }

        public void SetValue(float v)
        {
            if (Dragging) return;
            Value = Math.Max(0f, Math.Min(1f, v));
            Invalidate();
        }

        void UpdateFromMouse(int x)
        {
            Value = Math.Max(0f, Math.Min(1f, x / (float)Width));
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { Dragging = true; Capture = true; UpdateFromMouse(e.X); }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (Dragging) UpdateFromMouse(e.X);
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (Dragging)
            {
                Dragging = false; Capture = false; UpdateFromMouse(e.X);
                if (SeekRequested != null) SeekRequested(Value);
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cy = Height / 2;
            var track = new Rectangle(0, cy - 2, Width, 4);
            using (var b = new SolidBrush(T.Track)) g.FillRectangle(b, track);
            int fw = (int)(Width * Value);
            if (fw > 0)
                using (var b = new SolidBrush(T.Accent)) g.FillRectangle(b, new Rectangle(0, cy - 2, fw, 4));
            int kx = Math.Max(6, Math.Min(Width - 6, fw));
            using (var b = new SolidBrush(Color.White))
                g.FillEllipse(b, kx - 5, cy - 5, 10, 10);
        }
    }

    // ---------------------------------------------------------------- 音量条
    class VolumeBar : Control
    {
        float _value = 0.8f;
        public event Action<float> ValueChanged;
        bool _drag;

        public VolumeBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(80, 34);
            Cursor = Cursors.Hand;
        }

        public float Value
        {
            get { return _value; }
            set { _value = Math.Max(0f, Math.Min(1f, value)); Invalidate(); }
        }

        void SetFromMouse(int x)
        {
            Value = x / (float)Width;
            if (ValueChanged != null) ValueChanged(_value);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _drag = true; Capture = true; SetFromMouse(e.X); }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_drag) SetFromMouse(e.X);
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e) { _drag = false; Capture = false; base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cy = Height / 2;
            using (var b = new SolidBrush(T.Track)) g.FillRectangle(b, 0, cy - 2, Width, 4);
            int fw = (int)(Width * _value);
            if (fw > 0)
                using (var b = new SolidBrush(T.Accent)) g.FillRectangle(b, 0, cy - 2, fw, 4);
            int kx = Math.Max(4, Math.Min(Width - 4, fw));
            using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, kx - 4, cy - 4, 8, 8);
        }
    }

    // ---------------------------------------------------------------- 视频渲染区
    class VideoPanel : Panel
    {
        public bool Active; // 是否有媒体正在播放/暂停

        public VideoPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Black;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Active) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = Width / 2, cy = Height / 2;

            // 橙色圆角方块 + 白色三角 Logo
            int s = 64;
            var rect = new Rectangle(cx - s / 2, cy - s / 2 - 40, s, s);
            using (var path = RoundedRect(rect, 14))
            using (var b = new SolidBrush(T.Accent)) g.FillPath(b, path);
            var tri = new[] {
                new Point(cx - 9, cy - 16 - 40),
                new Point(cx - 9, cy + 14 - 40),
                new Point(cx + 16, cy - 1 - 40)
            };
            using (var b = new SolidBrush(Color.White)) g.FillPolygon(b, tri);

            using (var b = new SolidBrush(T.Text))
                TextRenderer.DrawText(g, "H-Player", T.UiBig,
                    new Rectangle(0, cy + 34, Width, 40), b.Color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            using (var b = new SolidBrush(T.Dim))
                TextRenderer.DrawText(g, "将视频文件拖到这里，或按 Ctrl+O 打开", T.Ui,
                    new Rectangle(0, cy + 80, Width, 24), b.Color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }

        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // ---------------------------------------------------------------- 深色右键菜单
    class DarkColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected { get { return T.Hover; } }
        public override Color MenuItemSelectedGradientBegin { get { return T.Hover; } }
        public override Color MenuItemSelectedGradientEnd { get { return T.Hover; } }
        public override Color MenuItemBorder { get { return T.Hover; } }
        public override Color MenuBorder { get { return T.Border; } }
        public override Color ToolStripDropDownBackground { get { return T.PanelAlt; } }
        public override Color ImageMarginGradientBegin { get { return T.PanelAlt; } }
        public override Color ImageMarginGradientMiddle { get { return T.PanelAlt; } }
        public override Color ImageMarginGradientEnd { get { return T.PanelAlt; } }
        public override Color SeparatorDark { get { return T.Border; } }
    }

    // ---------------------------------------------------------------- 主窗口
    class MainForm : Form
    {
        enum LoopMode { Off, All, One }

        static readonly HashSet<string> MediaExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".m2ts",
            ".mpg", ".mpeg", ".3gp", ".vob", ".rmvb", ".rm", ".asf", ".divx", ".ogv",
            ".mp3", ".flac", ".wav", ".aac", ".ogg", ".m4a", ".wma", ".ape", ".opus"
        };

        // UI
        Panel titleBar, controls, playlistPanel, footerPanel;
        Label titleLabel, countLabel, timeLabel;
        VideoPanel video;
        SeekBar seek;
        VolumeBar volBar;
        ListBox list;
        IconButton btnPrev, btnPlay, btnStop, btnNext, btnRate, btnLoop, btnVol, btnList, btnFs;
        IconButton btnMin, btnMax, btnClose;
        ToolTip tip = new ToolTip();
        Timer uiTimer, hideTimer, clickTimer;

        // 播放状态
        IntPtr _mp = IntPtr.Zero;
        int _current = -1;
        bool _muted;
        int _volume = 80;
        float _rate = 1f;
        static readonly float[] Rates = { 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f };
        LoopMode _loop = LoopMode.Off;
        bool _endedHandled;
        bool _fullscreen;
        bool _cursorHidden;
        FormWindowState _stateBeforeFs = FormWindowState.Normal;

        public MainForm(string[] initialFiles)
        {
            DoubleBuffered = true;
            BackColor = T.Bg;
            ForeColor = T.Text;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(980, 620);
            MinimumSize = new Size(560, 380);
            Icon = MakeIcon();
            Text = "H-Player";
            KeyPreview = true;
            Font = T.Ui;

            BuildUi();
            WireEvents();

            uiTimer = new Timer { Interval = 150 };
            uiTimer.Tick += (s, e) => UpdateUi();
            uiTimer.Start();

            hideTimer = new Timer { Interval = 2500 };
            hideTimer.Tick += (s, e) =>
            {
                hideTimer.Stop();
                if (_fullscreen) { controls.Visible = false; Cursor.Hide(); _cursorHidden = true; }
            };

            clickTimer = new Timer { Interval = SystemInformation.DoubleClickTime };
            clickTimer.Tick += (s, e) => { clickTimer.Stop(); TogglePlay(); };

            if (initialFiles.Length > 0) AddFiles(initialFiles, true);
        }

        // ------------------------------------------------------------ UI 构建
        void BuildUi()
        {
            // 视频区(先添加,dock 最后布局)
            video = new VideoPanel { Dock = DockStyle.Fill };
            Controls.Add(video);

            // 播放列表
            playlistPanel = new Panel { Dock = DockStyle.Right, Width = 252, BackColor = T.PanelAlt, Visible = false };
            countLabel = new Label
            {
                Dock = DockStyle.Top, Height = 36, Text = "  播放列表 (0)",
                ForeColor = T.Text, Font = T.UiBold, TextAlign = ContentAlignment.MiddleLeft, BackColor = T.PanelAlt
            };
            list = new ListBox
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = T.PanelAlt,
                ForeColor = T.Text, Font = T.Ui, ItemHeight = 28, IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed, HorizontalScrollbar = true
            };
            list.DrawItem += List_DrawItem;
            footerPanel = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = T.PanelAlt };
            var btnAdd = new IconButton { Icon = "＋ 添加文件", Font = T.Ui, Dock = DockStyle.Fill };
            var btnClear = new IconButton { Icon = "清空", Font = T.Ui, Dock = DockStyle.Right, Width = 64 };
            btnAdd.Click += (s, e) => OpenFilesDialog();
            btnClear.Click += (s, e) => ClearPlaylist();
            footerPanel.Controls.Add(btnAdd);
            footerPanel.Controls.Add(btnClear);
            playlistPanel.Controls.Add(list);
            playlistPanel.Controls.Add(countLabel);
            playlistPanel.Controls.Add(footerPanel);
            playlistPanel.Paint += (s, e) =>
            {
                using (var p = new Pen(T.Border)) e.Graphics.DrawLine(p, 0, 0, 0, playlistPanel.Height);
            };
            Controls.Add(playlistPanel);

            // 底部控制区
            controls = new Panel { Dock = DockStyle.Bottom, Height = 72, BackColor = T.Panel };
            seek = new SeekBar { Dock = DockStyle.Top };
            var row = new Panel { Dock = DockStyle.Fill, BackColor = T.Panel };
            row.Padding = new Padding(8, 2, 8, 2);

            timeLabel = new Label
            {
                Dock = DockStyle.Left, Width = 132, Text = "00:00 / 00:00",
                ForeColor = T.Dim, Font = T.UiSm, TextAlign = ContentAlignment.MiddleCenter, BackColor = T.Panel
            };

            var leftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, AutoSize = true, WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight, BackColor = T.Panel, Padding = new Padding(0),
                Margin = new Padding(0)
            };
            btnPrev = MakeBtn(T.IcoPrev, T.Symbol, "上一个 (PgUp)");
            btnPlay = MakeBtn(T.IcoPlay, T.Mdl2, "播放/暂停 (空格)");
            btnStop = MakeBtn(T.IcoStop, T.Symbol, "停止");
            btnNext = MakeBtn(T.IcoNext, T.Symbol, "下一个 (PgDn)");
            leftFlow.Controls.AddRange(new Control[] { btnPrev, btnPlay, btnStop, btnNext });

            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true, WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight, BackColor = T.Panel, Padding = new Padding(0),
                Margin = new Padding(0)
            };
            btnRate = new IconButton { Icon = "1.0×", Font = T.UiSm, Width = 44 };
            tip.SetToolTip(btnRate, "倍速播放");
            btnLoop = MakeBtn(T.IcoRepeatAll, T.Mdl2, "循环模式：顺序");
            btnLoop.IconColor = T.Dim;
            btnVol = MakeBtn(T.IcoVol, T.Mdl2, "静音 (M)");
            volBar = new VolumeBar { Value = 0.8f, Margin = new Padding(0, 0, 4, 0) };
            btnList = MakeBtn(T.IcoList, T.Mdl2, "播放列表 (L)");
            btnFs = MakeBtn(T.IcoFull, T.Mdl2, "全屏 (F)");
            rightFlow.Controls.AddRange(new Control[] { btnRate, btnLoop, btnVol, volBar, btnList, btnFs });

            row.Controls.Add(timeLabel);
            row.Controls.Add(leftFlow);
            row.Controls.Add(rightFlow);
            controls.Controls.Add(row);
            controls.Controls.Add(seek);
            controls.Paint += (s, e) =>
            {
                using (var p = new Pen(T.Border)) e.Graphics.DrawLine(p, 0, 0, controls.Width, 0);
            };
            Controls.Add(controls);

            // 标题栏
            titleBar = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = T.Panel };
            titleLabel = new Label
            {
                Dock = DockStyle.Fill, Text = "H-Player", ForeColor = T.Text, Font = T.Ui,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(34, 0, 0, 0), BackColor = T.Panel
            };
            btnMin = MakeWinBtn(T.IcoMin, "最小化");
            btnMax = MakeWinBtn(T.IcoMax, "最大化");
            btnClose = MakeWinBtn(T.IcoClose, "关闭");
            btnClose.HoverBack = T.CloseRed;
            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(btnMin);
            titleBar.Controls.Add(btnMax);
            titleBar.Controls.Add(btnClose);
            titleBar.Paint += TitleBar_Paint;
            Controls.Add(titleBar);
        }

        IconButton MakeBtn(string icon, Font font, string tooltip)
        {
            var b = new IconButton { Icon = icon, Font = font };
            tip.SetToolTip(b, tooltip);
            return b;
        }

        IconButton MakeWinBtn(string icon, string tooltip)
        {
            var b = new IconButton { Icon = icon, Font = T.Mdl2Sm, Dock = DockStyle.Right, Width = 46 };
            tip.SetToolTip(b, tooltip);
            return b;
        }

        // ------------------------------------------------------------ 事件绑定
        void WireEvents()
        {
            // 标题栏
            titleBar.MouseDown += DragMove;
            titleLabel.MouseDown += DragMove;
            titleBar.MouseDoubleClick += (s, e) => { if (e.Button == MouseButtons.Left) ToggleMaximize(); };
            titleLabel.MouseDoubleClick += (s, e) => { if (e.Button == MouseButtons.Left) ToggleMaximize(); };
            btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
            btnMax.Click += (s, e) => ToggleMaximize();
            btnClose.Click += (s, e) => Close();

            // 播放控制
            btnPlay.Click += (s, e) => TogglePlay();
            btnStop.Click += (s, e) => StopPlayback(true);
            btnPrev.Click += (s, e) => PlayRelative(-1);
            btnNext.Click += (s, e) => PlayRelative(1);
            btnRate.Click += (s, e) => CycleRate();
            btnLoop.Click += (s, e) => CycleLoop();
            btnVol.Click += (s, e) => ToggleMute();
            btnList.Click += (s, e) => playlistPanel.Visible = !playlistPanel.Visible;
            btnFs.Click += (s, e) => ToggleFullscreen();

            seek.SeekRequested += v =>
            {
                if (_mp != IntPtr.Zero && Vlc.GetLength(_mp) > 0) Vlc.SetPosition(_mp, v);
            };
            volBar.ValueChanged += v =>
            {
                _volume = (int)(v * 100);
                if (_muted && _volume > 0) { _muted = false; ApplyMute(); }
                if (_mp != IntPtr.Zero) Vlc.SetVolume(_mp, _volume);
                btnVol.Icon = _muted || _volume == 0 ? T.IcoMute : T.IcoVol;
                btnVol.Invalidate();
            };

            // 视频区交互: 单击播放/暂停(延迟以避让双击), 双击全屏
            video.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) { clickTimer.Stop(); clickTimer.Start(); } };
            video.MouseDoubleClick += (s, e) => { if (e.Button == MouseButtons.Left) { clickTimer.Stop(); ToggleFullscreen(); } };
            video.MouseMove += (s, e) => OnFullscreenMouseMove();

            // 播放列表
            list.MouseDoubleClick += (s, e) =>
            {
                int idx = list.IndexFromPoint(e.Location);
                if (idx >= 0) PlayIndex(idx);
            };
            list.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && list.SelectedIndex >= 0) RemoveAt(list.SelectedIndex);
            };
            var menu = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new DarkColors()), ForeColor = T.Text };
            menu.Items.Add("移除", null, (s, e) => { if (list.SelectedIndex >= 0) RemoveAt(list.SelectedIndex); });
            menu.Items.Add("清空列表", null, (s, e) => ClearPlaylist());
            list.ContextMenuStrip = menu;

            // 拖放
            EnableDrop(this);
            EnableDrop(video);
            EnableDrop(list);
            EnableDrop(controls);
            EnableDrop(titleBar);
        }

        void EnableDrop(Control c)
        {
            c.AllowDrop = true;
            c.DragEnter += (s, e) =>
            {
                e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            };
            c.DragDrop += (s, e) =>
            {
                var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (paths != null && paths.Length > 0) AddFiles(paths, true);
            };
        }

        void DragMove(object s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && WindowState == FormWindowState.Normal && !_fullscreen)
            {
                Native.ReleaseCapture();
                Native.SendMessage(Handle, Native.WM_NCLBUTTONDOWN, Native.HTCAPTION, 0);
            }
        }

        // ------------------------------------------------------------ 播放控制
        void PlayIndex(int i)
        {
            if (i < 0 || i >= list.Items.Count) return;
            StopCurrent();
            _current = i;
            string path = list.Items[i] as string;
            _mp = Vlc.NewPlayerFromPath(path);
            if (_mp == IntPtr.Zero)
            {
                SetStatus("无法打开: " + Path.GetFileName(path));
                return;
            }
            Vlc.SetHwnd(_mp, video.Handle);
            Vlc.SetVolume(_mp, _volume);
            Vlc.SetMute(_mp, _muted);
            Vlc.Play(_mp);
            Vlc.SetRate(_mp, _rate);
            _endedHandled = false;
            video.Active = true;
            video.Invalidate();
            UpdateTitle();
            UpdateCount();
            list.Invalidate();
            try { list.TopIndex = Math.Max(0, _current - 2); } catch { }
        }

        void PlayRelative(int delta)
        {
            if (list.Items.Count == 0) return;
            int next = _current + delta;
            if (next < 0) next = _loop == LoopMode.All ? list.Items.Count - 1 : 0;
            if (next >= list.Items.Count) next = _loop == LoopMode.All ? 0 : list.Items.Count - 1;
            PlayIndex(next);
        }

        void TogglePlay()
        {
            if (_mp == IntPtr.Zero)
            {
                if (list.Items.Count > 0) PlayIndex(Math.Max(0, _current));
                return;
            }
            var st = Vlc.GetState(_mp);
            if (st == Vlc.State.Playing) Vlc.Pause(_mp, true);
            else if (st == Vlc.State.Paused) Vlc.Pause(_mp, false);
            else if (st == Vlc.State.Stopped || st == Vlc.State.Ended) PlayIndex(_current);
            else Vlc.Play(_mp);
        }

        void StopCurrent()
        {
            if (_mp != IntPtr.Zero)
            {
                try { Vlc.Stop(_mp); Vlc.Release(_mp); } catch { }
                _mp = IntPtr.Zero;
            }
        }

        void StopPlayback(bool resetUi)
        {
            StopCurrent();
            if (resetUi)
            {
                video.Active = false;
                video.Invalidate();
                seek.SetValue(0);
                timeLabel.Text = "00:00 / 00:00";
                btnPlay.Icon = T.IcoPlay;
                btnPlay.Invalidate();
            }
        }

        void AutoNext()
        {
            if (_loop == LoopMode.One && _current >= 0) { PlayIndex(_current); return; }
            if (_current + 1 < list.Items.Count) { PlayIndex(_current + 1); return; }
            if (_loop == LoopMode.All && list.Items.Count > 0) { PlayIndex(0); return; }
            StopPlayback(true);
            SetStatus("播放完毕");
        }

        void CycleRate()
        {
            int idx = Array.IndexOf(Rates, _rate);
            _rate = Rates[(idx + 1) % Rates.Length];
            if (_mp != IntPtr.Zero) Vlc.SetRate(_mp, _rate);
            btnRate.Icon = _rate.ToString("0.##") + "×";
            btnRate.Invalidate();
        }

        void CycleLoop()
        {
            _loop = (LoopMode)(((int)_loop + 1) % 3);
            switch (_loop)
            {
                case LoopMode.Off:
                    btnLoop.Icon = T.IcoRepeatAll; btnLoop.IconColor = T.Dim;
                    tip.SetToolTip(btnLoop, "循环模式：顺序"); break;
                case LoopMode.All:
                    btnLoop.Icon = T.IcoRepeatAll; btnLoop.IconColor = T.Accent;
                    tip.SetToolTip(btnLoop, "循环模式：列表循环"); break;
                case LoopMode.One:
                    btnLoop.Icon = T.IcoRepeatOne; btnLoop.IconColor = T.Accent;
                    tip.SetToolTip(btnLoop, "循环模式：单集循环"); break;
            }
            btnLoop.Invalidate();
        }

        void ToggleMute()
        {
            _muted = !_muted;
            ApplyMute();
            btnVol.Icon = _muted ? T.IcoMute : T.IcoVol;
            btnVol.Invalidate();
        }

        void ApplyMute() { if (_mp != IntPtr.Zero) Vlc.SetMute(_mp, _muted); }

        // ------------------------------------------------------------ 播放列表
        void AddFiles(IEnumerable<string> paths, bool autoplay)
        {
            int firstNew = list.Items.Count;
            foreach (var p in paths)
            {
                try
                {
                    if (Directory.Exists(p))
                    {
                        foreach (var f in Directory.EnumerateFiles(p))
                            if (MediaExts.Contains(Path.GetExtension(f)) && !list.Items.Contains(f))
                                list.Items.Add(f);
                    }
                    else if (File.Exists(p) && !list.Items.Contains(p))
                    {
                        list.Items.Add(p);
                    }
                }
                catch { }
            }
            UpdateCount();
            if (autoplay && list.Items.Count > firstNew) PlayIndex(firstNew);
            else list.Invalidate();
        }

        void RemoveAt(int idx)
        {
            if (idx < 0 || idx >= list.Items.Count) return;
            list.Items.RemoveAt(idx);
            if (idx == _current)
            {
                StopPlayback(true);
                _current = -1;
                UpdateTitle();
            }
            else if (idx < _current) _current--;
            UpdateCount();
            list.Invalidate();
        }

        void ClearPlaylist()
        {
            StopPlayback(true);
            list.Items.Clear();
            _current = -1;
            UpdateTitle();
            UpdateCount();
        }

        void OpenFilesDialog()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Multiselect = true;
                dlg.Title = "打开媒体文件";
                dlg.Filter = "视频/音频|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.ts;*.m2ts;*.mpg;*.mpeg;*.3gp;*.vob;*.rmvb;*.rm;*.mp3;*.flac;*.wav;*.aac;*.ogg;*.m4a;*.wma;*.ape;*.opus|所有文件|*.*";
                if (dlg.ShowDialog(this) == DialogResult.OK) AddFiles(dlg.FileNames, true);
            }
        }

        void UpdateCount()
        {
            countLabel.Text = string.Format("  播放列表 ({0})", list.Items.Count);
        }

        // ------------------------------------------------------------ UI 刷新
        void UpdateUi()
        {
            if (_mp == IntPtr.Zero) return;
            Vlc.State st;
            long len, t;
            try
            {
                st = Vlc.GetState(_mp);
                len = Vlc.GetLength(_mp);
                t = Vlc.GetTime(_mp);
            }
            catch { return; }

            string wantIcon = st == Vlc.State.Playing ? T.IcoPause : T.IcoPlay;
            if (btnPlay.Icon != wantIcon) { btnPlay.Icon = wantIcon; btnPlay.Invalidate(); }

            if (len > 0)
            {
                if (!seek.Dragging) seek.SetValue(t / (float)len);
                timeLabel.Text = Fmt(t) + " / " + Fmt(len);
            }

            if (st == Vlc.State.Ended)
            {
                if (!_endedHandled) { _endedHandled = true; AutoNext(); }
            }
            else
            {
                _endedHandled = false;
                if (st == Vlc.State.Error) AutoNext();
            }
        }

        static string Fmt(long ms)
        {
            if (ms < 0) ms = 0;
            var ts = TimeSpan.FromMilliseconds(ms);
            return ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
        }

        void UpdateTitle()
        {
            titleLabel.Text = _current >= 0 && _current < list.Items.Count
                ? "H-Player — " + Path.GetFileName(list.Items[_current] as string)
                : "H-Player";
        }

        void SetStatus(string msg) { titleLabel.Text = "H-Player — " + msg; }

        // ------------------------------------------------------------ 全屏 / 最大化
        void ToggleMaximize()
        {
            if (_fullscreen) return;
            WindowState = WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal : FormWindowState.Maximized;
            btnMax.Icon = WindowState == FormWindowState.Maximized ? T.IcoRestore : T.IcoMax;
            btnMax.Invalidate();
            Invalidate();
        }

        void ToggleFullscreen()
        {
            _fullscreen = !_fullscreen;
            if (_fullscreen)
            {
                _stateBeforeFs = WindowState;
                titleBar.Visible = false;
                WindowState = FormWindowState.Maximized;
                btnFs.Icon = T.IcoFullExit;
                tip.SetToolTip(btnFs, "退出全屏 (F/Esc)");
                hideTimer.Stop();
                hideTimer.Start();
            }
            else
            {
                titleBar.Visible = true;
                WindowState = _stateBeforeFs;
                controls.Visible = true;
                if (_cursorHidden) { Cursor.Show(); _cursorHidden = false; }
                hideTimer.Stop();
                btnFs.Icon = T.IcoFull;
                tip.SetToolTip(btnFs, "全屏 (F)");
            }
            btnFs.Invalidate();
        }

        void OnFullscreenMouseMove()
        {
            if (!_fullscreen) return;
            if (!controls.Visible) controls.Visible = true;
            if (_cursorHidden) { Cursor.Show(); _cursorHidden = false; }
            hideTimer.Stop();
            hideTimer.Start();
        }

        // ------------------------------------------------------------ 键盘
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && e.KeyCode == Keys.O) { OpenFilesDialog(); e.Handled = true; return; }

            switch (e.KeyCode)
            {
                case Keys.Space:
                    TogglePlay(); e.Handled = true; break;
                case Keys.Left:
                    if (_mp != IntPtr.Zero) Vlc.SetTime(_mp, Math.Max(0, Vlc.GetTime(_mp) - 5000));
                    e.Handled = true; break;
                case Keys.Right:
                    if (_mp != IntPtr.Zero)
                    {
                        long len = Vlc.GetLength(_mp);
                        long nt = Vlc.GetTime(_mp) + 5000;
                        if (len > 0) nt = Math.Min(nt, len - 200);
                        Vlc.SetTime(_mp, nt);
                    }
                    e.Handled = true; break;
                case Keys.Up:
                    volBar.Value = Math.Min(1f, volBar.Value + 0.05f);
                    volBar_ValueApply();
                    e.Handled = true; break;
                case Keys.Down:
                    volBar.Value = Math.Max(0f, volBar.Value - 0.05f);
                    volBar_ValueApply();
                    e.Handled = true; break;
                case Keys.F:
                    ToggleFullscreen(); e.Handled = true; break;
                case Keys.Escape:
                    if (_fullscreen) { ToggleFullscreen(); e.Handled = true; }
                    break;
                case Keys.M:
                    ToggleMute(); e.Handled = true; break;
                case Keys.L:
                    playlistPanel.Visible = !playlistPanel.Visible; e.Handled = true; break;
                case Keys.PageUp:
                    PlayRelative(-1); e.Handled = true; break;
                case Keys.PageDown:
                    PlayRelative(1); e.Handled = true; break;
            }
        }

        void volBar_ValueApply()
        {
            _volume = (int)(volBar.Value * 100);
            if (_volume > 0 && _muted) { _muted = false; ApplyMute(); }
            if (_mp != IntPtr.Zero) Vlc.SetVolume(_mp, _volume);
            btnVol.Icon = _muted || _volume == 0 ? T.IcoMute : T.IcoVol;
            btnVol.Invalidate();
        }

        // ------------------------------------------------------------ 播放列表绘制
        void List_DrawItem(object s, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= list.Items.Count) return;
            bool cur = e.Index == _current;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            var g = e.Graphics;
            using (var b = new SolidBrush(sel ? T.Hover : T.PanelAlt))
                g.FillRectangle(b, e.Bounds);
            string name = Path.GetFileName(list.Items[e.Index] as string);
            string prefix = cur ? "▶ " : "    ";
            var r = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height);
            using (var b = new SolidBrush(cur ? T.Accent : T.Text))
            {
                var fmt = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                g.DrawString(prefix + name, T.Ui, b, r, fmt);
            }
        }

        // ------------------------------------------------------------ 标题栏 / 窗口
        void TitleBar_Paint(object s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(10, 10, 15, 15);
            using (var path = VideoPanel.RoundedRect(rect, 4))
            using (var b = new SolidBrush(T.Accent)) g.FillPath(b, path);
            var tri = new[] {
                new Point(15, 13), new Point(15, 22), new Point(22, 17)
            };
            using (var b = new SolidBrush(Color.White)) g.FillPolygon(b, tri);
            using (var p = new Pen(T.Border)) g.DrawLine(p, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);
        }

        // 无边框窗口: 边缘拖拽调整大小
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal && !_fullscreen)
            {
                base.WndProc(ref m);
                if ((int)m.Result == 1) // HTCLIENT
                {
                    int lp = m.LParam.ToInt32();
                    int x = (short)(lp & 0xFFFF);
                    int y = (short)((lp >> 16) & 0xFFFF);
                    var p = PointToClient(new Point(x, y));
                    int grip = 6;
                    bool l = p.X <= grip, rgt = p.X >= Width - grip;
                    bool tp = p.Y <= grip, bt = p.Y >= Height - grip;
                    int ht = 1;
                    if (tp && l) ht = 13; else if (tp && rgt) ht = 14;
                    else if (bt && l) ht = 16; else if (bt && rgt) ht = 17;
                    else if (l) ht = 10; else if (rgt) ht = 11;
                    else if (tp) ht = 12; else if (bt) ht = 15;
                    if (ht != 1) m.Result = (IntPtr)ht;
                }
                return;
            }
            base.WndProc(ref m);
        }

        // 1px 边框
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (WindowState != FormWindowState.Maximized)
                using (var p = new Pen(T.Border))
                    e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        static Icon MakeIcon()
        {
            // 优先使用嵌入 exe 的 app.ico
            try
            {
                var ic = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (ic != null) return ic;
            }
            catch { }
            // 回退: 运行时绘制
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = VideoPanel.RoundedRect(new Rectangle(1, 1, 30, 30), 8))
                using (var b = new SolidBrush(T.Accent)) g.FillPath(b, path);
                var tri = new[] { new Point(12, 9), new Point(12, 23), new Point(24, 16) };
                using (var b = new SolidBrush(Color.White)) g.FillPolygon(b, tri);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            uiTimer.Stop();
            StopCurrent();
            base.OnFormClosing(e);
        }
    }
}
