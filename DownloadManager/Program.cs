using System;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace DownloadManager
{

    sealed class DownloadTask
    {
        public int StartTime
        {
            get; private set;
        }

        public int EstimatedSize
        {
            get;
            private set;
        }

        public int DownloadedSize
        {
            get; internal set;
        }

        public bool IsComplete
        {
            get { return this.DownloadedSize >= this.EstimatedSize; }
        }

        public int DownloadTime
        {
            get; internal set;
        }

        public DownloadTask(int startTime, int estimatedSize)
        {
            if (startTime < 0)
                throw new ArgumentOutOfRangeException("startTime", startTime, "Start time must be equals to or greater than zero.");
            if (estimatedSize < 0)
                throw new ArgumentOutOfRangeException("estimatedSize", estimatedSize, "Size must be equals to or greater than zero.");

            this.StartTime = startTime;
            this.EstimatedSize = estimatedSize;
        }
    }

    sealed class Downloader
    {
        public int ChannelBandwidth { get; private set; }

        public ReadOnlyCollection<DownloadTask> Tasks { get; private set; }

        public Downloader(int channelBandwidth, IList<DownloadTask> tasks)
        {
            if (channelBandwidth < 1)
                throw new ArgumentOutOfRangeException("channelBandwidth", channelBandwidth, "Channel bandwidth must be greater than zero.");
            if ((object)tasks != null && tasks.Contains(null))
                throw new ArgumentException("Tasks must contains all not null values", "tasks");

            this.ChannelBandwidth = channelBandwidth;
            this.Tasks = new ReadOnlyCollection<DownloadTask>(tasks ?? new DownloadTask[0]);
        }

        public void DownloadAll()
        {
            if (this.Tasks.Count == 0)
                return;


            List<DownloadTask> orderedTask = this.Tasks.OrderBy(task => task.StartTime).ToList();

            HashSet<DownloadTask> taskQueue = new HashSet<DownloadTask>(orderedTask);

            int clock = orderedTask.First().StartTime;

            while (taskQueue.Count > 0)
            {
                List<DownloadTask> activeTasks = new List<DownloadTask>(taskQueue.Count);
                for (int i = 0; i < orderedTask.Count && activeTasks.Count < this.ChannelBandwidth; i++)
                {
                    if (!orderedTask[i].IsComplete && orderedTask[i].StartTime <= clock)
                        activeTasks.Add(orderedTask[i]);
                }

                if (activeTasks.Count > 0)
                {
                    int bandwidthPerTask = this.ChannelBandwidth / activeTasks.Count;
                    for (int i = 0; i < activeTasks.Count; i++)
                    {
                        activeTasks[i].DownloadedSize += bandwidthPerTask;
                        activeTasks[i].DownloadTime = clock;
                    }
                    //int processed = 0;
                    //for (int i = 0; i < activeTasks.Count; i++)
                    //{
                    //    if (!activeTasks[i].IsComplete)
                    //    {
                    //        activeTasks[i].DownloadedSize++;
                    //        if (++processed > bandwidthPerTask % activeTasks.Count)
                    //            break;
                    //    }
                    //}

                    foreach (DownloadTask task in activeTasks)
                        if (task.IsComplete)
                            taskQueue.Remove(task);
                }

                clock++;
            }
        }
    }

    static class Program
    {
        static int ParseNonnegativeInt32(string s)
        {
            return int.Parse(s, NumberStyles.Integer & ~NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);
        }

        static int ParsePositiveInt32(string s)
        {
            int value = ParseNonnegativeInt32(s);
            if (value == 0)
                throw new ArgumentException("Value must be greater than zero.", "s");
            return value;
        }

        static int ReadPositiveInt32()
        {
            return ParsePositiveInt32(Console.ReadLine());
        }

        static string[] SplitLine(string s, int maxCount)
        {
            string[] tempItems = s.Split(new char[] { '\u0020', '\u0009' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> items = new List<string>(Math.Min(tempItems.Length, maxCount));
            for (int i = 0; i < tempItems.Length && i < maxCount; i++)
                if (!string.IsNullOrWhiteSpace(tempItems[i]))
                    items.Add(tempItems[i]);
            return items.ToArray();
        }

        static Tuple<int, int> ReadValuePair()
        {
            string s = Console.ReadLine();
            string[] items = SplitLine(s, 2);
            if (items.Length < 2)
                throw new ArgumentException("Two items per line was expected.");
            return new Tuple<int, int>(ParsePositiveInt32(items[0]), ParsePositiveInt32(items[1]));
        }

        static IList<DownloadTask> ReadTaskList(int taskCount)
        {
            List<DownloadTask> list = new List<DownloadTask>(taskCount);
            for (int i = 0; i < taskCount; i++)
            {
                Tuple<int, int> buffer = ReadValuePair();
                list.Add(new DownloadTask(buffer.Item1, buffer.Item2));
            }
            return list;
        }

        static void Main(string[] args)
        {
            Tuple<int, int> buffer = ReadValuePair();
            int taskCount = buffer.Item1;
            int channelBandwidth = buffer.Item2;

            Downloader downloader = new Downloader(channelBandwidth, ReadTaskList(taskCount));
            downloader.DownloadAll();

            for (int i = 0; i < downloader.Tasks.Count; i++)
                Console.WriteLine(downloader.Tasks[i].DownloadTime);

            Console.ReadLine();
        }
    }
}
