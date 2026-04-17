using Infrastructure.Db.App;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Read;

public sealed class ReceiverReadRepository : IReceiverReadRepository
{
    private readonly AppDbContext _dbContext;

    public ReceiverReadRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedList<DomainListItem>> GetDomainsAsync(DomainListQuery query, CancellationToken ct)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var source = _dbContext.Domains.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            source = source.Where(d =>
                d.NameOriginal.Contains(query.Search) ||
                d.NameNormalized.Contains(query.Search) ||
                d.NamePunycode.Contains(query.Search));
        }

        if (!string.IsNullOrWhiteSpace(query.Tld))
        {
            source = source.Where(d => d.Tld == query.Tld);
        }

        var projection = source.Select(d => new DomainListItem
        {
            Id = d.Id,
            NameOriginal = d.NameOriginal,
            NameNormalized = d.NameNormalized,
            NamePunycode = d.NamePunycode,
            Tld = d.Tld,
            Sld = d.Sld,
            AuctionsCount = d.Auctions.Count
        });

        projection = query.Sort switch
        {
            "name" or "+name" => projection.OrderBy(d => d.NameNormalized),
            "-name" => projection.OrderByDescending(d => d.NameNormalized),
            "+createdAt" => projection.OrderBy(d => d.Id),
            _ => projection.OrderByDescending(d => d.Id)
        };

        return PagedList<DomainListItem>.ToPagedListAsync(projection, pageNumber, pageSize);
    }

    public Task<DomainDetailsItem?> GetDomainByIdAsync(long domainId, CancellationToken ct)
    {
        return _dbContext.Domains.AsNoTracking()
            .Where(d => d.Id == domainId)
            .Select(d => new DomainDetailsItem
            {
                Id = d.Id,
                NameOriginal = d.NameOriginal,
                NameNormalized = d.NameNormalized,
                NamePunycode = d.NamePunycode,
                Tld = d.Tld,
                Sld = d.Sld,
                AuctionsCount = d.Auctions.Count,
                CreatedAt = d.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task<PagedList<AuctionListItem>> GetAuctionsAsync(AuctionListQuery query, CancellationToken ct)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var source = _dbContext.Auctions.AsNoTracking().AsQueryable();
        if (query.DomainId.HasValue)
        {
            source = source.Where(a => a.DomainId == query.DomainId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.DomainName))
        {
            source = source.Where(a =>
                a.Domain.NameNormalized.Contains(query.DomainName) ||
                a.Domain.NameOriginal.Contains(query.DomainName));
        }

        if (!string.IsNullOrWhiteSpace(query.SourceCode))
        {
            source = source.Where(a => a.SourcePlatform.Code == query.SourceCode);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(a => a.Status == query.Status);
        }

        if (query.TerminalOnly == true)
        {
            source = source.Where(a => a.TerminalStatus != null);
        }

        var projection = source.Select(a => new AuctionListItem
        {
            Id = a.Id,
            DomainId = a.DomainId,
            DomainName = a.Domain.NameNormalized,
            SourceCode = a.SourcePlatform.Code,
            SourceName = a.SourcePlatform.Name,
            ExternalAuctionId = a.ExternalAuctionId,
            Status = a.Status,
            CurrentPrice = a.CurrentPrice,
            FinalPrice = a.FinalPrice,
            CurrencyCode = a.CurrencyCode,
            EndAt = a.EndAt,
            TerminalStatus = a.TerminalStatus,
            FinalizedAt = a.FinalizedAt,
            LastObservedAt = a.LastObservedAt
        });

        projection = query.Sort switch
        {
            "price" or "+price" => projection.OrderBy(a => a.CurrentPrice),
            "-price" => projection.OrderByDescending(a => a.CurrentPrice),
            "endAt" or "+endAt" => projection.OrderBy(a => a.EndAt),
            "-endAt" => projection.OrderByDescending(a => a.EndAt),
            "+createdAt" => projection.OrderBy(a => a.Id),
            _ => projection.OrderByDescending(a => a.LastObservedAt).ThenByDescending(a => a.Id)
        };

        return PagedList<AuctionListItem>.ToPagedListAsync(projection, pageNumber, pageSize);
    }

    public Task<AuctionDetailsItem?> GetAuctionByIdAsync(long auctionId, CancellationToken ct)
    {
        return _dbContext.Auctions.AsNoTracking()
            .Where(a => a.Id == auctionId)
            .Select(a => new AuctionDetailsItem
            {
                Id = a.Id,
                DomainId = a.DomainId,
                DomainName = a.Domain.NameNormalized,
                SourceCode = a.SourcePlatform.Code,
                SourceName = a.SourcePlatform.Name,
                ExternalAuctionId = a.ExternalAuctionId,
                Status = a.Status,
                CurrentPrice = a.CurrentPrice,
                FinalPrice = a.FinalPrice,
                CurrencyCode = a.CurrencyCode,
                EndAt = a.EndAt,
                TerminalStatus = a.TerminalStatus,
                FinalizedAt = a.FinalizedAt,
                LastObservedAt = a.LastObservedAt,
                StartAt = a.StartAt,
                ExtendedEndAt = a.ExtendedEndAt,
                IsExtended = a.IsExtended,
                LotUrl = a.LotUrl
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task<PagedList<AuctionObservationItem>> GetAuctionObservationsAsync(long auctionId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var source = _dbContext.AuctionObservations.AsNoTracking()
            .Where(o => o.AuctionId == auctionId)
            .Select(o => new AuctionObservationItem
            {
                Id = o.Id,
                AuctionId = o.AuctionId,
                MessageId = o.MessageId,
                SchemaVersion = o.SchemaVersion,
                ParserCode = o.ParserCode,
                ParserVersion = o.ParserVersion,
                ObservedAt = o.ObservedAt,
                AuctionStatusRaw = o.AuctionStatusRaw,
                AuctionStatusNormalized = o.AuctionStatusNormalized,
                CurrentPrice = o.CurrentPrice,
                FinalPrice = o.FinalPrice,
                CurrencyCode = o.CurrencyCode,
                IsOutOfOrder = o.IsOutOfOrder
            })
            .OrderByDescending(o => o.ObservedAt)
            .ThenByDescending(o => o.Id);

        return PagedList<AuctionObservationItem>.ToPagedListAsync(source, Math.Max(pageNumber, 1), Math.Clamp(pageSize, 1, 200));
    }

    public Task<PagedList<AuctionStatusHistoryItem>> GetAuctionStatusHistoryAsync(long auctionId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var source = _dbContext.AuctionStatusHistory.AsNoTracking()
            .Where(s => s.AuctionId == auctionId)
            .Select(s => new AuctionStatusHistoryItem
            {
                Id = s.Id,
                AuctionId = s.AuctionId,
                PreviousStatus = s.PreviousStatus,
                NewStatus = s.NewStatus,
                IsTerminal = s.IsTerminal,
                ObservedAt = s.ObservedAt,
                Source = s.Source
            })
            .OrderByDescending(s => s.ObservedAt)
            .ThenByDescending(s => s.Id);

        return PagedList<AuctionStatusHistoryItem>.ToPagedListAsync(source, Math.Max(pageNumber, 1), Math.Clamp(pageSize, 1, 200));
    }

    public Task<PagedList<AuctionResultItem>> GetAuctionResultsAsync(AuctionResultsQuery query, CancellationToken ct)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var source = _dbContext.Auctions.AsNoTracking().Where(a => a.TerminalStatus != null).AsQueryable();
        if (query.DomainId.HasValue)
        {
            source = source.Where(a => a.DomainId == query.DomainId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SourceCode))
        {
            source = source.Where(a => a.SourcePlatform.Code == query.SourceCode);
        }

        if (!string.IsNullOrWhiteSpace(query.TerminalStatus))
        {
            source = source.Where(a => a.TerminalStatus == query.TerminalStatus);
        }

        var projection = source.Select(a => new AuctionResultItem
        {
            AuctionId = a.Id,
            DomainId = a.DomainId,
            DomainName = a.Domain.NameNormalized,
            SourceCode = a.SourcePlatform.Code,
            ExternalAuctionId = a.ExternalAuctionId,
            TerminalStatus = a.TerminalStatus!,
            FinalPrice = a.FinalPrice,
            CurrencyCode = a.CurrencyCode,
            FinalizedAt = a.FinalizedAt
        });

        projection = query.Sort switch
        {
            "finalPrice" or "+finalPrice" => projection.OrderBy(r => r.FinalPrice),
            "-finalPrice" => projection.OrderByDescending(r => r.FinalPrice),
            "+finalizedAt" => projection.OrderBy(r => r.FinalizedAt),
            _ => projection.OrderByDescending(r => r.FinalizedAt).ThenByDescending(r => r.AuctionId)
        };

        return PagedList<AuctionResultItem>.ToPagedListAsync(projection, pageNumber, pageSize);
    }
}
