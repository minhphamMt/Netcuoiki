using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTAPLON.Models.ViewModels;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPdfDocument = QuestPDF.Fluent.Document;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace BTAPLON.Utilities
{
    public static class ReportExportHelper
    {
        static ReportExportHelper()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] StudentReportToPdf(StudentLearningReportViewModel model) =>
            CreatePdfDocument("Báo cáo học tập", column =>
            {
                column.Item().Text($"Học viên: {model.StudentName}").FontSize(12);
                column.Item().Text($"Bài tập đã giao: {model.TotalAssignments}");
                column.Item().Text($"Bài tập đã nộp: {model.CompletedAssignments}");
                column.Item().Text($"Kỳ thi đã giao: {model.TotalExams}");
                column.Item().Text($"Kỳ thi đã làm: {model.CompletedExams}");
                column.Item().Text($"Điểm TB bài tập: {FormatDecimal(model.AverageAssignmentScore)}");
                column.Item().Text($"Điểm TB kỳ thi: {FormatDecimal(model.AverageExamScore)}");

                column.Item().Text("Bài tập").SemiBold().FontSize(13);
                BuildTable(column, new[] { "Bài tập", "Lớp", "Hạn nộp", "Đã nộp", "Điểm" },
                    model.Assignments.Select(a => new[]
                    {
                        a.Title,
                        CombineClassInfo(a.ClassCode, a.CourseName),
                        FormatDate(a.DueDate),
                        FormatDate(a.SubmittedAt),
                        FormatDecimal(a.Score)
                    }));

                column.Item().Text("Kỳ thi").SemiBold().FontSize(13);
                BuildTable(column, new[] { "Kỳ thi", "Lớp", "Thời gian nộp", "Điểm" },
                    model.Exams.Select(e => new[]
                    {
                        e.Title,
                        CombineClassInfo(e.ClassCode, e.CourseName),
                        FormatDate(e.SubmittedAt),
                        FormatDouble(e.Score)
                    }));
            });

        public static byte[] StudentReportToWord(StudentLearningReportViewModel model) =>
            CreateWordDocument("Báo cáo học tập", body =>
            {
                AppendParagraph(body, $"Học viên: {model.StudentName}");
                AppendParagraph(body, $"Bài tập đã giao: {model.TotalAssignments}");
                AppendParagraph(body, $"Bài tập đã nộp: {model.CompletedAssignments}");
                AppendParagraph(body, $"Kỳ thi đã giao: {model.TotalExams}");
                AppendParagraph(body, $"Kỳ thi đã làm: {model.CompletedExams}");
                AppendParagraph(body, $"Điểm TB bài tập: {FormatDecimal(model.AverageAssignmentScore)}");
                AppendParagraph(body, $"Điểm TB kỳ thi: {FormatDecimal(model.AverageExamScore)}");

                AppendParagraph(body, "Bài tập đã nộp:", true);
                AppendTable(body, new[] { "Bài tập", "Lớp", "Hạn nộp", "Đã nộp", "Điểm" },
                    model.Assignments.Select(a => new[]
                    {
                        a.Title,
                        CombineClassInfo(a.ClassCode, a.CourseName),
                        FormatDate(a.DueDate),
                        FormatDate(a.SubmittedAt),
                        FormatDecimal(a.Score)
                    }));

                AppendParagraph(body, "Kỳ thi đã tham gia:", true);
                AppendTable(body, new[] { "Kỳ thi", "Lớp", "Thời gian nộp", "Điểm" },
                    model.Exams.Select(e => new[]
                    {
                        e.Title,
                        CombineClassInfo(e.ClassCode, e.CourseName),
                        FormatDate(e.SubmittedAt),
                        FormatDouble(e.Score)
                    }));
            });

        public static byte[] TeacherReportToPdf(TeacherPerformanceReportViewModel model) =>
            CreatePdfDocument("Báo cáo giảng dạy", column =>
            {
                column.Item().Text($"Giảng viên: {model.TeacherName}");
                column.Item().Text($"Lớp phụ trách: {model.TotalClasses}");
                column.Item().Text($"Học viên: {model.TotalStudents}");
                column.Item().Text($"Bài tập đã giao: {model.AssignmentCount}");
                column.Item().Text($"Kỳ thi đã tạo: {model.ExamCount}");
                column.Item().Text($"Điểm TB bài tập: {FormatDouble(model.AverageAssignmentScore)}");
                column.Item().Text($"Điểm TB kỳ thi: {FormatDouble(model.AverageExamScore)}");

                column.Item().Text("Chi tiết lớp học").SemiBold().FontSize(13);
                BuildTable(column,
                    new[] { "Lớp", "Khóa học", "Học viên", "Bài tập", "Kỳ thi", "TB bài tập", "TB kỳ thi" },
                    model.Classes.Select(c => new[]
                    {
                        c.ClassCode,
                        c.CourseName ?? string.Empty,
                        c.StudentCount.ToString(),
                        c.AssignmentCount.ToString(),
                        c.ExamCount.ToString(),
                        FormatDouble(c.AverageAssignmentScore),
                        FormatDouble(c.AverageExamScore)
                    }));
            });

        public static byte[] TeacherReportToWord(TeacherPerformanceReportViewModel model) =>
            CreateWordDocument("Báo cáo giảng dạy", body =>
            {
                AppendParagraph(body, $"Giảng viên: {model.TeacherName}");
                AppendParagraph(body, $"Lớp phụ trách: {model.TotalClasses}");
                AppendParagraph(body, $"Học viên: {model.TotalStudents}");
                AppendParagraph(body, $"Bài tập đã giao: {model.AssignmentCount}");
                AppendParagraph(body, $"Kỳ thi đã tạo: {model.ExamCount}");
                AppendParagraph(body, $"Điểm TB bài tập: {FormatDouble(model.AverageAssignmentScore)}");
                AppendParagraph(body, $"Điểm TB kỳ thi: {FormatDouble(model.AverageExamScore)}");

                AppendParagraph(body, "Chi tiết lớp học:", true);
                AppendTable(body,
                    new[] { "Lớp", "Khóa học", "Học viên", "Bài tập", "Kỳ thi", "TB bài tập", "TB kỳ thi" },
                    model.Classes.Select(c => new[]
                    {
                        c.ClassCode,
                        c.CourseName ?? string.Empty,
                        c.StudentCount.ToString(),
                        c.AssignmentCount.ToString(),
                        c.ExamCount.ToString(),
                        FormatDouble(c.AverageAssignmentScore),
                        FormatDouble(c.AverageExamScore)
                    }));
            });

        public static byte[] AdminReportToPdf(AdminReportViewModel model) =>
            CreatePdfDocument("Báo cáo quản trị", column =>
            {
                column.Item().Text($"Tổng người dùng: {model.TotalUsers}");
                column.Item().Text($"Học viên: {model.TotalStudents}");
                column.Item().Text($"Giảng viên: {model.TotalTeachers}");
                column.Item().Text($"Quản trị viên: {model.TotalAdmins}");
                column.Item().Text($"Khóa học: {model.TotalCourses}");
                column.Item().Text($"Lớp học: {model.TotalClasses}");
                column.Item().Text($"Bài tập: {model.TotalAssignments}");
                column.Item().Text($"Kỳ thi: {model.TotalExams}");
                column.Item().Text($"Kỳ thi đang mở: {model.ActiveExams}");
                column.Item().Text($"Thông báo: {model.TotalNotifications}");
                column.Item().Text($"Lượt ghi danh: {model.TotalEnrollments}");

                column.Item().Text("Ghi danh theo tháng").SemiBold().FontSize(13);
                BuildTable(column, new[] { "Tháng", "Số lượng" },
                    model.MonthlyEnrollments.Select(m => new[] { m.Label, m.EnrollmentCount.ToString() }));

                column.Item().Text("Lớp có nhiều học viên").SemiBold().FontSize(13);
                BuildTable(column, new[] { "Lớp", "Khóa học", "Học viên" },
                    model.TopClasses.Select(c => new[]
                    {
                        c.ClassCode,
                        c.CourseName,
                        c.StudentCount.ToString()
                    }));

                column.Item().Text("Giảng viên hoạt động").SemiBold().FontSize(13);
                BuildTable(column, new[] { "Giảng viên", "Lớp", "Bài tập", "Kỳ thi", "Tổng hoạt động" },
                    model.TopTeachers.Select(t => new[]
                    {
                        t.TeacherName,
                        t.ClassCount.ToString(),
                        t.AssignmentCount.ToString(),
                        t.ExamCount.ToString(),
                        t.TotalActivities.ToString()
                    }));
            });

        public static byte[] AdminReportToWord(AdminReportViewModel model) =>
            CreateWordDocument("Báo cáo quản trị", body =>
            {
                AppendParagraph(body, $"Tổng người dùng: {model.TotalUsers}");
                AppendParagraph(body, $"Học viên: {model.TotalStudents}");
                AppendParagraph(body, $"Giảng viên: {model.TotalTeachers}");
                AppendParagraph(body, $"Quản trị viên: {model.TotalAdmins}");
                AppendParagraph(body, $"Khóa học: {model.TotalCourses}");
                AppendParagraph(body, $"Lớp học: {model.TotalClasses}");
                AppendParagraph(body, $"Bài tập: {model.TotalAssignments}");
                AppendParagraph(body, $"Kỳ thi: {model.TotalExams}");
                AppendParagraph(body, $"Kỳ thi đang mở: {model.ActiveExams}");
                AppendParagraph(body, $"Thông báo: {model.TotalNotifications}");
                AppendParagraph(body, $"Lượt ghi danh: {model.TotalEnrollments}");

                AppendParagraph(body, "Ghi danh theo tháng:", true);
                AppendTable(body, new[] { "Tháng", "Số lượng" },
                    model.MonthlyEnrollments.Select(m => new[] { m.Label, m.EnrollmentCount.ToString() }));

                AppendParagraph(body, "Lớp có nhiều học viên:", true);
                AppendTable(body, new[] { "Lớp", "Khóa học", "Học viên" },
                    model.TopClasses.Select(c => new[]
                    {
                        c.ClassCode,
                        c.CourseName,
                        c.StudentCount.ToString()
                    }));

                AppendParagraph(body, "Giảng viên hoạt động nổi bật:", true);
                AppendTable(body, new[] { "Giảng viên", "Lớp", "Bài tập", "Kỳ thi", "Tổng hoạt động" },
                    model.TopTeachers.Select(t => new[]
                    {
                        t.TeacherName,
                        t.ClassCount.ToString(),
                        t.AssignmentCount.ToString(),
                        t.ExamCount.ToString(),
                        t.TotalActivities.ToString()
                    }));
            });

        private static byte[] CreatePdfDocument(string title, Action<ColumnDescriptor> buildContent)
        {
            var document = QuestPdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.Header().Text(title).SemiBold().FontSize(20);
                    page.Content().Column(column =>
                    {
                        column.Spacing(10);
                        buildContent(column);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static byte[] CreateWordDocument(string title, Action<Body> buildContent)
        {
            using var stream = new MemoryStream();
            using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new WordDocument();
                var body = mainPart.Document.AppendChild(new Body());

                var titleParagraph = new Paragraph();
                var titleRun = new Run();
                titleRun.RunProperties = new RunProperties(new Bold(), new FontSize { Val = "32" });
                titleRun.AppendChild(new Text(title));
                titleParagraph.Append(titleRun);
                body.Append(titleParagraph);

                buildContent(body);

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        private static void BuildTable(ColumnDescriptor column, string[] headers, IEnumerable<string[]> rows)
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in headers)
                    {
                        columns.RelativeColumn();
                    }
                });

                table.Header(header =>
                {
                    foreach (var headerText in headers)
                    {
                        header.Cell().Element(HeaderCellStyle).Text(headerText);
                    }
                });

                foreach (var row in rows)
                {
                    foreach (var cell in row)
                    {
                        table.Cell().Element(CellStyle).Text(cell ?? string.Empty);
                    }
                }
            });
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container.DefaultTextStyle(x => x.SemiBold())
                .Background(Colors.Grey.Lighten3)
                .Padding(5);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }

        private static void AppendParagraph(Body body, string text, bool bold = false)
        {
            var paragraph = new Paragraph();
            var run = new Run(new Text(text ?? string.Empty));
            if (bold)
            {
                run.RunProperties = new RunProperties(new Bold());
            }
            paragraph.Append(run);
            body.Append(paragraph);
        }

        private static void AppendTable(Body body, string[] headers, IEnumerable<string[]> rows)
        {
            var table = new Table();
            var borders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6 },
                new BottomBorder { Val = BorderValues.Single, Size = 6 },
                new LeftBorder { Val = BorderValues.Single, Size = 6 },
                new RightBorder { Val = BorderValues.Single, Size = 6 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 });

            table.AppendChild(new TableProperties(borders));

            var headerRow = new TableRow();
            foreach (var header in headers)
            {
                headerRow.Append(CreateCell(header, true));
            }
            table.Append(headerRow);

            foreach (var row in rows)
            {
                var tableRow = new TableRow();
                foreach (var cell in row)
                {
                    tableRow.Append(CreateCell(cell));
                }
                table.Append(tableRow);
            }

            body.Append(table);
        }

        private static TableCell CreateCell(string? text, bool bold = false)
        {
            var run = new Run(new Text(text ?? string.Empty));
            if (bold)
            {
                run.RunProperties = new RunProperties(new Bold());
            }
            var paragraph = new Paragraph(run);
            return new TableCell(paragraph);
        }

        private static string FormatDate(DateTime? dateTime)
        {
            return dateTime.HasValue ? dateTime.Value.ToString("dd/MM/yyyy HH:mm") : "-";
        }

        private static string FormatDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.##") : "-";
        }

        private static string FormatDouble(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.##") : "-";
        }

        private static string CombineClassInfo(string? classCode, string? courseName)
        {
            if (string.IsNullOrWhiteSpace(classCode) && string.IsNullOrWhiteSpace(courseName))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(courseName))
            {
                return classCode ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(classCode))
            {
                return courseName ?? string.Empty;
            }

            return $"{classCode} · {courseName}";
        }
    }
}