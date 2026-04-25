using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PhotoSplitter
{
    /// <summary>분류 작업에 필요한 모든 설정값을 담는 데이터 클래스</summary>
    class SplitJob
    {
        public string SourceDir     { get; set; }
        public string TargetDir     { get; set; }
        public int    ChunkSize     { get; set; }
        public string FolderPrefix  { get; set; }

        // 이름 변경
        public string RenameMode    { get; set; } // none|seq|prefix|custom|strip
        public string FormatTemplate { get; set; }
        public int    StartNumber   { get; set; }
        public int    PaddingDigits { get; set; }

        // 날짜 수정
        public bool?    SetCreatedDate  { get; set; }
        public DateTime CreatedDate     { get; set; }
        public bool?    SetModifiedDate { get; set; }
        public DateTime ModifiedDate    { get; set; }

        // 태그
        public string[] Tags { get; set; }
    }

    /// <summary>파일 분류 및 이동 실행 클래스</summary>
    static class FileSplitter
    {
        public static readonly string[] SupportedExtensions =
            { ".jpg", ".jpeg", ".png", ".heic", ".heif", ".gif", ".bmp", ".webp", ".tiff" };

        public delegate void ProgressCallback(int current, int total, string message);

        /// <summary>지정된 SplitJob 설정에 따라 파일을 분류·이동합니다</summary>
        public static void Execute(SplitJob job, ProgressCallback onProgress)
        {
            List<string> files = CollectImageFiles(job.SourceDir);
            int total = files.Count;
            if (total == 0) { onProgress(0, 0, "사진 파일이 없습니다."); return; }

            files.Sort(StringComparer.OrdinalIgnoreCase);
            int totalFolders = (int)Math.Ceiling((double)total / job.ChunkSize);
            int seqIndex = job.StartNumber;

            for (int i = 0; i < totalFolders; i++)
            {
                string folderName = string.Format("{0}_{1:D2}", job.FolderPrefix, i + 1);
                string destDir    = Path.Combine(job.TargetDir, folderName);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                var chunk = files.Skip(i * job.ChunkSize).Take(job.ChunkSize).ToList();
                foreach (string srcFile in chunk)
                {
                    string newName = BuildFileName(srcFile, job, seqIndex);
                    string destFile = ResolveDestPath(destDir, newName);

                    File.Move(srcFile, destFile);
                    MetadataWriter.ApplyMetadata(destFile, job);

                    seqIndex++;
                    onProgress(seqIndex - job.StartNumber, total,
                        string.Format("{0}  ({1}/{2})", folderName, seqIndex - job.StartNumber, total));
                }
            }
        }

        /// <summary>원본 폴더에서 지원하는 이미지 파일 목록 수집</summary>
        static List<string> CollectImageFiles(string dir)
        {
            return Directory.GetFiles(dir)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToList();
        }

        /// <summary>설정에 따라 새 파일명 생성 (확장자 포함)</summary>
        static string BuildFileName(string srcFile, SplitJob job, int seqIndex)
        {
            string ext     = Path.GetExtension(srcFile);
            string nameOnly = Path.GetFileNameWithoutExtension(srcFile);
            string seq     = seqIndex.ToString().PadLeft(job.PaddingDigits, '0');

            switch (job.RenameMode)
            {
                case "seq":
                    return seq + ext;
                case "prefix":
                    return string.Format("{0}_{1}{2}", seq, nameOnly, ext);
                case "custom":
                    return job.FormatTemplate.Replace("{seq}", seq).Replace("{name}", nameOnly) + ext;
                case "strip":
                    string stripped = Regex.Replace(nameOnly, @"\d", "").Trim('_', ' ', '-');
                    if (string.IsNullOrWhiteSpace(stripped)) stripped = "Photo";
                    return stripped + ext;
                default:
                    return nameOnly + ext;
            }
        }

        /// <summary>대상 경로 결정 (이름 충돌 시 자동 번호 부여)</summary>
        static string ResolveDestPath(string destDir, string fileName)
        {
            string dest = Path.Combine(destDir, fileName);
            if (!File.Exists(dest)) return dest;
            string nameOnly = Path.GetFileNameWithoutExtension(fileName);
            string ext      = Path.GetExtension(fileName);
            int    dup      = 1;
            while (File.Exists(dest))
            {
                dest = Path.Combine(destDir, string.Format("{0}_{1}{2}", nameOnly, dup++, ext));
            }
            return dest;
        }
    }

    /// <summary>파일 날짜 및 EXIF 태그 메타데이터 기록 클래스</summary>
    static class MetadataWriter
    {
        /// <summary>SplitJob 설정에 따라 날짜/태그를 파일에 적용</summary>
        public static void ApplyMetadata(string filePath, SplitJob job)
        {
            ApplyFileDates(filePath, job);

            if (job.Tags != null && job.Tags.Length > 0)
                TryWriteJpegTags(filePath, job.Tags);
        }

        /// <summary>파일 시스템 날짜 속성 변경 (생성일 / 수정일)</summary>
        static void ApplyFileDates(string filePath, SplitJob job)
        {
            try
            {
                if (job.SetCreatedDate  == true) File.SetCreationTime(filePath,  job.CreatedDate);
                if (job.SetModifiedDate == true) File.SetLastWriteTime(filePath, job.ModifiedDate);
            }
            catch { /* 권한 없거나 지원 안 되는 파일시스템은 조용히 건너뜀 */ }
        }

        /// <summary>JPEG 파일에 EXIF XP Keywords(Windows 탐색기 태그) 기록</summary>
        static void TryWriteJpegTags(string filePath, string[] tags)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext != ".jpg" && ext != ".jpeg") return;

            string tmpPath = filePath + ".tagtmp";
            Image img = null;
            try
            {
                img = Image.FromFile(filePath);

                // EXIF 0x9C9E: Windows XP Keywords (UTF-16LE)
                PropertyItem prop = (PropertyItem)System.Runtime.Serialization.FormatterServices
                    .GetUninitializedObject(typeof(PropertyItem));
                prop.Id    = 0x9C9E;
                prop.Type  = 1;
                byte[] tagBytes = Encoding.Unicode.GetBytes(string.Join(";", tags) + "\0");
                prop.Value = tagBytes;
                prop.Len   = tagBytes.Length;
                img.SetPropertyItem(prop);

                // 품질 98%로 재인코딩 (육안 품질 손실 없음)
                var codec = ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                var ep = new EncoderParameters(1);
                ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 98L);
                img.Save(tmpPath, codec, ep);
                img.Dispose(); img = null;

                File.Delete(filePath);
                File.Move(tmpPath, filePath);
            }
            catch { /* 태그 쓰기 실패해도 이동은 완료된 상태이므로 무시 */ }
            finally
            {
                if (img != null) img.Dispose();
                if (File.Exists(tmpPath)) try { File.Delete(tmpPath); } catch { }
            }
        }
    }
}
