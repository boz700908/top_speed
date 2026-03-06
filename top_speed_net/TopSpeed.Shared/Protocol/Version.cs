using System;

namespace TopSpeed.Protocol
{
    public readonly struct ProtocolVer : IComparable<ProtocolVer>, IEquatable<ProtocolVer>
    {
        public ProtocolVer(ushort year, byte month, byte day, byte revision)
        {
            if (year < 2000 || year > 9999)
                throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2000 and 9999.");
            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
            if (day < 1 || day > 31)
                throw new ArgumentOutOfRangeException(nameof(day), "Day must be between 1 and 31.");
            if (revision < 1)
                throw new ArgumentOutOfRangeException(nameof(revision), "Revision must be at least 1.");

            Year = year;
            Month = month;
            Day = day;
            Revision = revision;
        }

        public ushort Year { get; }
        public byte Month { get; }
        public byte Day { get; }
        public byte Revision { get; }

        public int CompareTo(ProtocolVer other)
        {
            var yearCompare = Year.CompareTo(other.Year);
            if (yearCompare != 0)
                return yearCompare;

            var monthCompare = Month.CompareTo(other.Month);
            if (monthCompare != 0)
                return monthCompare;

            var dayCompare = Day.CompareTo(other.Day);
            if (dayCompare != 0)
                return dayCompare;

            return Revision.CompareTo(other.Revision);
        }

        public bool Equals(ProtocolVer other)
        {
            return Year == other.Year && Month == other.Month && Day == other.Day && Revision == other.Revision;
        }

        public override bool Equals(object? obj)
        {
            return obj is ProtocolVer other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Year;
                hash = (hash * 397) ^ Month;
                hash = (hash * 397) ^ Day;
                hash = (hash * 397) ^ Revision;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Year}.{Month}.{Day} (r{Revision})";
        }

        public string ToMachineString()
        {
            return $"{Year}.{Month}.{Day}.{Revision}";
        }

        public static bool operator <(ProtocolVer left, ProtocolVer right) => left.CompareTo(right) < 0;
        public static bool operator >(ProtocolVer left, ProtocolVer right) => left.CompareTo(right) > 0;
        public static bool operator <=(ProtocolVer left, ProtocolVer right) => left.CompareTo(right) <= 0;
        public static bool operator >=(ProtocolVer left, ProtocolVer right) => left.CompareTo(right) >= 0;
        public static bool operator ==(ProtocolVer left, ProtocolVer right) => left.Equals(right);
        public static bool operator !=(ProtocolVer left, ProtocolVer right) => !left.Equals(right);
    }

    public readonly struct ProtocolRange : IEquatable<ProtocolRange>
    {
        public ProtocolRange(ProtocolVer minSupported, ProtocolVer maxSupported)
        {
            if (maxSupported < minSupported)
                throw new ArgumentException("Maximum supported version cannot be lower than minimum supported version.");

            MinSupported = minSupported;
            MaxSupported = maxSupported;
        }

        public ProtocolVer MinSupported { get; }
        public ProtocolVer MaxSupported { get; }

        public bool Contains(ProtocolVer version)
        {
            return version >= MinSupported && version <= MaxSupported;
        }

        public bool Equals(ProtocolRange other)
        {
            return MinSupported == other.MinSupported && MaxSupported == other.MaxSupported;
        }

        public override bool Equals(object? obj)
        {
            return obj is ProtocolRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (MinSupported.GetHashCode() * 397) ^ MaxSupported.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"{MinSupported} to {MaxSupported}";
        }
    }
}
