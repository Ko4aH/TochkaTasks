using System.Text.RegularExpressions;


class HotelCapacity
{
    enum TimelineEvent
    {
        CheckIn = 1,
        CheckOut = -1
    }

    record HotelEvent(string Date, TimelineEvent TimelineEvent);

    static IEnumerable<HotelEvent> BuildTimeline(List<Guest> guests)
    {
        return guests
            .SelectMany(guest => new[]
            {
                new HotelEvent(guest.CheckIn, TimelineEvent.CheckIn),
                new HotelEvent(guest.CheckOut, TimelineEvent.CheckOut)
            })
            .OrderBy(e => e.Date)
            .ThenBy(e => e.TimelineEvent);
    }
    
    static bool CheckCapacity(int maxCapacity, List<Guest> guests)
    {
        var hotelVisits = BuildTimeline(guests);
        var guestCount = 0;
        foreach (var e in hotelVisits)
        {
            guestCount += (int)e.TimelineEvent;
            if (guestCount > maxCapacity)
            {
                return false;
            }
        }
        return true;
    }


    class Guest
    {
        public string Name { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
    }


    static void Main()
    {
        var maxCapacity = int.Parse(Console.ReadLine());
        var n = int.Parse(Console.ReadLine());
        
        var guests = new List<Guest>();
        
        for (int i = 0; i < n; i++)
        {
            var line = Console.ReadLine();
            var guest = ParseGuest(line);
            guests.Add(guest);
        }
        
        var result = CheckCapacity(maxCapacity, guests);
        
        Console.WriteLine(result ? "True" : "False");
    }

    
    static Guest ParseGuest(string json)
    {
        var guest = new Guest();
        
        var nameMatch = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
        if (nameMatch.Success)
            guest.Name = nameMatch.Groups[1].Value;
        
        var checkInMatch = Regex.Match(json, "\"check-in\"\\s*:\\s*\"([^\"]+)\"");
        if (checkInMatch.Success)
            guest.CheckIn = checkInMatch.Groups[1].Value;
        
        var checkOutMatch = Regex.Match(json, "\"check-out\"\\s*:\\s*\"([^\"]+)\"");
        if (checkOutMatch.Success)
            guest.CheckOut = checkOutMatch.Groups[1].Value;
        
        return guest;
    }
}