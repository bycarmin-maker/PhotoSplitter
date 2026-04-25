using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

    public class MainForm : Form
    {
        // --- Controls ---
        private Panel panelTop;
        private Label lblTitle, lblSubtitle;
        private Panel panelSource, panelTarget;
        private Label lblSourceTitle, lblSourcePath, lblTargetTitle, lblTargetPath;
        private Button btnSelectSource, btnSelectTarget;
        private GroupBox grpOptions, grpRename, grpExport;
        private NumericUpDown numChunkSize;
        private TextBox txtPrefix;
        private RadioButton rdoRenameNone, rdoRenameNum, rdoRenamePrefix, rdoRenameStrip;
        private CheckBox chkZip;
        private Button btnStart;
        private ProgressBar progressBar;
        private Label lblStatus;
        private RichTextBox rtbLog;

        private string sourcePath = "";
        private string targetPath = "";

        public MainForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "사진 파일 분류 프로그램";
            this.Size = new Size(680, 800);
            this.MinimumSize = new Size(600, 750);
            // 앱 창 아이콘 설정
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath)) this.Icon = new Icon(iconPath);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 250);
            this.Font = new Font("맑은 고딕", 9.5f);

            // ── 상단 헤더 ──────────────────────────────────────
            panelTop = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(88, 86, 214) };
            panelTop.Paint += (s, e) => {
                var r = panelTop.ClientRectangle;
                using (var br = new LinearGradientBrush(r, Color.FromArgb(118, 75, 229), Color.FromArgb(64, 112, 244), LinearGradientMode.Horizontal))
                    e.Graphics.FillRectangle(br, r);
            };
            lblTitle = new Label { Text = "📁  사진 파일 분류 프로그램", ForeColor = Color.White, Font = new Font("맑은 고딕", 15, FontStyle.Bold), Location = new Point(20, 14), AutoSize = true };
            lblSubtitle = new Label { Text = "폴더를 선택하거나 여기에 드래그하세요", ForeColor = Color.FromArgb(220, 220, 255), Font = new Font("맑은 고딕", 9), Location = new Point(22, 48), AutoSize = true };
            panelTop.Controls.Add(lblTitle);
            panelTop.Controls.Add(lblSubtitle);
            this.Controls.Add(panelTop);

            // ── 스크롤 패널 ──────────────────────────────────────
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16, 12, 16, 12) };
            this.Controls.Add(scroll);

            int y = 12;

            // ── 원본 폴더 ──────────────────────────────────────
            panelSource = CreateFolderPanel("📂  원본 폴더 (사진이 있는 곳)", ref y);
            lblSourcePath = (Label)panelSource.Controls["lblPath"];
            btnSelectSource = (Button)panelSource.Controls["btnSelect"];
            btnSelectSource.Click += (s, e) => PickFolder(ref sourcePath, lblSourcePath);
            panelSource.AllowDrop = true;
            panelSource.DragEnter += Panel_DragEnter;
            panelSource.DragDrop += (s, e) => DropFolder(e, ref sourcePath, lblSourcePath);
            scroll.Controls.Add(panelSource);
            y += panelSource.Height + 10;

            // ── 대상 폴더 ──────────────────────────────────────
            panelTarget = CreateFolderPanel("💾  저장 위치 (USB·구글 드라이브 등)", ref y);
            lblTargetPath = (Label)panelTarget.Controls["lblPath"];
            btnSelectTarget = (Button)panelTarget.Controls["btnSelect"];
            btnSelectTarget.Click += (s, e) => PickFolder(ref targetPath, lblTargetPath);
            panelTarget.AllowDrop = true;
            panelTarget.DragEnter += Panel_DragEnter;
            panelTarget.DragDrop += (s, e) => DropFolder(e, ref targetPath, lblTargetPath);
            var lblTargetHint = new Label { Text = "비워두면 원본 폴더 안에 저장됩니다", ForeColor = Color.Gray, Font = new Font("맑은 고딕", 8.5f), Location = new Point(14, 58), AutoSize = true };
            panelTarget.Controls.Add(lblTargetHint);
            scroll.Controls.Add(panelTarget);
            y += panelTarget.Height + 12;

            // ── 분류 옵션 ──────────────────────────────────────
            grpOptions = MakeGroup("⚙️  분류 옵션", y, 110);
            AddLabel(grpOptions, "한 폴더당 파일 수:", 16, 28);
            numChunkSize = new NumericUpDown { Location = new Point(150, 26), Width = 90, Minimum = 1, Maximum = 99999, Value = 500, Font = new Font("맑은 고딕", 10) };
            grpOptions.Controls.Add(numChunkSize);
            AddLabel(grpOptions, "폴더 이름 접두사:", 16, 62);
            txtPrefix = new TextBox { Location = new Point(150, 60), Width = 180, Text = "사진분할", Font = new Font("맑은 고딕", 10) };
            grpOptions.Controls.Add(txtPrefix);
            scroll.Controls.Add(grpOptions);
            y += grpOptions.Height + 10;

            // ── 이름 변경 ──────────────────────────────────────
            grpRename = MakeGroup("✏️  파일 이름 일괄 변경", y, 150);
            rdoRenameNone   = AddRadio(grpRename, "이름 변경 안 함 (원본 유지)", 16, 28, true);
            rdoRenameNum    = AddRadio(grpRename, "순번으로 완전히 바꾸기  (예: 001.jpg, 002.jpg)", 16, 54, false);
            rdoRenamePrefix = AddRadio(grpRename, "원본 이름 앞에 순번 붙이기  (예: 001_IMG_1234.jpg)", 16, 80, false);
            rdoRenameStrip  = AddRadio(grpRename, "파일 이름에서 숫자 모두 제거", 16, 106, false);
            scroll.Controls.Add(grpRename);
            y += grpRename.Height + 10;

            // ── 내보내기 ──────────────────────────────────────
            grpExport = MakeGroup("📦  내보내기 옵션", y, 80);
            chkZip = new CheckBox { Text = "각 묶음을 ZIP 압축 파일로도 만들기  (공유·전송에 유용)", Location = new Point(16, 30), AutoSize = true };
            grpExport.Controls.Add(chkZip);
            scroll.Controls.Add(grpExport);
            y += grpExport.Height + 14;

            // ── 시작 버튼 ──────────────────────────────────────
            btnStart = new Button
            {
                Text = "▶  분류 시작",
                Location = new Point(16, y), Width = 620, Height = 46,
                BackColor = Color.FromArgb(88, 86, 214), ForeColor = Color.White,
                Font = new Font("맑은 고딕", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += BtnStart_Click;
            RoundControl(btnStart, 8);
            scroll.Controls.Add(btnStart);
            y += btnStart.Height + 12;

            // ── 진행 ──────────────────────────────────────
            progressBar = new ProgressBar { Location = new Point(16, y), Width = 620, Height = 14, Style = ProgressBarStyle.Continuous };
            scroll.Controls.Add(progressBar);
            y += progressBar.Height + 6;

            lblStatus = new Label { Location = new Point(16, y), Width = 620, AutoSize = false, Height = 20, ForeColor = Color.FromArgb(88, 86, 214), Font = new Font("맑은 고딕", 9) };
            scroll.Controls.Add(lblStatus);
            y += lblStatus.Height + 8;

            rtbLog = new RichTextBox { Location = new Point(16, y), Width = 620, Height = 150, ReadOnly = true, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.FromArgb(180, 255, 180), Font = new Font("Consolas", 9), BorderStyle = BorderStyle.None };
            scroll.Controls.Add(rtbLog);

            scroll.Controls[scroll.Controls.Count - 1].Height = 150;
        }

        // ── 폴더 패널 생성 헬퍼 ──────────────────────────────────
        private Panel CreateFolderPanel(string title, ref int y)
        {
            var p = new Panel { Location = new Point(16, y), Width = 620, Height = 72, BackColor = Color.White, Cursor = Cursors.Hand };
            p.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(200, 200, 220), 1.5f))
                    DrawRoundRect(g, pen, p.ClientRectangle, 10);
            };
            var lbl = new Label { Text = title, Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(60, 60, 80), Location = new Point(14, 8), AutoSize = true, Name = "lblTitle" };
            var lblPath = new Label { Text = "폴더를 선택하거나 여기에 드래그 & 드롭하세요", Font = new Font("맑은 고딕", 9), ForeColor = Color.Gray, Location = new Point(14, 34), AutoSize = false, Width = 490, Name = "lblPath" };
            var btn = new Button { Text = "폴더 선택", Location = new Point(516, 20), Width = 86, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(88, 86, 214), ForeColor = Color.White, Font = new Font("맑은 고딕", 9), Cursor = Cursors.Hand, Name = "btnSelect" };
            btn.FlatAppearance.BorderSize = 0;
            p.Controls.Add(lbl); p.Controls.Add(lblPath); p.Controls.Add(btn);
            return p;
        }

        private void Panel_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void DropFolder(DragEventArgs e, ref string path, Label lbl)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && Directory.Exists(paths[0]))
            { path = paths[0]; lbl.Text = path; lbl.ForeColor = Color.FromArgb(40, 40, 60); }
        }

        private void PickFolder(ref string path, Label lbl)
        {
            using (var d = new FolderBrowserDialog { Description = "폴더를 선택하세요", ShowNewFolderButton = true })
            {
                if (d.ShowDialog() == DialogResult.OK)
                { path = d.SelectedPath; lbl.Text = path; lbl.ForeColor = Color.FromArgb(40, 40, 60); }
            }
        }

        // ── 시작 버튼 ──────────────────────────────────────────
        private void BtnStart_Click(object sender, EventArgs e)
        {
            string src = sourcePath;
            if (string.IsNullOrEmpty(src) || !Directory.Exists(src))
            { MessageBox.Show("원본 폴더를 먼저 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string tgt = string.IsNullOrEmpty(targetPath) ? src : targetPath;
            int chunk = (int)numChunkSize.Value;
            string prefix = string.IsNullOrEmpty(txtPrefix.Text.Trim()) ? "사진분할" : txtPrefix.Text.Trim();
            string renameMode = rdoRenameNum.Checked ? "1" : rdoRenamePrefix.Checked ? "2" : rdoRenameStrip.Checked ? "3" : "0";
            bool makeZip = chkZip.Checked;

            btnStart.Enabled = false;
            rtbLog.Clear();
            progressBar.Value = 0;
            lblStatus.Text = "파일 목록 수집 중...";

            new Thread(() => RunSplit(src, tgt, chunk, prefix, renameMode, makeZip)) { IsBackground = true }.Start();
        }

        private void RunSplit(string src, string tgt, int chunk, string prefix, string renameMode, bool makeZip)
        {
            string[] validExts = { ".jpg", ".jpeg", ".png", ".heic", ".heif", ".gif", ".bmp", ".webp", ".tiff" };
            List<string> files;
            try
            {
                files = Directory.GetFiles(src)
                    .Where(f => validExts.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => f)
                    .ToList();
            }
            catch (Exception ex) { Log("오류: " + ex.Message, Color.Red); EnableBtn(); return; }

            if (files.Count == 0) { Log("사진 파일이 없습니다.", Color.Yellow); EnableBtn(); return; }

            int total = files.Count;
            int folders = (int)Math.Ceiling((double)total / chunk);
            int numDigits = Math.Max(3, total.ToString().Length);
            SetStatus(string.Format("총 {0}장 → {1}개 폴더로 이동 시작", total, folders));
            SetProgress(0, total);

            int globalIdx = 1;
            for (int i = 0; i < folders; i++)
            {
                int num = i + 1;
                string folderName = string.Format("{0}_{1:D2}", prefix, num);
                string destDir = Path.Combine(tgt, folderName);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                var chunk_ = files.Skip(i * chunk).Take(chunk).ToList();
                foreach (string file in chunk_)
                {
                    string ext = Path.GetExtension(file);
                    string nameNoExt = Path.GetFileNameWithoutExtension(file);
                    string seq = globalIdx.ToString().PadLeft(numDigits, '0');
                    string newName;

                    if (renameMode == "1") newName = seq + ext;
                    else if (renameMode == "2") newName = string.Format("{0}_{1}{2}", seq, nameNoExt, ext);
                    else if (renameMode == "3")
                    {
                        string stripped = Regex.Replace(nameNoExt, @"\d", "").Trim('_', ' ', '-');
                        if (string.IsNullOrWhiteSpace(stripped)) stripped = "Photo";
                        newName = stripped + ext;
                        // 중복 처리
                        string chk = Path.Combine(destDir, newName); int dup = 1;
                        while (File.Exists(chk)) { newName = string.Format("{0}_{1}{2}", stripped, dup++, ext); chk = Path.Combine(destDir, newName); }
                    }
                    else newName = nameNoExt + ext;

                    string dest = Path.Combine(destDir, newName);
                    if (File.Exists(dest)) dest = Path.Combine(destDir, Guid.NewGuid().ToString().Substring(0, 6) + "_" + newName);
                    File.Move(file, dest);
                    globalIdx++;
                    SetProgress(globalIdx - 1, total);
                }

                Log(string.Format("✅  {0}  →  {1}장 이동 완료", folderName, chunk_.Count), Color.FromArgb(120, 255, 120));

                if (makeZip)
                {
                    string zipPath = Path.Combine(tgt, folderName + ".zip");
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    ZipFile.CreateFromDirectory(destDir, zipPath);
                    Log(string.Format("    📦 압축 완료: {0}.zip", folderName), Color.FromArgb(180, 220, 255));
                }
            }

            SetStatus("🎉 모든 작업 완료! (파일 날짜 원본 그대로 유지됨)");
            Log("\n모든 작업이 완료되었습니다!", Color.White);
            EnableBtn();
        }

        // ── 스레드-세이프 UI 헬퍼 ──────────────────────────────
        private void Log(string msg, Color color) {
            if (rtbLog.InvokeRequired) { rtbLog.Invoke(new Action(() => Log(msg, color))); return; }
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = color;
            rtbLog.AppendText(msg + "\n");
            rtbLog.ScrollToCaret();
        }
        private void SetStatus(string msg) {
            if (lblStatus.InvokeRequired) { lblStatus.Invoke(new Action(() => lblStatus.Text = msg)); return; }
            lblStatus.Text = msg;
        }
        private void SetProgress(int val, int max) {
            if (progressBar.InvokeRequired) { progressBar.Invoke(new Action(() => SetProgress(val, max))); return; }
            progressBar.Maximum = max; progressBar.Value = Math.Min(val, max);
        }
        private void EnableBtn() {
            if (btnStart.InvokeRequired) { btnStart.Invoke(new Action(EnableBtn)); return; }
            btnStart.Enabled = true;
        }

        // ── UI 헬퍼 ──────────────────────────────────────────
        private GroupBox MakeGroup(string title, int y, int height)
        {
            return new GroupBox { Text = title, Location = new Point(16, y), Width = 620, Height = height, Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold) };
        }

        private Label AddLabel(Control parent, string text, int x, int y) {
            var l = new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("맑은 고딕", 9.5f) };
            parent.Controls.Add(l); return l;
        }
        private RadioButton AddRadio(Control parent, string text, int x, int y, bool chk) {
            var r = new RadioButton { Text = text, Location = new Point(x, y), AutoSize = true, Checked = chk };
            parent.Controls.Add(r); return r;
        }

        private void RoundControl(Control ctl, int r) {
            var p = new GraphicsPath();
            p.AddArc(0, 0, r, r, 180, 90); p.AddArc(ctl.Width - r, 0, r, r, 270, 90);
            p.AddArc(ctl.Width - r, ctl.Height - r, r, r, 0, 90); p.AddArc(0, ctl.Height - r, r, r, 90, 90);
            p.CloseFigure(); ctl.Region = new Region(p);
        }

        private void DrawRoundRect(Graphics g, Pen pen, Rectangle r, int radius) {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius, radius, 180, 90);
            path.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);
            path.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure(); g.DrawPath(pen, path);
        }
    }
}
