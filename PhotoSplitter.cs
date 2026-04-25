using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace PhotoSplitter
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // ── 색상 토큰 ──────────────────────────────────────────────
    static class C
    {
        public static Color Bg      = Color.FromArgb(248, 247, 255);
        public static Color Card    = Color.White;
        public static Color Border  = Color.FromArgb(220, 215, 255);
        public static Color Text    = Color.FromArgb(36, 32, 60);
        public static Color Sub     = Color.FromArgb(130, 118, 160);
        public static Color Purple  = Color.FromArgb(120, 90, 255);
        public static Color Blue    = Color.FromArgb(70, 140, 230);
        public static Color Pink    = Color.FromArgb(248, 100, 160);
        public static Color Green   = Color.FromArgb(80, 210, 140);
    }

    // ── GraphicsPath 유틸 ──────────────────────────────────────
    static class Draw
    {
        public static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, rad, rad, 180, 90);
            p.AddArc(r.Right - rad, r.Y, rad, rad, 270, 90);
            p.AddArc(r.Right - rad, r.Bottom - rad, rad, rad, 0, 90);
            p.AddArc(r.X, r.Bottom - rad, rad, rad, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void GradientFill(Graphics g, Rectangle r)
        {
            using (var br = new LinearGradientBrush(r, C.Purple, C.Pink, LinearGradientMode.Horizontal))
            {
                var bl = new ColorBlend(3);
                bl.Colors    = new[] { C.Purple, C.Blue, C.Pink };
                bl.Positions = new[] { 0f, 0.5f, 1f };
                br.InterpolationColors = bl;
                g.FillRectangle(br, r);
            }
        }
    }

    // ── 카드 패널 ──────────────────────────────────────────────
    class Card : Panel
    {
        public string Title = "";
        public Card() { DoubleBuffered = true; BackColor = C.Card; Padding = new Padding(16, 40, 16, 16); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Draw.RoundRect(r, 14))
            using (var fill = new SolidBrush(C.Card))
            using (var pen  = new Pen(C.Border, 1.5f))
            { g.FillPath(fill, path); g.DrawPath(pen, path); }
            using (var f = new Font("맑은 고딕", 10f, FontStyle.Bold))
            using (var br = new LinearGradientBrush(new Rectangle(14, 10, 300, 20), C.Purple, C.Pink, LinearGradientMode.Horizontal))
            {
                var bl = new ColorBlend(3); bl.Colors = new[] { C.Purple, C.Blue, C.Pink }; bl.Positions = new[] { 0f, 0.5f, 1f };
                br.InterpolationColors = bl;
                g.DrawString(Title, f, br, 16, 12);
            }
        }
    }

    // ── 폴더 드롭 패널 ────────────────────────────────────────
    class FolderCard : Panel
    {
        public string Path = "";
        private Label _lbl;
        private Button _btn;
        public FolderCard(string title)
        {
            DoubleBuffered = true; Height = 70; AllowDrop = true;
            BackColor = C.Card; Cursor = Cursors.Hand;
            var t = new Label { Text = title, Font = new Font("맑은 고딕", 9f, FontStyle.Bold), ForeColor = C.Sub, Location = new Point(14, 7), AutoSize = true };
            _lbl = new Label { Text = "폴더를 선택하거나 드래그하세요", Font = new Font("맑은 고딕", 9.5f), ForeColor = C.Sub, Location = new Point(14, 30), Width = 480, Height = 24 };
            _btn = new Button { Text = "선택", Location = new Point(520, 18), Width = 68, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = C.Purple, ForeColor = Color.White, Font = new Font("맑은 고딕", 9f, FontStyle.Bold), Cursor = Cursors.Hand };
            _btn.FlatAppearance.BorderSize = 0;
            var br = new Button(); // round region for btn
            _btn.Region = new Region(Draw.RoundRect(new Rectangle(0, 0, 68, 34), 10));
            _btn.Click += (s, e) => PickFolder();
            DragEnter += (s, e) => { e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; };
            DragDrop  += (s, e) => { var d = (string[])e.Data.GetData(DataFormats.FileDrop); if (d.Length > 0 && Directory.Exists(d[0])) SetPath(d[0]); };
            Controls.Add(t); Controls.Add(_lbl); Controls.Add(_btn);
        }
        void PickFolder()
        {
            using (var d = new FolderBrowserDialog())
                if (d.ShowDialog() == DialogResult.OK) SetPath(d.SelectedPath);
        }
        public void SetPath(string p) { Path = p; _lbl.Text = p; _lbl.ForeColor = C.Text; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Draw.RoundRect(r, 12)) using (var fill = new SolidBrush(C.Card)) using (var pen = new Pen(C.Border, 1.5f))
            { g.FillPath(fill, path); g.DrawPath(pen, path); }
        }
    }

    // ── 그라디언트 버튼 ────────────────────────────────────────
    class GradBtn : Button
    {
        public GradBtn() { FlatStyle = FlatStyle.Flat; ForeColor = Color.White; Font = new Font("맑은 고딕", 11f, FontStyle.Bold); FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand; DoubleBuffered = true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = ClientRectangle;
            using (var path = Draw.RoundRect(new Rectangle(0, 0, r.Width - 1, r.Height - 1), 12))
            {
                Draw.GradientFill(g, r);
                g.SetClip(path);
                Draw.GradientFill(g, r);
                g.ResetClip();
            }
            TextRenderer.DrawText(g, Text, Font, r, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // ── 메인 폼 ───────────────────────────────────────────────
    public class MainForm : Form
    {
        FolderCard _src, _dst;
        NumericUpDown _chunk; TextBox _prefix;
        RadioButton _rdoNone, _rdoSeq, _rdoPre, _rdoCustom, _rdoStrip;
        TextBox _fmtTemplate; NumericUpDown _startNum, _digits;
        CheckBox _chkCreated, _chkModified; DateTimePicker _dtCreated, _dtModified;
        TextBox _txtTags;
        GradBtn _btnStart;
        ProgressBar _prog; Label _status; RichTextBox _log;
        Panel _scrollContent;

        public MainForm()
        {
            Text = "사진 파일 분류 프로그램";
            Size = new Size(700, 900);
            MinimumSize = new Size(640, 800);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = C.Bg;
            DoubleBuffered = true;
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);
            BuildUI();
        }

        void BuildUI()
        {
            // 헤더
            var header = new Panel { Dock = DockStyle.Top, Height = 90 };
            header.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                Draw.GradientFill(g, header.ClientRectangle);
                using (var f = new Font("맑은 고딕", 16f, FontStyle.Bold))
                    g.DrawString("📁  사진 파일 분류 프로그램", f, Brushes.White, 22, 16);
                using (var f2 = new Font("맑은 고딕", 9.5f))
                using (var b = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                    g.DrawString("폴더를 선택하거나 드래그&드롭 → 옵션 설정 → 분류 시작", f2, b, 24, 54);
            };
            Controls.Add(header);

            // 스크롤 패널
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = C.Bg, Padding = new Padding(20, 16, 20, 20) };
            Controls.Add(scroll);

            _scrollContent = new Panel { AutoSize = true, BackColor = C.Bg };
            scroll.Controls.Add(_scrollContent);

            int y = 0;
            int W = 620;

            // 폴더 선택
            _src = new FolderCard("📂  원본 폴더 (사진이 있는 곳)"); _src.Location = new Point(0, y); _src.Width = W; _scrollContent.Controls.Add(_src); y += 78;
            _dst = new FolderCard("💾  저장 위치 (비워두면 원본 폴더 내)"); _dst.Location = new Point(0, y); _dst.Width = W; _scrollContent.Controls.Add(_dst); y += 86;

            // 분류 옵션
            var cOpt = new Card { Title = "⚙️  분류 옵션", Location = new Point(0, y), Width = W, Height = 90 }; _scrollContent.Controls.Add(cOpt); y += 98;
            AddLabel(cOpt, "폴더당 파일 수:", 0, 0);
            _chunk = new NumericUpDown { Location = new Point(120, 0), Width = 90, Minimum = 1, Maximum = 99999, Value = 500, Font = new Font("맑은 고딕", 10f) }; cOpt.Controls.Add(_chunk);
            AddLabel(cOpt, "폴더명 접두사:", 240, 0);
            _prefix = new TextBox { Location = new Point(360, 0), Width = 180, Text = "사진분할", Font = new Font("맑은 고딕", 10f) }; cOpt.Controls.Add(_prefix);

            // 이름 변경
            var cName = new Card { Title = "✏️  파일 이름 변경", Location = new Point(0, y), Width = W, Height = 195 }; _scrollContent.Controls.Add(cName); y += 203;
            _rdoNone   = AddRadio(cName, "변경 안 함 (원본 유지)", 0, 0, true);
            _rdoSeq    = AddRadio(cName, "순번으로 완전히 바꾸기", 0, 26, false);
            _rdoPre    = AddRadio(cName, "순번을 원본 이름 앞에 붙이기", 0, 52, false);
            _rdoCustom = AddRadio(cName, "사용자 정의 형식:", 0, 78, false);
            _rdoStrip  = AddRadio(cName, "파일 이름에서 숫자 제거", 0, 104, false);
            _fmtTemplate = new TextBox { Location = new Point(155, 76), Width = 200, Text = "{seq}_{name}", Font = new Font("Consolas", 9.5f), ForeColor = C.Sub }; cName.Controls.Add(_fmtTemplate);
            AddLabel(cName, "  {seq} = 순번,  {name} = 원본이름", 155, 100);
            AddLabel(cName, "시작 번호:", 0, 130); _startNum = new NumericUpDown { Location = new Point(90, 128), Width = 80, Minimum = 0, Maximum = 99999, Value = 1 }; cName.Controls.Add(_startNum);
            AddLabel(cName, "자릿수:", 200, 130); _digits = new NumericUpDown { Location = new Point(260, 128), Width = 70, Minimum = 1, Maximum = 10, Value = 3 }; cName.Controls.Add(_digits);

            // 날짜 수정
            var cDate = new Card { Title = "📅  파일 날짜 수정", Location = new Point(0, y), Width = W, Height = 110 }; _scrollContent.Controls.Add(cDate); y += 118;
            _chkCreated = new CheckBox { Text = "생성 날짜 변경:", Location = new Point(0, 0), AutoSize = true }; cDate.Controls.Add(_chkCreated);
            _dtCreated = new DateTimePicker { Location = new Point(130, -2), Width = 200, Format = DateTimePickerFormat.Short, Value = DateTime.Now, Enabled = false }; cDate.Controls.Add(_dtCreated);
            _chkCreated.CheckedChanged += (s, e) => _dtCreated.Enabled = _chkCreated.Checked;
            _chkModified = new CheckBox { Text = "수정 날짜 변경:", Location = new Point(0, 32), AutoSize = true }; cDate.Controls.Add(_chkModified);
            _dtModified = new DateTimePicker { Location = new Point(130, 30), Width = 200, Format = DateTimePickerFormat.Short, Value = DateTime.Now, Enabled = false }; cDate.Controls.Add(_dtModified);
            _chkModified.CheckedChanged += (s, e) => _dtModified.Enabled = _chkModified.Checked;
            AddLabel(cDate, "※ JPEG 파일에만 날짜 메타데이터도 함께 수정됩니다", 0, 62);

            // 태그
            var cTag = new Card { Title = "🏷️  파일 태그 (Windows 탐색기 태그)", Location = new Point(0, y), Width = W, Height = 90 }; _scrollContent.Controls.Add(cTag); y += 98;
            AddLabel(cTag, "태그 입력:", 0, 0);
            _txtTags = new TextBox { Location = new Point(80, -2), Width = 450, Font = new Font("맑은 고딕", 9.5f), ForeColor = C.Sub, Text = "쉼표로 구분  예) 여행, 2024, 뉴진스" };
            _txtTags.GotFocus += (s, e2) => { if (_txtTags.ForeColor == C.Sub) { _txtTags.Text = ""; _txtTags.ForeColor = C.Text; } };
            _txtTags.LostFocus += (s, e2) => { if (string.IsNullOrEmpty(_txtTags.Text)) { _txtTags.Text = "쉼표로 구분  예) 여행, 2024, 뉴진스"; _txtTags.ForeColor = C.Sub; } }; cTag.Controls.Add(_txtTags);
            AddLabel(cTag, "※ JPEG 파일에 EXIF 키워드로 저장됩니다 (Windows 탐색기 태그와 연동)", 0, 30);

            // 시작 버튼
            _btnStart = new GradBtn { Text = "▶  분류 시작", Location = new Point(0, y), Width = W, Height = 50 }; _scrollContent.Controls.Add(_btnStart); _btnStart.Click += OnStart; y += 58;

            // 진행
            _prog = new ProgressBar { Location = new Point(0, y), Width = W, Height = 12, Style = ProgressBarStyle.Continuous }; _scrollContent.Controls.Add(_prog); y += 20;
            _status = new Label { Location = new Point(0, y), Width = W, Height = 22, ForeColor = C.Purple, Font = new Font("맑은 고딕", 9f) }; _scrollContent.Controls.Add(_status); y += 26;
            _log = new RichTextBox { Location = new Point(0, y), Width = W, Height = 160, ReadOnly = true, BackColor = Color.FromArgb(22, 20, 38), ForeColor = Color.FromArgb(160, 255, 200), Font = new Font("Consolas", 9f), BorderStyle = BorderStyle.None }; _scrollContent.Controls.Add(_log);
            y += 168;
            _scrollContent.Height = y + 20;
        }

        Label AddLabel(Control parent, string text, int x, int y)
        {
            var l = new Label { Text = text, Location = new Point(parent.Padding.Left + x, parent.Padding.Top + y), AutoSize = true, ForeColor = C.Sub, Font = new Font("맑은 고딕", 9f) };
            parent.Controls.Add(l); return l;
        }
        RadioButton AddRadio(Control parent, string text, int x, int y, bool chk)
        {
            var r = new RadioButton { Text = text, Location = new Point(parent.Padding.Left + x, parent.Padding.Top + y), AutoSize = true, Checked = chk, ForeColor = C.Text, Font = new Font("맑은 고딕", 9.5f) };
            parent.Controls.Add(r); return r;
        }

        void OnStart(object sender, EventArgs e)
        {
            string src = _src.Path;
            if (string.IsNullOrEmpty(src) || !Directory.Exists(src))
            { MessageBox.Show("원본 폴더를 선택해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string dst = string.IsNullOrEmpty(_dst.Path) ? src : _dst.Path;
            int chunk  = (int)_chunk.Value;
            string pfx = string.IsNullOrWhiteSpace(_prefix.Text) ? "사진분할" : _prefix.Text.Trim();
            string mode = _rdoSeq.Checked ? "seq" : _rdoPre.Checked ? "pre" : _rdoCustom.Checked ? "custom" : _rdoStrip.Checked ? "strip" : "none";
            string fmt  = _fmtTemplate.Text.Trim();
            int startN  = (int)_startNum.Value;
            int digs    = (int)_digits.Value;
            bool setCreated  = _chkCreated.Checked;
            bool setModified = _chkModified.Checked;
            DateTime dtC = _dtCreated.Value;
            DateTime dtM = _dtModified.Value;
            string[] tags = _txtTags.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();

            _btnStart.Enabled = false; _log.Clear(); _prog.Value = 0; SetStatus("파일 목록 수집 중...");

            new Thread(() => RunWork(src, dst, chunk, pfx, mode, fmt, startN, digs, setCreated, dtC, setModified, dtM, tags)) { IsBackground = true }.Start();
        }

        void RunWork(string src, string dst, int chunk, string pfx, string mode, string fmt, int startN, int digs, bool setC, DateTime dtC, bool setM, DateTime dtM, string[] tags)
        {
            string[] exts = { ".jpg", ".jpeg", ".png", ".heic", ".heif", ".gif", ".bmp", ".webp", ".tiff" };
            List<string> files;
            try { files = Directory.GetFiles(src).Where(f => exts.Contains(Path.GetExtension(f).ToLower())).OrderBy(f => f).ToList(); }
            catch (Exception ex) { Log("오류: " + ex.Message, Color.Red); EnableBtn(); return; }

            int total = files.Count;
            if (total == 0) { Log("사진 파일이 없습니다.", Color.Yellow); EnableBtn(); return; }
            int totalFolders = (int)Math.Ceiling((double)total / chunk);
            SetStatus(string.Format("총 {0}장 → {1}개 묶음으로 이동 시작", total, totalFolders));
            SetProg(0, total);

            int idx = startN;
            for (int i = 0; i < totalFolders; i++)
            {
                string folderName = string.Format("{0}_{1:D2}", pfx, i + 1);
                string destDir = Path.Combine(dst, folderName);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                var ch = files.Skip(i * chunk).Take(chunk).ToList();
                foreach (string file in ch)
                {
                    string ext = Path.GetExtension(file);
                    string orig = Path.GetFileNameWithoutExtension(file);
                    string seq = idx.ToString().PadLeft(digs, '0');
                    string newName;

                    switch (mode)
                    {
                        case "seq":    newName = seq + ext; break;
                        case "pre":    newName = string.Format("{0}_{1}{2}", seq, orig, ext); break;
                        case "strip":
                            string stripped = Regex.Replace(orig, @"\d", "").Trim('_', ' ', '-');
                            if (string.IsNullOrWhiteSpace(stripped)) stripped = "Photo";
                            newName = stripped + ext;
                            int dup = 1; string chk = Path.Combine(destDir, newName);
                            while (File.Exists(chk)) { newName = string.Format("{0}_{1}{2}", stripped, dup++, ext); chk = Path.Combine(destDir, newName); }
                            break;
                        case "custom":
                            newName = fmt.Replace("{seq}", seq).Replace("{name}", orig) + ext;
                            break;
                        default: newName = orig + ext; break;
                    }

                    string dest = Path.Combine(destDir, newName);
                    if (File.Exists(dest)) dest = Path.Combine(destDir, Guid.NewGuid().ToString().Substring(0, 6) + "_" + newName);

                    File.Move(file, dest);

                    // 날짜 적용
                    try
                    {
                        if (setC) File.SetCreationTime(dest, dtC);
                        if (setM) File.SetLastWriteTime(dest, dtM);
                    }
                    catch { }

                    // 태그 적용 (JPEG만)
                    if (tags.Length > 0)
                    {
                        string extLow = ext.ToLower();
                        if (extLow == ".jpg" || extLow == ".jpeg")
                        {
                            try { WriteJpegTags(dest, tags); } catch { }
                        }
                    }

                    idx++;
                    SetProg(idx - startN, total);
                }
                Log(string.Format("✅  {0}  ({1}장)", folderName, ch.Count), C.Green);
            }
            SetStatus("🎉  완료! 모든 파일이 이동되었습니다.");
            Log("\n작업 완료!", Color.White);
            EnableBtn();
        }

        // JPEG EXIF XP Keywords 태그 쓰기 (System.Drawing, 무손실 시도 후 재인코딩)
        void WriteJpegTags(string path, string[] tags)
        {
            string tagStr = string.Join(";", tags);
            byte[] tagBytes = Encoding.Unicode.GetBytes(tagStr + "\0");

            Image img = null;
            try
            {
                img = Image.FromFile(path);
                PropertyItem prop = (PropertyItem)System.Runtime.Serialization.FormatterServices
                    .GetUninitializedObject(typeof(PropertyItem));
                prop.Id   = 0x9C9E; // XP Keywords
                prop.Type = 1;      // Byte
                prop.Value = tagBytes;
                prop.Len   = tagBytes.Length;
                img.SetPropertyItem(prop);

                string tmp = path + ".tmp";
                var codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                var ep = new EncoderParameters(1);
                ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 98L);
                img.Save(tmp, codec, ep);
                img.Dispose(); img = null;
                File.Delete(path);
                File.Move(tmp, path);
            }
            finally { if (img != null) img.Dispose(); }
        }

        void Log(string msg, Color col)
        {
            if (_log.InvokeRequired) { _log.Invoke(new Action(() => Log(msg, col))); return; }
            _log.SelectionStart = _log.TextLength; _log.SelectionColor = col;
            _log.AppendText(msg + "\n"); _log.ScrollToCaret();
        }
        void SetStatus(string msg) { if (_status.InvokeRequired) { _status.Invoke(new Action(() => SetStatus(msg))); return; } _status.Text = msg; }
        void SetProg(int v, int max) { if (_prog.InvokeRequired) { _prog.Invoke(new Action(() => SetProg(v, max))); return; } _prog.Maximum = max; _prog.Value = Math.Min(v, max); }
        void EnableBtn() { if (_btnStart.InvokeRequired) { _btnStart.Invoke(new Action(EnableBtn)); return; } _btnStart.Enabled = true; }
    }
}
