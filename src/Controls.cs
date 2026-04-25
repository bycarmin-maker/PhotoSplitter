using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PhotoSplitter
{
    /// <summary>커스텀 프레임리스 타이틀바 (드래그 이동 + 최소화/최대화/닫기)</summary>
    class TitleBar : Panel
    {
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private readonly Form   _owner;
        private readonly Label  _lblTitle;
        private readonly TitleBarBtn _btnClose, _btnMax, _btnMin;
        private Point _iconPadLeft = new Point(16, 13);

        public TitleBar(Form owner)
        {
            _owner      = owner;
            Height      = Theme.TitleBarHeight;
            Dock        = DockStyle.Top;
            BackColor   = Theme.TitleBarBg;
            DoubleBuffered = true;

            _lblTitle = new Label
            {
                Text      = "사진 파일 분류 프로그램",
                Font      = Theme.FontAppTitle,
                ForeColor = Theme.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Height    = Theme.TitleBarHeight,
                Location  = new Point(48, 0),
                Width     = 400,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _lblTitle.MouseDown += OnDragMouseDown;
            MouseDown += OnDragMouseDown;

            _btnClose = new TitleBarBtn("✕", false); _btnClose.Click += (s, e) => owner.Close();
            _btnMax   = new TitleBarBtn("▢", true);  _btnMax.Click   += (s, e) => ToggleMaximize();
            _btnMin   = new TitleBarBtn("—", true);  _btnMin.Click   += (s, e) => owner.WindowState = FormWindowState.Minimized;

            Controls.Add(_lblTitle);
            Controls.Add(_btnClose);
            Controls.Add(_btnMax);
            Controls.Add(_btnMin);
        }

        void ToggleMaximize()
        {
            _owner.WindowState = _owner.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        void OnDragMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _owner.WindowState == FormWindowState.Normal)
            {
                ReleaseCapture();
                SendMessage(_owner.Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
            }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int bw = 46, bh = Theme.TitleBarHeight;
            _btnClose.SetBounds(Width - bw,      0, bw, bh);
            _btnMax.SetBounds  (Width - bw * 2,  0, bw, bh);
            _btnMin.SetBounds  (Width - bw * 3,  0, bw, bh);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            // 하단 구분선
            using (var pen = new Pen(Theme.TitleBarLine, 1))
                g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            // 앱 아이콘 원형 그라디언트
            var iconRect = new Rectangle(12, 11, 22, 22);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme.FillAccentGradient(g, iconRect);
            using (var path = Theme.CreateRoundedPath(iconRect, 6))
            {
                Theme.FillAccentGradient(g, iconRect);
                g.SetClip(path); Theme.FillAccentGradient(g, iconRect); g.ResetClip();
            }
            using (var f = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var b = new SolidBrush(Color.White))
                g.DrawString("📁", new Font("Segoe UI", 9f), b, 13, 12);
        }
    }

    /// <summary>타이틀바 버튼 (닫기/최대화/최소화)</summary>
    class TitleBarBtn : Button
    {
        private readonly bool _isSubtle;
        private bool _hovering;

        public TitleBarBtn(string symbol, bool isSubtle)
        {
            _isSubtle  = isSubtle;
            Text       = symbol;
            Font       = new Font("Segoe UI", 10f, FontStyle.Regular);
            ForeColor  = Theme.TextSecondary;
            FlatStyle  = FlatStyle.Flat;
            BackColor  = Color.Transparent;
            Cursor     = Cursors.Hand;
            FlatAppearance.BorderSize     = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            DoubleBuffered = true;
        }

        protected override void OnMouseEnter(EventArgs e) { _hovering = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovering = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (_hovering)
            {
                Color hoverBg = _isSubtle
                    ? Color.FromArgb(20, 108, 92, 231)
                    : Theme.CloseHover;
                using (var b = new SolidBrush(hoverBg))
                    g.FillRectangle(b, ClientRectangle);
            }
            using (var b = new SolidBrush(_hovering && !_isSubtle ? Color.White : Theme.TextSecondary))
                TextRenderer.DrawText(g, Text, Font, ClientRectangle, b.Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    /// <summary>둥근 모서리 카드 컨테이너 (그림자 포함)</summary>
    class CardPanel : Panel
    {
        public string CardTitle { get; set; }
        private const int _inset = 8; // 그림자 여백

        public CardPanel(string title)
        {
            CardTitle     = title;
            BackColor     = Color.Transparent;
            DoubleBuffered = true;
            Padding       = new Padding(Theme.CardPad, Theme.CardPad + 26, Theme.CardPad, Theme.CardPad);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(_inset, _inset, Width - _inset * 2, Height - _inset * 2);
            Theme.PaintCardSurface(g, rect, Theme.CardRadius);

            // 카드 제목
            using (var titleBrush = new LinearGradientBrush(
                new Rectangle(rect.X + 16, rect.Y + 12, 300, 18),
                Theme.AccentPurple, Theme.AccentPink, LinearGradientMode.Horizontal))
            {
                var blend = new ColorBlend(3);
                blend.Colors    = new[] { Theme.AccentPurple, Theme.AccentBlue, Theme.AccentPink };
                blend.Positions = new[] { 0f, 0.5f, 1f };
                titleBrush.InterpolationColors = blend;
                g.DrawString(CardTitle, Theme.FontCardTitle, titleBrush, rect.X + 16, rect.Y + 13);
            }
        }
    }

    /// <summary>폴더 선택 위젯 (버튼 + 드래그&드롭 + 경로 표시)</summary>
    class FolderPickerCard : Panel
    {
        public string SelectedPath { get; private set; }
        public event EventHandler PathChanged;

        private readonly Label  _lblCaption;
        private readonly Label  _lblPath;
        private readonly Button _btnBrowse;

        public FolderPickerCard(string caption)
        {
            SelectedPath   = "";
            Height         = 80;
            BackColor      = Color.Transparent;
            AllowDrop      = true;
            DoubleBuffered = true;

            _lblCaption = new Label
            {
                Text      = caption,
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                Location  = new Point(16, 10),
                AutoSize  = true
            };

            _lblPath = new Label
            {
                Text      = "폴더를 선택하거나 여기에 드래그하세요",
                Font      = Theme.FontBody,
                ForeColor = Theme.TextHint,
                BackColor = Color.Transparent,
                Location  = new Point(16, 30),
                AutoSize  = false,
                Height    = 24
            };

            _btnBrowse = new Button
            {
                Text      = "선택",
                Font      = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Theme.AccentPurple,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                Size      = new Size(60, 28)
            };
            _btnBrowse.FlatAppearance.BorderSize = 0;
            _btnBrowse.Region = new System.Drawing.Region(Theme.CreateRoundedPath(new Rectangle(0, 0, 60, 28), 8));
            _btnBrowse.Click += (s, e) => OpenFolderDialog();

            DragEnter += (s, e) => { e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; };
            DragDrop  += (s, e) =>
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths != null && paths.Length > 0 && System.IO.Directory.Exists(paths[0]))
                    SetPath(paths[0]);
            };

            Controls.Add(_lblCaption);
            Controls.Add(_lblPath);
            Controls.Add(_btnBrowse);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            _lblPath.Width    = Width - 96;
            _btnBrowse.Location = new Point(Width - 76, 26);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Theme.CreateRoundedPath(rect, 10))
            {
                using (var fill = new SolidBrush(Color.FromArgb(252, 252, 255)))
                    g.FillPath(fill, path);
                using (var pen = new Pen(Theme.Border, 1.2f))
                    g.DrawPath(pen, path);
            }
        }

        void OpenFolderDialog()
        {
            using (var d = new FolderBrowserDialog { ShowNewFolderButton = true })
                if (d.ShowDialog() == DialogResult.OK)
                    SetPath(d.SelectedPath);
        }

        public void SetPath(string path)
        {
            SelectedPath  = path;
            _lblPath.Text = path;
            _lblPath.ForeColor = Theme.TextPrimary;
            if (PathChanged != null) PathChanged(this, EventArgs.Empty);
        }
    }

    /// <summary>보라→파란→핑크 그라디언트 버튼</summary>
    class AccentButton : Button
    {
        private bool _hovering;

        public AccentButton()
        {
            FlatStyle  = FlatStyle.Flat;
            ForeColor  = Color.White;
            Font       = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            Cursor     = Cursors.Hand;
            FlatAppearance.BorderSize = 0;
            DoubleBuffered = true;
        }

        protected override void OnMouseEnter(EventArgs e) { _hovering = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovering = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width, Height);
            using (var path = Theme.CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Theme.BtnRadius))
            {
                g.SetClip(path);
                Theme.FillAccentGradient(g, rect);
                if (_hovering)
                    using (var overlay = new SolidBrush(Color.FromArgb(25, 255, 255, 255)))
                        g.FillRectangle(overlay, rect);
                g.ResetClip();
            }
            TextRenderer.DrawText(g, Text, Font, rect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    /// <summary>그라디언트 텍스트 레이블</summary>
    class GradientLabel : Label
    {
        public GradientLabel()
        {
            BackColor = Color.Transparent;
            Font      = Theme.FontCardTitle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width, Height);
            if (rect.Width < 2 || rect.Height < 2) return;
            using (var brush = new LinearGradientBrush(rect, Theme.AccentPurple, Theme.AccentPink, LinearGradientMode.Horizontal))
            {
                var blend = new ColorBlend(3);
                blend.Colors    = new[] { Theme.AccentPurple, Theme.AccentBlue, Theme.AccentPink };
                blend.Positions = new[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = blend;
                g.DrawString(Text, Font, brush, 0, 0);
            }
        }
    }
}
