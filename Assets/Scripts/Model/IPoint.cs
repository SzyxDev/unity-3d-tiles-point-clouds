public interface IPoint<T>
{
    public T X { get; set; }
    public T Y { get; set; }
    public T Z { get; set; }
    public byte[] Color { get; set; }
}