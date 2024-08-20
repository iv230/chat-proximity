using System;
using System.Linq;

namespace ChatProximity.Config;

public class Color
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }
    
    public override string ToString()
    {
        return $"{BitConverter.ToString([R])}{BitConverter.ToString([G])}{BitConverter.ToString([B])}";
    }

    public Color(string color)
    {
        var array = Enumerable.Range(0, color.Length)
                              .Where(x => x % 2 == 0)
                              .Select(x => Convert.ToByte(color.Substring(x, 2), 16))
                              .ToArray();

        R = array[0];
        G = array[1];
        B = array[2];
        A = 255;
    }

    public Color(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}
