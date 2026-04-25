using System;
using System.Drawing;
using System.Windows.Forms;

namespace PhotoSplitter
{
    // ─────────────────────────────────────────────────────────────
    //  분할 옵션 패널 — 폴더당 파일 수, 폴더명 접두사
    // ─────────────────────────────────────────────────────────────
    class SplitOptionsPanel : CardPanel
    {
        public NumericUpDown ChunkSize  { get; private set; }
        public TextBox       FolderPrefix { get; private set; }

        public SplitOptionsPanel() : base("⚙️   분할 설정")
        {
            Height = 110;

            var row = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 1,
                BackColor   = Color.Transparent
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            ChunkSize = new NumericUpDown
            {
                Minimum = 1, Maximum = 99999, Value = 500,
                Font    = Theme.FontBody,
                Width   = 100
            };

            FolderPrefix = new TextBox
            {
                Text  = "사진분할",
                Font  = Theme.FontBody,
                Dock  = DockStyle.Fill
            };

            row.Controls.Add(MakeLabel("폴더당 파일 수"), 0, 0);
            row.Controls.Add(ChunkSize,                   1, 0);
            row.Controls.Add(MakeLabel("  폴더명 접두사"), 2, 0);
            row.Controls.Add(FolderPrefix,                3, 0);

            Controls.Add(row);
        }

        Label MakeLabel(string text)
        {
            return new Label
            {
                Text      = text,
                Font      = Theme.FontBody,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Anchor    = AnchorStyles.Left
            };
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  이름 변경 옵션 패널
    // ─────────────────────────────────────────────────────────────
    class RenameOptionsPanel : CardPanel
    {
        public RadioButton RdoNone, RdoSeq, RdoPrefix, RdoCustom, RdoStrip;
        public TextBox     FormatTemplate;
        public NumericUpDown StartNumber, PaddingDigits;

        public RenameOptionsPanel() : base("✏️   파일 이름 변경")
        {
            Height = 210;

            RdoNone   = Radio("변경 안 함 (원본 유지)", true);
            RdoSeq    = Radio("순번으로 완전히 바꾸기  (예: 001.jpg)", false);
            RdoPrefix = Radio("순번을 원본 이름 앞에 붙이기  (예: 001_IMG_1234.jpg)", false);
            RdoCustom = Radio("사용자 정의 형식:", false);
            RdoStrip  = Radio("파일 이름에서 숫자 제거", false);

            FormatTemplate = new TextBox
            {
                Text      = "{seq}_{name}",
                Font      = Theme.FontMono,
                ForeColor = Theme.TextSecondary,
                Width     = 200
            };

            var hintLbl = new Label
            {
                Text      = "{seq} = 순번   {name} = 원본 이름",
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextHint,
                AutoSize  = true
            };

            var numPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize      = true,
                BackColor     = Color.Transparent,
                WrapContents  = false
            };

            StartNumber = new NumericUpDown { Minimum = 0, Maximum = 99999, Value = 1, Font = Theme.FontBody, Width = 80 };
            PaddingDigits = new NumericUpDown { Minimum = 1, Maximum = 10, Value = 3, Font = Theme.FontBody, Width = 70 };

            numPanel.Controls.Add(Label2("시작 번호: "));
            numPanel.Controls.Add(StartNumber);
            numPanel.Controls.Add(Label2("   자릿수: "));
            numPanel.Controls.Add(PaddingDigits);

            // Stack layout
            var stack = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                AutoSize      = false
            };

            var customRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize      = true,
                BackColor     = Color.Transparent,
                WrapContents  = false
            };
            customRow.Controls.Add(RdoCustom);
            customRow.Controls.Add(FormatTemplate);
            customRow.Controls.Add(hintLbl);

            stack.Controls.Add(RdoNone);
            stack.Controls.Add(RdoSeq);
            stack.Controls.Add(RdoPrefix);
            stack.Controls.Add(customRow);
            stack.Controls.Add(RdoStrip);
            stack.Controls.Add(numPanel);

            Controls.Add(stack);
        }

        RadioButton Radio(string text, bool check)
        {
            return new RadioButton
            {
                Text      = text,
                Checked   = check,
                Font      = Theme.FontBody,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true
            };
        }

        Label Label2(string text)
        {
            return new Label
            {
                Text      = text,
                Font      = Theme.FontBody,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Anchor    = AnchorStyles.Left
            };
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  날짜 수정 + 태그 패널
    // ─────────────────────────────────────────────────────────────
    class DateAndTagPanel : CardPanel
    {
        public CheckBox      ChkSetCreated, ChkSetModified;
        public DateTimePicker DtCreated,    DtModified;
        public TextBox        TagInput;

        public DateAndTagPanel() : base("📅   날짜 수정  &  🏷️  태그")
        {
            Height = 185;

            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 4,
                BackColor   = Color.Transparent
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent,  100));

            ChkSetCreated  = Chk("생성 날짜 변경");
            ChkSetModified = Chk("수정 날짜 변경");
            DtCreated      = DatePicker();
            DtModified     = DatePicker();

            ChkSetCreated.CheckedChanged  += (s, e) => DtCreated.Enabled  = ChkSetCreated.Checked;
            ChkSetModified.CheckedChanged += (s, e) => DtModified.Enabled = ChkSetModified.Checked;

            var tagHint = new Label
            {
                Text      = "태그 (쉼표 구분, JPEG EXIF 키워드 저장):",
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Anchor    = AnchorStyles.Left | AnchorStyles.Bottom
            };

            TagInput = new TextBox
            {
                Font      = Theme.FontBody,
                Dock      = DockStyle.Fill,
                ForeColor = Theme.TextSecondary,
                Text      = "예) 여행, 2024, 뉴진스"
            };
            TagInput.GotFocus  += (s, e) => { if (TagInput.ForeColor == Theme.TextSecondary) { TagInput.Text = ""; TagInput.ForeColor = Theme.TextPrimary; } };
            TagInput.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(TagInput.Text)) { TagInput.Text = "예) 여행, 2024, 뉴진스"; TagInput.ForeColor = Theme.TextSecondary; } };

            grid.Controls.Add(ChkSetCreated,  0, 0); grid.Controls.Add(DtCreated,  1, 0);
            grid.Controls.Add(ChkSetModified, 0, 1); grid.Controls.Add(DtModified, 1, 1);
            grid.Controls.Add(tagHint,        0, 2); grid.SetColumnSpan(tagHint, 2);
            grid.Controls.Add(TagInput,       0, 3); grid.SetColumnSpan(TagInput, 2);

            Controls.Add(grid);
        }

        CheckBox Chk(string text)
        {
            return new CheckBox
            {
                Text      = text,
                Font      = Theme.FontBody,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Anchor    = AnchorStyles.Left
            };
        }

        DateTimePicker DatePicker()
        {
            return new DateTimePicker
            {
                Format  = DateTimePickerFormat.Short,
                Value   = DateTime.Now,
                Enabled = false,
                Font    = Theme.FontBody,
                Dock    = DockStyle.Fill
            };
        }
    }
}
