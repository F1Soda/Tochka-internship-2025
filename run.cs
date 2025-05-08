using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;


class HotelCapacity
{
    static bool CheckCapacity(int maxCapacity, List<Guest> guests)
    {
        if (maxCapacity >= guests.Count)
            return true;
        
        var events = new List<(DateTime date, bool isArrived)>(guests.Count);
        foreach (var guest in guests)
        {
            var checkIn = DateTime.ParseExact(guest.CheckIn, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var checkOut = DateTime.ParseExact(guest.CheckOut, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            events.Add((checkIn, true));
            events.Add((checkOut, false));
        }
        
        events.Sort((a, b) =>
        {
            int result = a.date.CompareTo(b.date);
            if (result == 0)
                result = a.isArrived.CompareTo(b.isArrived); // false < true
            return result;
        });
        
        var counter = 0;
        foreach (var e in events)
        {
            if (e.isArrived)
            {
                counter++;
                if (counter > maxCapacity)
                    return false;
            }
            else
                counter--;
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
        int maxCapacity = int.Parse(Console.ReadLine());
        int n = int.Parse(Console.ReadLine());


        List<Guest> guests = new List<Guest>();


        for (int i = 0; i < n; i++)
        {
            string line = Console.ReadLine();
            Guest guest = ParseGuest(line);
            guests.Add(guest);
        }


        bool result = CheckCapacity(maxCapacity, guests);


        Console.WriteLine(result ? "True" : "False");
    }


    // Простой парсер JSON-строки для объекта Guest
    static Guest ParseGuest(string json)
    {
        var guest = new Guest();


        // Извлекаем имя
        Match nameMatch = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
        if (nameMatch.Success)
            guest.Name = nameMatch.Groups[1].Value;


        // Извлекаем дату заезда
        Match checkInMatch = Regex.Match(json, "\"check-in\"\\s*:\\s*\"([^\"]+)\"");
        if (checkInMatch.Success)
            guest.CheckIn = checkInMatch.Groups[1].Value;


        // Извлекаем дату выезда
        Match checkOutMatch = Regex.Match(json, "\"check-out\"\\s*:\\s*\"([^\"]+)\"");
        if (checkOutMatch.Success)
            guest.CheckOut = checkOutMatch.Groups[1].Value;


        return guest;
    }
}

