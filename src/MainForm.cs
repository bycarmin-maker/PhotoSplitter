using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PhotoSplitter
{
    /// <summary>메인 폼 — 프레임리스 창, 커스텀 타이틀바, 반응형 2열 레이아웃</summary>
    public class MainForm : Form
    {
        // ── Win32 리사이즈 지원 ────────────────────────────────
        const int WM_NCHITTEST   = 0x0084;
        const int HTLEFT=10, HTRIGHT=11, HTTOP=12, HTTOPLEFT=13;
        const int HTTOPRIGHT=14, HTBOTTOM=15, HTBOTTOMLEFT=16, HTBOTTOMRIGHT=17;
        const int ResizeBorder   = 6;

        // ── UI 컴포넌트 ────────────────────────────────────────
        private TitleBar          _titleBar;
        private FolderPickerCard  _srcPicker, _dstPicker;
        private SplitOptionsPanel _splitPanel;
        private RenameOptionsPanel _renamePanel;
        private DateAndTagPanel   _dateTagPanel;
        private AccentButton      _btnStart;
        private ProgressBar       _progressBar;
        private Label             _lblStatus;

        public MainForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            Size            = new Size(1366, 768);
            MinimumSize     = new Size(900, 600);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Theme.Background;
            DoubleBuffered  = true;

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);

            BuildLayout();
        }

        // ─────────────────────────────────────────────────────
        //  레이아웃 조립
        // ─────────────────────────────────────────────────────
        void BuildLayout()
        {
            // 타이틀바
            _titleBar = new TitleBar(this);
            Controls.Add(_titleBar);

            // 본문 — 왼쪽 패널 | 오른쪽 패널
            var splitter = new SplitContainer
            {
                Dock             = DockStyle.Fill,
                SplitterWidth    = 1,
                SplitterDistance = Theme.LeftPanelWidth,
                Panel1MinSize    = 320,
                Panel2MinSize    = 480,
                BackColor        = Theme.Background
            };
            splitter.Panel1.BackColor = Theme.Background;
            splitter.Panel2.BackColor = Theme.Background;
            Controls.Add(splitter);

            BuildLeftPanel(splitter.Panel1);
            BuildRightPanel(splitter.Panel2);
        }

        // ─────────────────────────────────────────────────────
        //  왼쪽 패널 — 폴더 선택 + 시작 버튼 + 진행 상태
        // ─────────────────────────────────────────────────────
        void BuildLeftPanel(Panel panel)
        {
            int pad = Theme.Gap;

            _srcPicker = new FolderPickerCard("📂  원본 폴더 (사진이 있는 곳)");
            _dstPicker = new FolderPickerCard("💾  저장 위치  (비워두면 원본 폴더 내)");

            // 저장 위치는 선택 사항임을 표시
            _dstPicker.Height = 80;

            _btnStart = new AccentButton { Text = "▶   분류 시작", Height = 52 };
            _btnStart.Click += OnStartClicked;

            _progressBar = new ProgressBar
            {
                Height = 6,
                Style  = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100
            };

            _lblStatus = new Label
            {
                Text      = "폴더를 선택하고 시작하세요",
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                Height    = 20,
                AutoSize  = false
            };

            // 위에서 아래로 도킹
            var stack = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 6,
                BackColor   = Color.Transparent,
                Padding     = new Padding(pad, pad, pad, pad)
            };
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));   // src
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));   // gap
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));   // dst
            stack.RowStyles.Add(new RowStyle(SizeType.Percent,  100));  // spacer
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));   // progress
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));   // status

            stack.Controls.Add(_srcPicker,    0, 0);
            stack.Controls.Add(new Panel { BackColor = Color.Transparent }, 0, 1);
            stack.Controls.Add(_dstPicker,    0, 2);
            stack.Controls.Add(_btnStart,     0, 3);
            stack.Controls.Add(_progressBar,  0, 4);
            stack.Controls.Add(_lblStatus,    0, 5);

            // 도킹 보조
            _srcPicker.Dock   = DockStyle.Fill;
            _dstPicker.Dock   = DockStyle.Fill;
            _btnStart.Dock    = DockStyle.Bottom;
            _progressBar.Dock = DockStyle.Fill;
            _lblStatus.Dock   = DockStyle.Fill;

            panel.Controls.Add(stack);
        }

        // ─────────────────────────────────────────────────────
        //  오른쪽 패널 — 옵션 카드들 (스크롤)
        // ─────────────────────────────────────────────────────
        void BuildRightPanel(Panel panel)
        {
            int pad = Theme.Gap;

            _splitPanel   = new SplitOptionsPanel();
            _renamePanel  = new RenameOptionsPanel();
            _dateTagPanel = new DateAndTagPanel();

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

            var stack = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount    = 3,
                AutoSize    = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor   = Color.Transparent,
                Padding     = new Padding(pad, pad, pad + 4, pad),
                Width       = panel.Width
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _splitPanel.Dock   = DockStyle.Fill; _splitPanel.Margin   = new Padding(0, 0, 0, pad);
            _renamePanel.Dock  = DockStyle.Fill; _renamePanel.Margin  = new Padding(0, 0, 0, pad);
            _dateTagPanel.Dock = DockStyle.Fill; _dateTagPanel.Margin = new Padding(0, 0, 0, pad);

            stack.Controls.Add(_splitPanel,   0, 0);
            stack.Controls.Add(_renamePanel,  0, 1);
            stack.Controls.Add(_dateTagPanel, 0, 2);

            scroll.Controls.Add(stack);
            stack.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            panel.Controls.Add(scroll);
            panel.SizeChanged += (s, e) => stack.Width = panel.Width - 4;
        }

        // ─────────────────────────────────────────────────────
        //  분류 시작
        // ─────────────────────────────────────────────────────
        void OnStartClicked(object sender, EventArgs e)
        {
            string src = _srcPicker.SelectedPath;
            if (string.IsNullOrEmpty(src) || !Directory.Exists(src))
            {
                MessageBox.Show("원본 폴더를 선택해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string dst = string.IsNullOrEmpty(_dstPicker.SelectedPath) ? src : _dstPicker.SelectedPath;

            string[] rawTags = _dateTagPanel.TagInput.ForeColor == Theme.TextSecondary
                ? new string[0]
                : _dateTagPanel.TagInput.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();

            var job = new SplitJob
            {
                SourceDir       = src,
                TargetDir       = dst,
                ChunkSize       = (int)_splitPanel.ChunkSize.Value,
                FolderPrefix    = string.IsNullOrWhiteSpace(_splitPanel.FolderPrefix.Text) ? "사진분할" : _splitPanel.FolderPrefix.Text.Trim(),
                RenameMode      = _renamePanel.RdoSeq.Checked ? "seq" : _renamePanel.RdoPrefix.Checked ? "prefix" : _renamePanel.RdoCustom.Checked ? "custom" : _renamePanel.RdoStrip.Checked ? "strip" : "none",
                FormatTemplate  = _renamePanel.FormatTemplate.Text,
                StartNumber     = (int)_renamePanel.StartNumber.Value,
                PaddingDigits   = (int)_renamePanel.PaddingDigits.Value,
                SetCreatedDate  = _dateTagPanel.ChkSetCreated.Checked,
                CreatedDate     = _dateTagPanel.DtCreated.Value,
                SetModifiedDate = _dateTagPanel.ChkSetModified.Checked,
                ModifiedDate    = _dateTagPanel.DtModified.Value,
                Tags            = rawTags
            };

            _btnStart.Enabled    = false;
            _progressBar.Value   = 0;
            SetStatus("시작 중...", Theme.AccentPurple);

            new Thread(() =>
            {
                try
                {
                    FileSplitter.Execute(job, (cur, total, msg) =>
                    {
                        int pct = total > 0 ? (int)((double)cur / total * 100) : 0;
                        Invoke(new Action(() =>
                        {
                            _progressBar.Value = Math.Min(pct, 100);
                            SetStatus(msg, Theme.AccentBlue);
                        }));
                    });
                    Invoke(new Action(() =>
                    {
                        _progressBar.Value = 100;
                        SetStatus("✅  완료! 모든 파일이 이동되었습니다.", Theme.SuccessGreen);
                        _btnStart.Enabled = true;
                    }));
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() =>
                    {
                        SetStatus("오류: " + ex.Message, Color.FromArgb(220, 60, 80));
                        _btnStart.Enabled = true;
                    }));
                }
            })
            { IsBackground = true }.Start();
        }

        void SetStatus(string text, Color color)
        {
            _lblStatus.Text      = text;
            _lblStatus.ForeColor = color;
        }

        // ─────────────────────────────────────────────────────
        //  프레임리스 창 리사이즈 (WM_NCHITTEST 오버라이드)
        // ─────────────────────────────────────────────────────
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
            {
                var pos = PointToClient(new Point(m.LParam.ToInt32() & 0xFFFF, m.LParam.ToInt32() >> 16));
                bool L = pos.X < ResizeBorder, R = pos.X > Width  - ResizeBorder;
                bool T = pos.Y < ResizeBorder, B = pos.Y > Height - ResizeBorder;

                if (T && L)       m.Result = new IntPtr(HTTOPLEFT);
                else if (T && R)  m.Result = new IntPtr(HTTOPRIGHT);
                else if (B && L)  m.Result = new IntPtr(HTBOTTOMLEFT);
                else if (B && R)  m.Result = new IntPtr(HTBOTTOMRIGHT);
                else if (L)       m.Result = new IntPtr(HTLEFT);
                else if (R)       m.Result = new IntPtr(HTRIGHT);
                else if (T)       m.Result = new IntPtr(HTTOP);
                else if (B)       m.Result = new IntPtr(HTBOTTOM);
                else base.WndProc(ref m);
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // 창 테두리 (1px 미묘한 테두리)
            using (var pen = new Pen(Theme.Border, 1))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
