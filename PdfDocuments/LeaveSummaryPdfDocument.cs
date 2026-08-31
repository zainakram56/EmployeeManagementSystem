using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebAppMVC.Models;

namespace WebAppMVC.PdfDocuments
{
    public class LeaveSummaryPdfDocument : IDocument
    {
        private readonly List<EmployeeLeaveSummaryViewModel> _data;
        private readonly List<string> _leaveTypeNames;

        public LeaveSummaryPdfDocument(List<EmployeeLeaveSummaryViewModel> data)
        {
            _data = data;
            _leaveTypeNames = data
                .SelectMany(e => e.LeaveTypes)
                .Select(lt => lt.LeaveTypeName)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4.Landscape());

                page.Header()
                    .Text("Leave Summary Report")
                    .SemiBold().FontSize(18);

                page.Content()
                    .PaddingVertical(15)
                    .Table(table =>
                    {
                        // Columns: Employee, Department, + ek column har LeaveType ke liye
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Employee
                            columns.RelativeColumn(2); // Department
                            foreach (var _ in _leaveTypeNames)
                            {
                                columns.RelativeColumn(1.5f);
                            }
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Employee");
                            header.Cell().Element(HeaderCellStyle).Text("Department");
                            foreach (var typeName in _leaveTypeNames)
                            {
                                header.Cell().Element(HeaderCellStyle).Text($"{typeName} (Used/Total)");
                            }
                        });

                        // Data rows
                        foreach (var emp in _data)
                        {
                            table.Cell().Element(DataCellStyle).Text(emp.EmployeeName);
                            table.Cell().Element(DataCellStyle).Text(emp.Department);

                            foreach (var typeName in _leaveTypeNames)
                            {
                                var lt = emp.LeaveTypes.FirstOrDefault(x => x.LeaveTypeName == typeName);
                                var value = lt != null ? $"{lt.Used}/{lt.Allocated}" : "-";
                                table.Cell().Element(DataCellStyle).Text(value);
                            }
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generated on ");
                        x.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
                    });
            });
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten2)
                .Padding(5)
                .DefaultTextStyle(x => x.SemiBold());
        }

        private static IContainer DataCellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }
    }
}