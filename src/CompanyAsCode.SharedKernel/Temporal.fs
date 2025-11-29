namespace CompanyAsCode.SharedKernel

open System

/// Temporal value objects for dates, periods, and deadlines
module Temporal =

    // ============================================
    // Date (日付)
    // ============================================

    /// Immutable date value object (date only, no time)
    [<Struct>]
    type Date = private Date of DateTime

    module Date =

        let create (year: int) (month: int) (day: int) : Result<Date, string> =
            try
                Ok (Date (DateTime(year, month, day)))
            with
            | :? ArgumentOutOfRangeException ->
                Error $"Invalid date: {year}-{month}-{day}"

        let fromDateTime (dt: DateTime) : Date =
            Date (dt.Date)

        let today () : Date =
            Date DateTime.Today

        let value (Date d) = d

        let year (Date d) = d.Year
        let month (Date d) = d.Month
        let day (Date d) = d.Day

        let addDays (days: int) (Date d) : Date =
            Date (d.AddDays(float days))

        let addMonths (months: int) (Date d) : Date =
            Date (d.AddMonths(months))

        let addYears (years: int) (Date d) : Date =
            Date (d.AddYears(years))

        let daysBetween (Date d1) (Date d2) : int =
            (d2 - d1).Days

        let isBefore (Date d1) (Date d2) = d1 < d2
        let isAfter (Date d1) (Date d2) = d1 > d2
        let isOnOrBefore (Date d1) (Date d2) = d1 <= d2
        let isOnOrAfter (Date d1) (Date d2) = d1 >= d2

        let format (Date d) : string =
            d.ToString("yyyy-MM-dd")

        let formatJapanese (Date d) : string =
            d.ToString("yyyy年M月d日")

        let toDateTimeOffset (Date d) : DateTimeOffset =
            DateTimeOffset(d.ToUniversalTime(), TimeSpan.Zero)

    // ============================================
    // Fiscal Year End (決算期)
    // ============================================

    /// Month and day of fiscal year end
    type FiscalYearEnd = private {
        _month: int
        _day: int
    }

    module FiscalYearEnd =

        /// Default Japanese fiscal year end (March 31)
        let march31 : FiscalYearEnd = { _month = 3; _day = 31 }

        /// December 31 (calendar year)
        let december31 : FiscalYearEnd = { _month = 12; _day = 31 }

        let create (month: int) (day: int) : Result<FiscalYearEnd, string> =
            if month < 1 || month > 12 then
                Error $"Invalid month: {month}"
            else
                // Validate day for month
                let daysInMonth = DateTime.DaysInMonth(2000, month) // Use leap year
                if day < 1 || day > daysInMonth then
                    Error $"Invalid day {day} for month {month}"
                else
                    Ok { _month = month; _day = day }

        let month (fye: FiscalYearEnd) = fye._month
        let day (fye: FiscalYearEnd) = fye._day

        /// Get actual date for a given calendar year
        let toDate (calendarYear: int) (fye: FiscalYearEnd) : Date =
            // Handle leap year for Feb 29
            let actualDay =
                let maxDay = DateTime.DaysInMonth(calendarYear, fye._month)
                min fye._day maxDay
            Date.create calendarYear fye._month actualDay
            |> Result.defaultValue (Date.fromDateTime DateTime.Today)

        let format (fye: FiscalYearEnd) : string =
            $"{fye._month}月{fye._day}日"

    // ============================================
    // Fiscal Year (事業年度)
    // ============================================

    /// Fiscal year period
    type FiscalYear = {
        StartDate: Date
        EndDate: Date
        YearNumber: int  // e.g., 2024 for FY2024
    }

    module FiscalYear =

        /// Create fiscal year from end date and year number
        let create (yearEnd: FiscalYearEnd) (yearNumber: int) : FiscalYear =
            let endDate = FiscalYearEnd.toDate yearNumber yearEnd
            let startDate = endDate |> Date.addDays 1 |> Date.addYears -1
            {
                StartDate = startDate
                EndDate = endDate
                YearNumber = yearNumber
            }

        /// Create Japanese standard fiscal year (April 1 - March 31)
        let japaneseStandard (yearNumber: int) : FiscalYear =
            create FiscalYearEnd.march31 yearNumber

        /// Check if date is within fiscal year
        let contains (date: Date) (fy: FiscalYear) : bool =
            Date.isOnOrAfter date fy.StartDate && Date.isOnOrBefore date fy.EndDate

        /// Get fiscal year for a given date
        let forDate (yearEnd: FiscalYearEnd) (date: Date) : FiscalYear =
            let year = Date.year date
            let endDateThisYear = FiscalYearEnd.toDate year yearEnd

            if Date.isOnOrBefore date endDateThisYear then
                create yearEnd year
            else
                create yearEnd (year + 1)

        let format (fy: FiscalYear) : string =
            $"FY{fy.YearNumber}"

        let formatRange (fy: FiscalYear) : string =
            $"{Date.format fy.StartDate} - {Date.format fy.EndDate}"

    // ============================================
    // Date Range (期間)
    // ============================================

    /// Date range value object
    type DateRange = private {
        _start: Date
        _end: Date
    }

    module DateRange =

        let create (startDate: Date) (endDate: Date) : Result<DateRange, string> =
            if Date.isAfter startDate endDate then
                Error "Start date must be on or before end date"
            else
                Ok { _start = startDate; _end = endDate }

        let startDate (range: DateRange) = range._start
        let endDate (range: DateRange) = range._end

        let contains (date: Date) (range: DateRange) : bool =
            Date.isOnOrAfter date range._start && Date.isOnOrBefore date range._end

        let overlaps (r1: DateRange) (r2: DateRange) : bool =
            Date.isOnOrBefore r1._start r2._end && Date.isOnOrAfter r1._end r2._start

        let durationInDays (range: DateRange) : int =
            Date.daysBetween range._start range._end + 1

        let format (range: DateRange) : string =
            $"{Date.format range._start} - {Date.format range._end}"

    // ============================================
    // Term Period (任期)
    // ============================================

    /// Term period for directors, auditors, etc.
    type TermPeriod = {
        StartDate: Date
        EndDate: Date
        MaxYears: int option  // Legal maximum term
    }

    module TermPeriod =

        /// Create term with specified duration
        let create (startDate: Date) (years: int) (maxYears: int option) : Result<TermPeriod, string> =
            match maxYears with
            | Some max when years > max ->
                Error $"Term cannot exceed {max} years"
            | _ ->
                let endDate = startDate |> Date.addYears years |> Date.addDays -1
                Ok {
                    StartDate = startDate
                    EndDate = endDate
                    MaxYears = maxYears
                }

        /// Create director term (max 2 years per Companies Act)
        let directorTerm (startDate: Date) (years: int) : Result<TermPeriod, string> =
            create startDate years (Some 2)

        /// Create auditor term (max 4 years per Companies Act)
        let auditorTerm (startDate: Date) (years: int) : Result<TermPeriod, string> =
            create startDate years (Some 4)

        let isActive (date: Date) (term: TermPeriod) : bool =
            Date.isOnOrAfter date term.StartDate && Date.isOnOrBefore date term.EndDate

        let isExpired (date: Date) (term: TermPeriod) : bool =
            Date.isAfter date term.EndDate

        let daysRemaining (date: Date) (term: TermPeriod) : int =
            if isExpired date term then 0
            else Date.daysBetween date term.EndDate

    // ============================================
    // Deadline (期限)
    // ============================================

    /// Deadline with status tracking
    type DeadlineStatus =
        | Upcoming of daysRemaining: int
        | DueToday
        | Overdue of daysOverdue: int
        | Completed of completedDate: Date

    type Deadline = {
        DueDate: Date
        Description: string
        Status: DeadlineStatus
    }

    module Deadline =

        let create (dueDate: Date) (description: string) : Deadline =
            let today = Date.today()
            let status =
                let diff = Date.daysBetween today dueDate
                if diff > 0 then Upcoming diff
                elif diff = 0 then DueToday
                else Overdue (-diff)
            {
                DueDate = dueDate
                Description = description
                Status = status
            }

        let markCompleted (completedDate: Date) (deadline: Deadline) : Deadline =
            { deadline with Status = Completed completedDate }

        let isOverdue (deadline: Deadline) : bool =
            match deadline.Status with
            | Overdue _ -> true
            | _ -> false

        let isDueSoon (withinDays: int) (deadline: Deadline) : bool =
            match deadline.Status with
            | Upcoming days -> days <= withinDays
            | DueToday -> true
            | _ -> false

    // ============================================
    // Timestamp (タイムスタンプ)
    // ============================================

    /// Immutable timestamp with timezone
    [<Struct>]
    type Timestamp = private Timestamp of DateTimeOffset

    module Timestamp =

        let now () : Timestamp =
            Timestamp DateTimeOffset.UtcNow

        let nowJst () : Timestamp =
            let jst = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")
            Timestamp (TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, jst))

        let fromDateTimeOffset (dto: DateTimeOffset) : Timestamp =
            Timestamp dto

        let value (Timestamp ts) = ts

        let toUtc (Timestamp ts) : DateTimeOffset =
            ts.ToUniversalTime()

        let toDate (Timestamp ts) : Date =
            Date.fromDateTime ts.Date

        let format (Timestamp ts) : string =
            ts.ToString("yyyy-MM-dd HH:mm:ss zzz")

        let formatIso8601 (Timestamp ts) : string =
            ts.ToString("o")

    // ============================================
    // Age / Duration (年齢・期間)
    // ============================================

    /// Age in years, months, days
    type Age = {
        Years: int
        Months: int
        Days: int
    }

    module Age =

        let calculate (birthDate: Date) (asOfDate: Date) : Age =
            let birth = Date.value birthDate
            let asOf = Date.value asOfDate

            let mutable years = asOf.Year - birth.Year
            let mutable months = asOf.Month - birth.Month
            let mutable days = asOf.Day - birth.Day

            if days < 0 then
                months <- months - 1
                let prevMonth = asOf.AddMonths(-1)
                days <- days + DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month)

            if months < 0 then
                years <- years - 1
                months <- months + 12

            { Years = years; Months = months; Days = days }

        let inYears (age: Age) : int = age.Years

        let format (age: Age) : string =
            $"{age.Years}歳{age.Months}ヶ月"
