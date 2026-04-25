using System.Drawing;
using System.Drawing.Drawing2D;

namespace PhotoSplitter
{
    /// <summary>앱 전체 디자인 토큰 및 그리기 유틸리티</summary>
    static class Theme
    {
        // ── 색상 ──────────────────────────────────────────────
        public static readonly Color Background   = Color.FromArgb(248, 248, 252);
        public static readonly Color Surface      = Color.White;
        public static readonly Color Border       = Color.FromArgb(228, 225, 242);
        public static readonly Color ShadowColor  = Color.FromArgb(20, 80, 60, 140);
        public static readonly Color TitleBarBg   = Color.FromArgb(251, 251, 254);
        public static readonly Color TitleBarLine = Color.FromArgb(235, 232, 248);

        public static readonly Color TextPrimary   = Color.FromArgb(28, 24, 50);
        public static readonly Color TextSecondary = Color.FromArgb(108, 104, 135);
        public static readonly Color TextHint      = Color.FromArgb(180, 175, 205);

        public static readonly Color AccentPurple = Color.FromArgb(108, 92, 231);
        public static readonly Color AccentBlue   = Color.FromArgb(66, 130, 220);
        public static readonly Color AccentPink   = Color.FromArgb(232, 88, 148);
        public static readonly Color SuccessGreen = Color.FromArgb(0, 184, 120);
        public static readonly Color CloseHover   = Color.FromArgb(232, 64, 80);

        // ── 폰트 ──────────────────────────────────────────────
        public static readonly Font FontAppTitle  = new Font("Segoe UI", 10.5f, FontStyle.Regular);
        public static readonly Font FontCardTitle = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font FontBody      = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font FontSmall     = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        public static readonly Font FontMono      = new Font("Consolas", 9f, FontStyle.Regular);

        // ── 치수 ──────────────────────────────────────────────
        public const int TitleBarHeight = 46;
        public const int LeftPanelWidth = 400;
        public const int CardRadius     = 14;
        public const int BtnRadius      = 10;
        public const int Gap            = 14;
        public const int CardPad        = 20;

        // ── 그리기 헬퍼 ───────────────────────────────────────

        /// <summary>둥근 사각형 GraphicsPath 생성</summary>
        public static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X,                rect.Y,                radius, radius, 180, 90);
            path.AddArc(rect.Right  - radius,  rect.Y,                radius, radius, 270, 90);
            path.AddArc(rect.Right  - radius,  rect.Bottom - radius,  radius, radius,   0, 90);
            path.AddArc(rect.X,                rect.Bottom - radius,  radius, radius,  90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>보라→파란→핑크 그라디언트로 직사각형 채우기</summary>
        public static void FillAccentGradient(Graphics g, Rectangle rect)
        {
            if (rect.Width < 1 || rect.Height < 1) return;
            using (var brush = new LinearGradientBrush(rect, AccentPurple, AccentPink, LinearGradientMode.Horizontal))
            {
                var blend = new ColorBlend(3);
                blend.Colors    = new[] { AccentPurple, AccentBlue, AccentPink };
                blend.Positions = new[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = blend;
                g.FillRectangle(brush, rect);
            }
        }

        /// <summary>카드 테두리 및 미묘한 그림자 페인트</summary>
        public static void PaintCardSurface(Graphics g, Rectangle rect, int radius)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // 미묘한 드롭 섀도
            for (int i = 3; i >= 1; i--)
            {
                var sr = new Rectangle(rect.X + i, rect.Y + i + 1, rect.Width - i, rect.Height - i);
                using (var sp = CreateRoundedPath(sr, radius))
                using (var sb = new SolidBrush(Color.FromArgb(i * 6, 80, 60, 140)))
                    g.FillPath(sb, sp);
            }
            // 카드 배경
            using (var path = CreateRoundedPath(rect, radius))
            {
                using (var fill = new SolidBrush(Surface))
                    g.FillPath(fill, path);
                using (var pen = new Pen(Border, 1.2f))
                    g.DrawPath(pen, path);
            }
        }
    }
}
