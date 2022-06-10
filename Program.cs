namespace dotnet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Program
    {
        private const int MillisecondsPerMinute = 60000;

        private static readonly Random Random = new Random();

        public static void Main(string[] args)
        {
            const int requestCpuTime = 5;
            const int maxConcurrentThreads = 16;
            const int maxRPMs = MillisecondsPerMinute * maxConcurrentThreads / requestCpuTime;

            var rpms = new List<int>();
            //rpms.Add(6500 /* 1 instance normal load, WE */);
            //rpms.Add(9000 /* 1 instance normal load, 1 DC */);
            //rpms.Add(12500 /* 50K RPM, WE */);
            //rpms.Add(18750 /* 75K RPM, 1 DC */);
            //rpms.Add(21000 /* 84K RPM, 1 DC */);
            //rpms.Add(19500 /* 1 instance 3 x normal, WE */);
            //rpms.Add(27000 /* 1 instance 3 x normal, 1 DC */);
            //rpms.Add(35000 /* 1 instance 3 x normal, 1 DC */);
            //rpms.Add(maxRPMs);
            CreateIncrementRpms(ref rpms, rpmIncrement: 1000, maxRPMs: maxRPMs);
            rpms = rpms.Distinct().OrderBy(i => i).ToList();

            Console.WriteLine($"Request CPU time = {requestCpuTime}");
            Console.WriteLine($"Max Concurrent threads = {maxConcurrentThreads}");
            Console.WriteLine();
            Console.WriteLine("  RPMs, Required CPU time, CPU usage, Max simultaneous requests, Max queued requests, Average queue time, Max queue time");

            var requestTimestamps = new List<int>();
            var cpuUsage = 0;
            var currentRpms = 0;
            while (rpms.Count > 0)
            {
                var lastRpms = currentRpms;
                currentRpms = rpms.Min();
                rpms.Remove(currentRpms);

                AddRequests(ref requestTimestamps, currentRpms - lastRpms);

                requestTimestamps.Sort();

                var maxSimultaneousRequests = 0;
                var queueTimes = new List<double>();
                for (var i = 1; i < requestTimestamps.Count; i++)
                {
                    var queueTime = (double)0;

                    var simultaneousRequestsCount = CalculateSimultaneousRequests(ref requestTimestamps, requestCpuTime, i);

                    if (simultaneousRequestsCount > 1)
                    {
                        if (simultaneousRequestsCount > maxSimultaneousRequests)
                        {
                            maxSimultaneousRequests = simultaneousRequestsCount;
                        }

                        if (simultaneousRequestsCount > maxConcurrentThreads)
                        {
                            queueTime = ((simultaneousRequestsCount - 1) * requestCpuTime) / (double)maxConcurrentThreads;
                        }
                    }

                    queueTimes.Add(queueTime);
                }

                var requiredCpuTime = requestTimestamps.Count * requestCpuTime;
                cpuUsage = (int)Math.Round((requiredCpuTime * 100) / (double)(MillisecondsPerMinute * maxConcurrentThreads), 2);

                Console.WriteLine(
                        $"{requestTimestamps.Count, 6}"
                    + $", {requiredCpuTime, 17}"
                    + $", {cpuUsage, 9}"
                    + $", {maxSimultaneousRequests, 25}"
                    + $", {Math.Max(0, maxSimultaneousRequests - maxConcurrentThreads), 19}"
                    + $", {Math.Round(queueTimes.Average(), 2).ToString("#0.00"), 18}"
                    + $", {Math.Round(queueTimes.Max(), 2).ToString("#0.00"), 14}");
            }
        }

        private static void CreateIncrementRpms(ref List<int> rpms, int rpmIncrement, int maxRPMs)
        {
            for (var i = rpmIncrement; i < maxRPMs; i+=rpmIncrement)
            {
                rpms.Add(i);
            }

            if (!rpms.Contains(maxRPMs))
            {
                rpms.Add(maxRPMs);
            }
        }

        private static void AddRequests(ref List<int> requestTimestamps, int numberOfRequests)
        {
            for (var i = 0; i < numberOfRequests; i++)
            {
                requestTimestamps.Add(Random.Next(MillisecondsPerMinute));
            }
        }

        private static int CalculateSimultaneousRequests(ref List<int> requestTimestamps, int requestCpuTime, int currentIndex)
        {
            var simultaneousRequestsCount = 1;

            for (var i = currentIndex - 1; i >= 0; i--)
            {
                if (requestTimestamps[currentIndex] >= (requestTimestamps[i] + requestCpuTime))
                {
                    break;
                }

                simultaneousRequestsCount++;
            }

            return simultaneousRequestsCount;
        }
    }
}
