using QuestPDF.Fluent;
using QuestPDF.Helpers;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class PdfService : IPdfService
{
    public byte[] GenerateCostsPdf(Trip trip)
{
    return Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(11));

            // Header
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(trip.Title).FontSize(20).Bold();
                    col.Item().Text($"{trip.StartDate:dd MMM yyyy} → {trip.EndDate:dd MMM yyyy}")
                       .FontColor(Colors.Grey.Medium);
                });
                row.ConstantItem(100).AlignRight()
                   .Image("wwwroot/logo.png"); // logo opțional
            });

            page.Content().PaddingTop(20).Column(col =>
            {
                foreach (var timeline in trip.Timelines)
                {
                    var activitiesWithCosts = timeline.Activities
                        .Where(a => a.Cost.HasValue && a.Cost.Value > 0)
                        .ToList();

                    if (!activitiesWithCosts.Any()) continue;

                    // Titlu zi
                    col.Item().Background(Colors.Grey.Lighten3).Padding(8).Text(
                        $"Ziua {timeline.StartDay}-{timeline.EndDay}: {timeline.StartingPoint} → {timeline.EndPoint}"
                    ).Bold();

                    // Tabel activități
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3); // Nume
                            cols.RelativeColumn(1); // Tip
                            cols.ConstantColumn(80); // Cost
                        });

                        // Header tabel
                        table.Header(header =>
                        {
                            header.Cell().Text("Activitate").Bold();
                            header.Cell().Text("Tip").Bold();
                            header.Cell().AlignRight().Text("Cost (RON)").Bold();
                        });

                        // Rows
                        foreach (var activity in activitiesWithCosts)
                        {
                            table.Cell().PaddingVertical(4).Text(activity.Name);
                            table.Cell().PaddingVertical(4).Text(activity.Type.ToString())
                                 .FontColor(Colors.Grey.Medium);
                            table.Cell().PaddingVertical(4).AlignRight()
                                 .Text($"{activity.Cost:F2}");
                        }

                        // Subtotal per zi
                        var subtotal = activitiesWithCosts.Sum(a => a.Cost ?? 0);
                        table.Cell().ColumnSpan(2).AlignRight().PaddingTop(4)
                             .Text("Subtotal:").Bold();
                        table.Cell().AlignRight().PaddingTop(4)
                             .Text($"{subtotal:F2}").Bold();
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(0.5f)
                       .LineColor(Colors.Grey.Lighten2);
                }

                // Total general
                var total = trip.Timelines
                    .SelectMany(t => t.Activities)
                    .Where(a => a.Cost.HasValue && a.Cost.Value > 0)
                    .Sum(a => a.Cost ?? 0);

                col.Item().Background(Colors.Blue.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Text("TOTAL ESTIMAT").FontSize(14).Bold();
                    row.ConstantItem(100).AlignRight()
                       .Text($"{total:F2} RON").FontSize(14).Bold();
                });
            });

            page.Footer().AlignCenter()
                .Text($"Generat de TripGenius • {DateTime.Now:dd.MM.yyyy}")
                .FontColor(Colors.Grey.Medium).FontSize(9);
        });
    }).GeneratePdf();
}
}