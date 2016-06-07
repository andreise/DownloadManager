using System;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace DownloadManager
{

    sealed class DownloadTask
    {
        public bool IsActive
        {
            get; internal set;
        }

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

    sealed class DownloadTaskComparer : IComparer<DownloadTask>
    {
        public int Compare(DownloadTask x, DownloadTask y)
        {
            if ((object)x == (object)y)
                return 0;

            if ((object)x == null)
                return -1;

            if ((object)y == null)
                return 1;

            return Comparer<int>.Default.Compare(x.StartTime, y.StartTime);
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
            List<DownloadTask> activeTasks = new List<DownloadTask>(this.Tasks.Count);
            int uncompletedTasks = this.Tasks.Count;

            int clock = orderedTask.First().StartTime;

            while (uncompletedTasks > 0)
            {
                activeTasks.Clear();
                foreach (DownloadTask task in orderedTask)
                    if (activeTasks.Count < this.ChannelBandwidth)
                    {
                        if (!task.IsComplete && task.StartTime <= clock)
                            activeTasks.Add(task);
                    }

                if (activeTasks.Count > 0)
                {
                    int bandwidthPerTask = this.ChannelBandwidth / activeTasks.Count;
                    int bandwidthRemainder = this.ChannelBandwidth % activeTasks.Count;
                    foreach (DownloadTask task in activeTasks)
                    {
                        task.DownloadedSize += bandwidthPerTask;
                        if (task.DownloadedSize > task.EstimatedSize)
                            task.DownloadedSize = task.EstimatedSize;
                        task.DownloadTime = clock + 1;
                        if (bandwidthRemainder > 0)
                        {
                            if (!task.IsComplete)
                                task.DownloadedSize++;
                            bandwidthRemainder--;
                        }
                        if (task.IsComplete)
                            uncompletedTasks--;
                    }
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

        static string[] SplitLine(string s)
        {
            return s.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        static Tuple<int, int> ReadValuePair()
        {
            string s = Console.ReadLine();
            string[] items = SplitLine(s);
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

            //Console.ReadLine();
        }
    }
}
