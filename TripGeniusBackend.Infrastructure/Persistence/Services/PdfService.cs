using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class PdfService : IPdfService
{
    private const string CurrencyCode = "EUR";

    public byte[] GenerateCostsPdf(Trip trip)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Verdana));

                // Header
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(trip.Title.ToUpper()).FontSize(24).ExtraBold().FontColor(Colors.Indigo.Medium);
                        col.Item().Text($"{trip.StartDate:dd MMM yyyy} - {trip.EndDate:dd MMM yyyy}")
                           .FontSize(12).FontColor(Colors.Grey.Medium);
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("COST REPORT").FontSize(20).Bold().FontColor(Colors.Indigo.Lighten2);
                        col.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    // Total calculation
                    var total = trip.Timelines
                        .SelectMany(t => t.Activities)
                        .Where(a => a.Cost.HasValue && a.Cost.Value > 0)
                        .Sum(a => a.Cost ?? 0);

                    // Summary Box
                    col.Item().PaddingBottom(20).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(innerCol =>
                        {
                            innerCol.Item().Text("TOTAL ESTIMATED BUDGET").FontSize(10).SemiBold().FontColor(Colors.Grey.Medium);
                            innerCol.Item().Text($"{total:N2} {CurrencyCode}").FontSize(20).ExtraBold().FontColor(Colors.Indigo.Medium);
                        });
                        
                        row.RelativeItem().AlignRight().Column(innerCol =>
                        {
                            var count = trip.Timelines.SelectMany(t => t.Activities).Count(a => a.Cost > 0);
                            innerCol.Item().Text("BILLABLE ACTIVITIES").FontSize(10).SemiBold().FontColor(Colors.Grey.Medium);
                            innerCol.Item().Text(count.ToString()).FontSize(20).ExtraBold();
                        });
                    });

                    foreach (var timeline in trip.Timelines.OrderBy(t => t.StartDay))
                    {
                        var activitiesWithCosts = timeline.Activities
                            .Where(a => a.Cost.HasValue && a.Cost.Value > 0)
                            .ToList();

                        if (!activitiesWithCosts.Any()) continue;

                        // Day Header
                        col.Item().PaddingTop(15).PaddingBottom(5).Text(
                            $"Day {timeline.StartDay}-{timeline.EndDay}: {timeline.StartingPoint} → {timeline.EndPoint}"
                        ).FontSize(14).Bold().FontColor(Colors.Indigo.Darken2);

                        // Activities Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(30); // #
                                cols.RelativeColumn(3);  // Name
                                cols.RelativeColumn(1);  // Type
                                cols.ConstantColumn(100); // Cost
                            });

                            // Table Header
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Activity");
                                header.Cell().Element(CellStyle).Text("Type");
                                header.Cell().Element(CellStyle).AlignRight().Text($"Cost ({CurrencyCode})");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold())
                                                    .PaddingVertical(5)
                                                    .BorderBottom(1)
                                                    .BorderColor(Colors.Black);
                                }
                            });

                            // Rows
                            int index = 1;
                            foreach (var activity in activitiesWithCosts)
                            {
                                table.Cell().Element(RowStyle).Text(index++.ToString());
                                table.Cell().Element(RowStyle).Text(activity.Name);
                                table.Cell().Element(RowStyle).Text(activity.Type.ToString());
                                table.Cell().Element(RowStyle).AlignRight().Text($"{activity.Cost:N2} {CurrencyCode}");

                                static IContainer RowStyle(IContainer container)
                                {
                                    return container.BorderBottom(1)
                                                    .BorderColor(Colors.Grey.Lighten4)
                                                    .PaddingVertical(5);
                                }
                            }

                            // Subtotal
                            var subtotal = activitiesWithCosts.Sum(a => a.Cost ?? 0);
                            table.Cell().ColumnSpan(3).AlignRight().PaddingVertical(10).Text("Daily Subtotal:").SemiBold();
                            table.Cell().AlignRight().PaddingVertical(10).Text($"{subtotal:N2} {CurrencyCode}").SemiBold().FontColor(Colors.Indigo.Medium);
                        });
                    }
                });

                // Footer
                page.Footer().AlignCenter().Column(footerCol =>
                {
                    footerCol.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                    footerCol.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Generated by TripGenius").FontSize(9).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ").FontSize(9);
                            x.CurrentPageNumber().FontSize(9);
                        });
                    });
                });
            });
        }).GeneratePdf();
    }
}