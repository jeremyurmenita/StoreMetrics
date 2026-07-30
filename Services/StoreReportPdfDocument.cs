using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StoreMetrics.ViewModels;

namespace StoreMetrics.Services
{
    public class StoreReportPdfDocument : IDocument
    {
        private readonly StoreVm _store;
        private readonly EvaluationVm _eval;
        private readonly byte[] _chartImage;

        public StoreReportPdfDocument(StoreVm store, EvaluationVm eval, byte[] chartImage)
        {
            _store = store;
            _eval = eval;
            _chartImage = chartImage;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text("Store Performance Report")
                    .SemiBold()
                    .FontSize(26)
                   .FontColor("#1565C0");   // Darker blue


                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Text($"Store: {_store.StoreName}")
                        .FontSize(16).SemiBold();

                    col.Item().Text(
                        $"Address: {_store.BuildingNumber} {_store.StreetName}, " +
                        $"{_store.Brgy}, {_store.City}, {_store.Province} {_store.PostalCode}"
                    ).FontSize(12);

                    col.Item().Text($"Date Evaluated: {_eval.EvaluationDate:MMMM dd, yyyy}");
                    col.Item().Text($"Average Rating: {_eval.AverageRating:F2}");
                    col.Item().Text($"Performance Rating: {_eval.PerformancePercent:F2}%");



                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#E0E0E0");  // Light grey

                    // CATEGORY RATINGS TABLE
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Category
                            columns.RelativeColumn(1); // Rating
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle)
                                .Text("Category").SemiBold();

                            header.Cell().Element(CellStyle)
                                .Text("Rating").SemiBold();
                        });

                        AddRow(table, "Cleanliness", _eval.Cleanliness);
                        AddRow(table, "Condition", _eval.Condition);
                        AddRow(table, "Customer Engagement", _eval.CustomerEngagement);
                        AddRow(table, "Personal Grooming", _eval.PersonalGrooming);
                        AddRow(table, "Accuracy", _eval.Accuracy);
                        AddRow(table, "Speed of Service", _eval.SpeedOfService);
                        AddRow(table, "Product Quality", _eval.ProductQuality);
                    });

                    col.Item().PaddingVertical(15)
                        .Text("Ratings per Category (Graph)")
                        .FontSize(14)
                        .SemiBold();

                    if (_chartImage != null)
                    {
                        col.Item().Image(_chartImage);
                    }
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Generated on ").FontSize(10);
                    txt.Span(DateTime.Now.ToString("MMMM dd, yyyy")).FontSize(10).SemiBold();
                });
            });
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container.Padding(4).Background(Colors.Grey.Lighten4);
        }

        private void AddRow(TableDescriptor table, string category, int rating)
        {
            table.Cell().Element(CellStyle)
                .Text(category);

            table.Cell().Element(CellStyle)
                .Text(rating.ToString());
        }
    }
}
