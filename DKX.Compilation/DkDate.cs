using System;

namespace DKX.Compilation
{
	public struct DkDate
	{
		private ushort _value;

		public ushort Value { get => _value; set => _value = value; }

		private const int FOURYEARS = 1461; // # days in full 4-year cycle
		private static readonly int[] days = new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };	/* days each month, non leapyear */
		private static readonly int[] ydays = new int[] { 0, 366, 731, 1096, 1461 }; // cumulative days, 4 years
		private static readonly int[] _mdays = new int[] { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };	// cumulative days, non leapyear
		private const int max_day = 31;
		private const int max_month = 12;
		private const int max_year = 179;
		private const int DFIX = 1; // to fix day-of-week 

		public static readonly DkDate Empty = new DkDate(0);
		public static readonly DkDate MinValue = new DkDate(0);
		public static readonly DkDate MaxValue = new DkDate(0xffff);

		public DkDate(ushort value)
		{
			_value = value;
		}

		public DkDate(int year, int month, int day)
		{
			_value = MakeDate(year, month, day).Value;
		}

		public DkDate(DateTime dt)
		{
			_value = MakeDate(dt.Year, dt.Month, dt.Day).Value;
		}

		public static implicit operator DateTime(DkDate dt) => dt.ToDateTime();
		public static implicit operator DkDate(DateTime dt) => new DkDate(dt);

		public int Year => Split().Year + 1900;
		public int Month => Split().Month + 1;
		public int Day => Split().DayOfMonth;

		private DkDate(DkDateParts tp)
		{
			_value = FromDateParts(tp);
		}

		public static DkDate MakeDate(int year, int month, int day)
		{
			if ((year < 1900 || year > 1900 + max_year) ||     // max year is 2079 in current DK definition
				(month < 1 || month > max_month) ||
				(day < 1 || day > max_day))
			{
				return DkDate.Empty;
			}

			var dp = new DkDateParts
			{
				Year = year - 1900,
				Month = month - 1,
				DayOfMonth = day
			};

			return new DkDate(DkDate.FromDateParts(dp));
		}

		private static ushort FromDateParts(DkDateParts tp)
		{
			int yr = tp.Year % 4;
			uint di = (uint)(FOURYEARS * (tp.Year / 4) + ydays[yr] + _mdays[tp.Month] + tp.DayOfMonth - 1);
			if (yr == 0 && tp.Month >= 2) ++di;     /* adjust for leapyr */
			if (tp.Year > 0 || tp.Month >= 2) --di; /* undo 1900 leapyr */

			if (di > 65535) di = 0;
			return (ushort)di;
		}

		private DkDateParts Split()
		{
			int days = _value;
			int yr, mm, yy, dr, dy;

			if (days >= 59) days++;					// pretend 1900 lpyr

			yy = 4 * (days / FOURYEARS);			// full 4-yr periods
			dr = days % FOURYEARS;                  // remaining days

			for (yr = 0; yr < 4; ++yr)
			{
				if (dr < ydays[yr + 1]) break;
			}
			yy += yr;								// delta years
			dr -= ydays[yr];
			dy = dr;								// day-of-year

			if (yr == 0 && dr > 59) --dr;			// leapyear fixup
			for (mm = 0; mm < max_month; ++mm)
			{
				if (dr < _mdays[mm + 1]) break;
			}
			dr -= _mdays[mm];
			if (yr == 0 && dy == 59)				// leapyear fixup
			{
				--mm; dr = 28;
			}
			if (days >= 60)
			{										// undo 1900 lpyr
				--days;
				if (yy == 0) --dy;
			}

			return new DkDateParts
			{
				DayOfMonth = dr + 1,
				Month = mm,
				Year = yy,
				WeekDay = (days + DFIX) % 7,
				YearDay = dy
			};
		}

		public DkDate IncDate(int numUnits, DkDateUnit units, int day)
		{
			int idt = _value;

			switch (units)
			{
				case DkDateUnit.Days:
					return new DkDate((ushort)(_value + numUnits));

				case DkDateUnit.Weeks:
					return new DkDate((ushort)(_value + numUnits * 7));

				case DkDateUnit.Semimonths:
					{
						int sign = numUnits;
						if (numUnits < 0) numUnits = -numUnits;
						int hm = numUnits % 2;                      // half-month
						numUnits /= 2;                              // make full months
						if (sign < 0)                               // make positive
						{
							numUnits = -numUnits;
							if (hm == 1) --numUnits;
						}

						var dp = Split();
						int dd = dp.DayOfMonth;
						int dl = LastDayOfMonth(dp.Month, dp.Year);           // last day of month
						bool topfl = (dd >= 15 && dd < dl);         // top-half flag

						if (hm == 1)                                // half-month
						{
							if (topfl)
							{
								dp.DayOfMonth = dl;                 // set last day
							}
							else
							{
								if (dd == dl) dp = dp.FullMonthIncrement(1, 0);
								dp.DayOfMonth = 15;					// set 15th
							}
						}
						else                                        // no half-month
						{
							if (topfl)          // = top half
							{
								dp.DayOfMonth = 15;   // set 15th
							}
							else if (dd != dl)      // = bottom half
							{
								dp = dp.FullMonthIncrement(-1, max_day);    // set last prvius
							}
						}

						dp = dp.FullMonthIncrement(numUnits, 0);        /* increment month */
						if (dp.DayOfMonth != 15)          /* force last day */
							dp.DayOfMonth = LastDayOfMonth(dp.Month, dp.Year);
						return new DkDate(dp);
					}

				case DkDateUnit.Months:
					return new DkDate(Split().FullMonthIncrement(numUnits, day));

				case DkDateUnit.Years:
					{
						var dp = Split();
						dp.Year += numUnits;
						if (day != 0) dp.DayOfMonth = day;
						int dl = LastDayOfMonth(dp.Month, dp.Year);
						if (dp.DayOfMonth > dl) dp.DayOfMonth = dl;
						return new DkDate(dp);
					}

				default:
					return DkDate.Empty;
			}
		}

		public static DkDate Today => new DkDate(DateTime.Now);

		private static int LastDayOfMonth(int mm, int yy) => days[mm] + ((mm == 1 && IsLeapYear(yy)) ? 1 : 0);
		private static bool IsLeapYear(int year) => year % 4 == 0 && year != 0;

		private static string MonthAbbrev(int mon)
		{
			switch (mon)
			{
				case 0:
					return "Jan";
				case 1:
					return "Feb";
				case 2:
					return "Mar";
				case 3:
					return "Apr";
				case 4:
					return "May";
				case 5:
					return "Jun";
				case 6:
					return "Jul";
				case 7:
					return "Aug";
				case 8:
					return "Sep";
				case 9:
					return "Oct";
				case 10:
					return "Nov";
				case 11:
					return "Dec";
				default:
					return string.Empty;
			}
		}

		public override string ToString()
		{
			var dp = Split();
			return $"{dp.DayOfMonth:00}{MonthAbbrev(dp.Month)}{dp.Year + 1900}";
		}

		public string ToApiString() => $"{Year:0000}-{Month:00}-{Day:00}";

		public DateTime ToDateTime() => new DateTime(Year, Month, Day);

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != typeof(DkDate)) return false;
			var dt = (DkDate)obj;
			return _value == dt._value;
		}

		public override int GetHashCode() => _value.GetHashCode();
		public static bool operator ==(DkDate a, DkDate b) => a._value == b._value;
		public static bool operator !=(DkDate a, DkDate b) => a._value != b._value;
		public static bool operator <(DkDate a, DkDate b) => a._value < b._value;
		public static bool operator <=(DkDate a, DkDate b) => a._value <= b._value;
		public static bool operator >(DkDate a, DkDate b) => a._value > b._value;
		public static bool operator >=(DkDate a, DkDate b) => a._value >= b._value;
		public static DkDate operator +(DkDate dt, int numDays) => new DkDate((ushort)(dt.Value + numDays));
		public static DkDate operator -(DkDate dt, int numDays) => new DkDate((ushort)(dt.Value - numDays));

		private struct DkDateParts
		{
			public int DayOfMonth { get; set; }
			public int Month { get; set; }
			public int Year { get; set; }
			public int WeekDay { get; set; }
			public int YearDay { get; set; }

			public DkDateParts(DkDateParts copy)
			{
				DayOfMonth = copy.DayOfMonth;
				Month = copy.Month;
				Year = copy.Year;
				WeekDay = copy.WeekDay;
				YearDay = copy.YearDay;
			}

			private const int max_month = 12;

			public DkDateParts FullMonthIncrement(int incr, int dval)
			{
				int mm;
				int dl, sign;
				var dp = new DkDateParts(this);

				mm = dp.Month + incr;             /* increment month */
				sign = mm;                  /* '/' & '%' undefined*/
				if (mm < 0) mm = -mm;                   /* for negative ops */
				dp.Month = mm % max_month;
				mm /= max_month;
				if (sign < 0)
				{
					mm = -mm;
					if ((dp.Month = -dp.Month) != 0)
					{   /* make month positiv */
						dp.Month += max_month;
						--mm;
					}
				}
				dp.Year += mm;              /* fix year */

				if (dval != 0)                  /* force day */
					dp.DayOfMonth = dval;
				dl = DkDate.LastDayOfMonth(dp.Month, dp.Year);     /* last day of month */
				if (dp.DayOfMonth > dl)               /* make sure in range */
					dp.DayOfMonth = dl;

				return dp;
			}
		}
	}

	public enum DkDateUnit
	{
		Days,
		Weeks,
		Semimonths,
		Months,
		Years
	}
}
