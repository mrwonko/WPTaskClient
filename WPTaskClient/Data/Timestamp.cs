using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPTaskClient.Data
{
    public class Timestamp : IComparable<Timestamp>
    {
        private static readonly string dateFormat = "yyyyMMdd'T'HHmmss'Z'";
        private DateTime dateTime;

        public static Timestamp Now { get { return new Timestamp(DateTime.UtcNow); } }

        public Timestamp(DateTime utcDateTime)
        {
            this.dateTime = utcDateTime;
        }

        public Timestamp(string timestamp)
        {
            this.dateTime = DateTime.ParseExact(timestamp, dateFormat, System.Globalization.CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return this.dateTime.ToString(dateFormat);
        }

        public int CompareTo(Timestamp other)
        {
            return dateTime.CompareTo(other.dateTime);
        }
    }
}
