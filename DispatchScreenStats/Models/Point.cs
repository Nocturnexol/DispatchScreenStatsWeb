namespace DispatchScreenStats.Models
{
    public class Point
    {
        public Point() { }
        public Point(string x, string y)
        {
            X =  x;
            Y =  y;
        }
        public string X { get; set; }
        public string Y { get; set; }
    }
}