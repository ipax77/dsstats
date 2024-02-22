using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Extensions;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace dsstats.db8services;

public class FaqService(ReplayContext context, IMapper mapper, IMemoryCache memoryCache) : IFaqService, IDisposable
{
    private readonly SemaphoreSlim ss = new(1, 1);

    public async Task<int> GetCount(FaqRequest request, CancellationToken token = default)
    {
        var faqs = GetQueriably(request);

        return await faqs.CountAsync(token);
    }

    public async Task<List<FaqDto>> GetList(FaqRequest request, CancellationToken token = default)
    {
        var faqs = GetQueriably(request);
        faqs = Order(faqs, request);

        return await faqs
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);
    }

    private IQueryable<FaqDto> Order(IQueryable<FaqDto> faqs, FaqRequest request)
    {
        bool hasOrders = false;

        foreach (var order in request.Orders)
        {
            var property = typeof(FaqDto).GetProperty(order.Property);
            if (property is null)
            {
                continue;
            }

            hasOrders = true;
            if (order.Ascending)
            {
                faqs = faqs.AppendOrderBy(order.Property);
            }
            else
            {
                faqs = faqs.AppendOrderByDescending(order.Property);

            }
        }

        if (!hasOrders)
        {
            faqs = faqs.OrderBy(o => o.Level)
                .ThenByDescending(o => o.Upvotes)
                .ThenBy(o => o.Question);
        }

        return faqs;
    }

    private IQueryable<FaqDto> GetQueriably(FaqRequest request)
    {
        var faqs = context.Faqs
            .ProjectTo<FaqDto>(mapper.ConfigurationProvider);

        if (request.Level != FaqLevel.None)
        {
            faqs = faqs.Where(x => x.Level == request.Level);
        }

        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchStrings = request.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var searchString in searchStrings)
            {
                faqs = faqs.Where(x => x.Question.Contains(searchString));
            }
        }

        return faqs;
    }

    public async Task<bool> Upvote(int faqId, string clientIp)
    {
        var memKey = $"faqvote|{clientIp}|{faqId}";

        if (memoryCache.TryGetValue(memKey, out bool voted))
        {
            return false;
        }
        else
        {
            memoryCache.Set(memKey, true, TimeSpan.FromHours(12));
        }

        context.FaqVotes.Add(new() { FaqId = faqId });
        await context.SaveChangesAsync();

        await CollectUpvotes();
        return true;
    }

    public async Task<int> CreateFaq(FaqDto faqDto, string? name)
    {
        var faq = mapper.Map<Faq>(faqDto);
        faq.CreatedBy = name;
        context.Faqs.Add(faq);
        await context.SaveChangesAsync();
        return faq.FaqId;
    }

    public async Task<bool> UpdateFaq(FaqDto faqDto, string? name)
    {
        var faq = await context.Faqs
            .FirstOrDefaultAsync(f => f.FaqId == faqDto.FaqId);

        if (faq is null)
        {
            return false;
        }

        mapper.Map<FaqDto, Faq>(faqDto, faq);
        faq.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteFaq(int faqId)
    {
        var faq = await context.Faqs
            .FirstOrDefaultAsync(f => f.FaqId == faqId);

        if (faq is null)
        {
            return false;
        }

        context.Faqs.Remove(faq);
        await context.SaveChangesAsync();
        return true;
    }

    private async Task CollectUpvotes()
    {
        await ss.WaitAsync();
        try
        {
            var upvotes = await context.FaqVotes
                .ToListAsync();

            context.FaqVotes.RemoveRange(upvotes);
            await context.SaveChangesAsync();

            StringBuilder sb = new();
            foreach (var upvote in upvotes)
            {
                sb.AppendLine($"UPDATE {nameof(ReplayContext.Faqs)} SET {nameof(Faq.Upvotes)} = {nameof(Faq.Upvotes)} + 1 WHERE {nameof(Faq.FaqId)} = {upvote.FaqId};");
            }
            await context.Database.ExecuteSqlRawAsync(sb.ToString());
        }
        finally
        {
            ss.Release();
        }
    }

    public void Dispose()
    {
        ss.Dispose();
    }
}
