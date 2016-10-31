using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernFileEncrypter.Helper
{
    public class ProgressTimeHelper
    {
        public DateTime StartTime { get; protected set; }
        public long Total { get; protected set; }

        public ProgressTimeHelper() { }

        public void Reset(long totalSize)
        {
            StartTime = DateTime.Now;
            Total = totalSize;
        }

        public double Tick(long processed)
        {
            DateTime now = DateTime.Now;
            processed = processed == 0 ? 1 : processed;
            var processedTime = (now - StartTime).TotalSeconds;
            var estimatedRestTime = (processedTime / processed) * (Total - processed);
            return estimatedRestTime > 0 ? estimatedRestTime : 0;
        }
    }
}
