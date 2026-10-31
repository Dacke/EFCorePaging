namespace EFCorePagingPrototype.Dtos;

public abstract class BasicResponse<T> {
    public int Count => Values?.Length ?? 0;

    public T[] Values { get; set; }

    protected BasicResponse(IEnumerable<T> values) => Values = values.ToArray();
}
