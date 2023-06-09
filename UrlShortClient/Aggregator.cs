using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShortClient
{
    public enum Kind
    {
        Read,
        Write,
        Delete
    }

    public record Entry(int ClientId, Kind Kind, TimeSpan RequestDuration, bool Success)
    {
    }

    internal class Aggregator
    {
        public ConcurrentStack<Entry> ReadEntries { get; set; } = new ConcurrentStack<Entry>();
        public ConcurrentStack<Entry> WriteEntries { get; set; } = new ConcurrentStack<Entry>();

        public void AddEntry(Entry entry)
        {
            if (entry.Kind == Kind.Read)
            {
                ReadEntries.Push(entry);
            }
            else
            {
                WriteEntries.Push(entry);
            }
        }

        public List<TimeSpan> SummarizeReadResponses()
        {
            var list = ReadEntries
                .Where(x => x.Success && x.Kind == Kind.Read)
                .Select(x => x.RequestDuration)
                .ToList();

            list.Sort();

            return list;
        }

        public List<TimeSpan> SummarizeWriteResponses()
        {
            var list = ReadEntries
                .Where(x => x.Success && x.Kind == Kind.Write)
                .Select(x => x.RequestDuration)
                .ToList();

            list.Sort();

            return list;
        }

        public static TimeSpan Quantile(IList<TimeSpan> times, double quantile)
        {
            var q = (int)Math.Floor(times.Count * quantile) - 1;

            if (q < 0)
            {
                q = 0;
            }

            return times[q];
        }
    }
}
