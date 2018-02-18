// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AmiDate.cs" company="KriaSoft LLC">
//   Copyright © 2013 Konstantin Tarkus, KriaSoft LLC. See LICENSE.txt
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace AmiBroker.Plugin.Models
{
    using System;

    internal class AmiDate : IComparable<AmiDate>
    { 
        private readonly ulong _packedDate;

        public AmiDate(ulong date)
        {
            _packedDate = date;
        }

        public AmiDate(DateTime date, bool isEOD = false, bool isFuturePad = false)
            : this(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond, 0, isEOD: isEOD, isFuturePad: isFuturePad)
        {
        }

        public AmiDate(int year, int month, int day, bool isFuturePad = false)
            : this(year, month, day, 0, 0, 0, 0, 0, isEOD: true, isFuturePad: isFuturePad)
        {
        }

        public AmiDate(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, bool isEOD = false, bool isFuturePad = false)
        {
            if (isEOD)
            {
                // EOD markets
                hour = 31;
                minute = 63;
                second = 63;
                millisecond = 1023;
                microsecond = 1023;
            }

            _packedDate = (ulong)year << 52 | (ulong)month << 48 | (ulong)day << 43 | (ulong)hour << 38 |
                              (ulong)minute << 32 | (ulong)second << 26 | (ulong)millisecond << 16 |
                              (ulong)microsecond << 6 | Convert.ToUInt64(isFuturePad);
        }

        public ulong ToUInt64 => _packedDate;

        public int Year => (int)(_packedDate >> 52);

        public int Month => (int)(_packedDate >> 48) & 15;

        public int Day => (int)(_packedDate >> 43) & 31;

        public int Hour => (int)(_packedDate >> 38) & 31;

        public int Minute => (int)(_packedDate >> 32) & 63;

        public int Second => (int)(_packedDate >> 26) & 63;

        public int MilliSecond => (int)(_packedDate >> 16) & 1023;

        public int MicroSecond => (int)(_packedDate >> 6) & 1023;

        public bool IsFuturePad => ((int)_packedDate & 1) == 1;

        public static explicit operator AmiDate(ulong date) => new AmiDate(date);

        public static implicit operator ulong(AmiDate date) => date.ToUInt64;

        public override bool Equals(object obj) => obj is AmiDate date && Year == date.Year && Month == date.Month 
            && Day == date.Day && Hour == date.Hour && Minute == date.Minute && Second == date.Second 
            && MilliSecond == date.MilliSecond && MicroSecond == date.MicroSecond;

        public override int GetHashCode() => ToUInt64.GetHashCode();

        public int CompareTo(AmiDate other) => new DateTime(Year, Month, Day, Hour, Minute, Second, MilliSecond)
                .CompareTo(new DateTime(other.Year, other.Month, other.Day, other.Hour, other.Minute, other.Second, other.MilliSecond));

        public int GetDateAsInt => Year * 10_000 + Month * 100 + Day;
    }
}
